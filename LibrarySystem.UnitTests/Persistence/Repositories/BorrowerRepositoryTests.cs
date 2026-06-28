using FluentAssertions;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibrarySystem.UnitTests.Persistence.Repositories;

public class BorrowerRepositoryTests
{
    private LibraryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LibraryDbContext(options);
        return context;
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBorrower_WhenExists()
    {
        await using var context = CreateDbContext();
        var repository = new BorrowerRepository(context);
        var borrower = new Borrower("John", "john@test.com");
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();
        var result = await repository.GetByIdAsync(borrower.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(borrower.Id);
        result.Name.Should().Be("John");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
    {
        await using var context = CreateDbContext();
        var repository = new BorrowerRepository(context);
        var result = await repository.GetByIdAsync(999);
        result.Should().BeNull();
    }
}

