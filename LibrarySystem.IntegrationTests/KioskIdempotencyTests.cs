using FluentAssertions;
using LibrarySystem.Application.UseCases.Kiosk;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace LibrarySystem.IntegrationTests;

public class KioskIdempotencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer;

    public KioskIdempotencyTests()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16.11-alpine")
            .Build();
    }

    public Task InitializeAsync()
    {
        return _postgreSqlContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync().AsTask();
    }

    private DbContextOptions<LibraryDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<LibraryDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .Options;
    }

    [Fact]
    public async Task BulkReturn_WithSameIdempotencyKey_ShouldSkipDuplicate()
    {
        // Arrange
        var options = CreateNewContextOptions();
        
        using (var context = new LibraryDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var book = new Book("Test Book", "Author", 2026, 100, 1);
            context.Books.Add(book);
            var borrower = new Borrower("Test User", "test@test.com");
            context.Borrowers.Add(borrower);
            await context.SaveChangesAsync();

            // Borrow the book
            book.BorrowCopy(borrower);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        var scanDto = new ReturnScanDto(1, DateTime.UtcNow, "txn_12345");
        var command = new BulkReturnCommand("KIOSK-01", new[] { scanDto });

        // First pass
        using (var context = new LibraryDbContext(options))
        {
            var bookRepo = new BookRepository(context);
            var processedRepo = new ProcessedEventRepository(context);
            var handler = new BulkReturnCommandHandler(bookRepo, processedRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.Accepted.Should().Be(1);
            result.Rejected.Should().Be(0);
        }

        // Second pass (replay)
        using (var context = new LibraryDbContext(options))
        {
            var bookRepo = new BookRepository(context);
            var processedRepo = new ProcessedEventRepository(context);
            var handler = new BulkReturnCommandHandler(bookRepo, processedRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            // Because the key is identical, it should be treated as duplicate.
            result.Accepted.Should().Be(0);
            result.Rejected.Should().Be(0);
            result.DuplicatesSkipped.Should().Be(1);
        }
    }

    [Fact]
    public async Task BulkReturn_ConcurrentReplay_ShouldNotDoubleReturn()
    {
        // Arrange
        var options = CreateNewContextOptions();

        using (var context = new LibraryDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var book = new Book("Concurrent Test Book", "Author", 2026, 100, 1);
            context.Books.Add(book);
            var borrower = new Borrower("Concurrent User", "concurrent@test.com");
            context.Borrowers.Add(borrower);
            await context.SaveChangesAsync();

            book.BorrowCopy(borrower);
            await context.SaveChangesAsync();
        }

        var scanDto = new ReturnScanDto(1, DateTime.UtcNow, "txn_race_key");
        var command = new BulkReturnCommand("KIOSK-01", new[] { scanDto });

        // Act: two concurrent handlers race with the same idempotency key.
        // One must win (accept) and the other must lose (duplicate), never double-return.
        var task1 = Task.Run(async () =>
        {
            using var ctx = new LibraryDbContext(options);
            return await new BulkReturnCommandHandler(new BookRepository(ctx), new ProcessedEventRepository(ctx))
                .Handle(command, CancellationToken.None);
        });

        var task2 = Task.Run(async () =>
        {
            using var ctx = new LibraryDbContext(options);
            return await new BulkReturnCommandHandler(new BookRepository(ctx), new ProcessedEventRepository(ctx))
                .Handle(command, CancellationToken.None);
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert
        var totalAccepted = results.Sum(r => r.Accepted);
        totalAccepted.Should().Be(1, "exactly one handler should succeed in returning the book — no double-return");

        // Verify the idempotency key is stored exactly once regardless of which handler won.
        using (var verifyCtx = new LibraryDbContext(options))
        {
            var storedCount = await verifyCtx.ProcessedEvents
                .CountAsync(e => e.IdempotencyKey == "txn_race_key");
            storedCount.Should().Be(1);
        }
    }
}
