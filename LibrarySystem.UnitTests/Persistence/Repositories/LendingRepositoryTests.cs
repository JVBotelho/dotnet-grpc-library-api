using FluentAssertions;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibrarySystem.UnitTests.Persistence.Repositories;

public class LendingRepositoryTests
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
    public async Task GetByIdAsync_ShouldReturnLendingActivity_WithBook()
    {
        await using var context = CreateDbContext();
        var repository = new LendingRepository(context);
        
        var book = new Book("Title", "Author", 2000, 100, 5);
        var borrower = new Borrower("John", "j@test.com");
        
        await context.Books.AddAsync(book);
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();
        
        var lending = book.BorrowCopy(borrower);
        await context.SaveChangesAsync();
        var result = await repository.GetByIdAsync(lending.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(lending.Id);
        result.Book.Should().NotBeNull();
        result.Book!.Id.Should().Be(book.Id);
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        await using var context = CreateDbContext();
        var repository = new LendingRepository(context);
        var result = await repository.GetByIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBorrowedCopiesCountAsync_ShouldReturnCorrectCount()
    {
        await using var context = CreateDbContext();
        var repository = new LendingRepository(context);
        
        var book = new Book("Title", "Author", 2000, 100, 5);
        var borrower = new Borrower("John", "j@test.com");
        
        await context.Books.AddAsync(book);
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();
        
        var lending1 = book.BorrowCopy(borrower);
        var lending2 = book.BorrowCopy(borrower);
        lending2.MarkAsReturned();
        await context.SaveChangesAsync();
        var count = await repository.GetBorrowedCopiesCountAsync(book.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTopBorrowersAsync_ShouldReturnActivitiesWithinDateRange()
    {
        await using var context = CreateDbContext();
        var repository = new LendingRepository(context);
        
        var book = new Book("Title", "Author", 2000, 100, 5);
        var borrower = new Borrower("John", "j@test.com");
        
        await context.Books.AddAsync(book);
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();
        
        var lending = book.BorrowCopy(borrower);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var result = (await repository.GetTopBorrowersAsync(now.AddDays(-1), now.AddDays(1), 10)).ToList();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(lending.Id);
        result[0].Borrower.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserHistoryAsync_ShouldReturnUserActivitiesWithinDateRange()
    {
        await using var context = CreateDbContext();
        var repository = new LendingRepository(context);
        
        var book = new Book("Title", "Author", 2000, 100, 5);
        var borrower = new Borrower("John", "j@test.com");
        var borrower2 = new Borrower("Jane", "jane@test.com");
        
        await context.Books.AddAsync(book);
        await context.Borrowers.AddRangeAsync(borrower, borrower2);
        await context.SaveChangesAsync();
        
        var lending1 = book.BorrowCopy(borrower);
        var lending2 = book.BorrowCopy(borrower2);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var result = (await repository.GetUserHistoryAsync(borrower.Id, now.AddDays(-1), now.AddDays(1))).ToList();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(lending1.Id);
        result[0].Book.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByBookIdAsync_ShouldReturnReturnedLendingActivitiesForBook()
    {
        await using var context = CreateDbContext();
        var repository = new LendingRepository(context);
        
        var book = new Book("Title", "Author", 2000, 100, 5);
        var book2 = new Book("T2", "A2", 2000, 100, 5);
        var borrower = new Borrower("John", "j@test.com");
        
        await context.Books.AddRangeAsync(book, book2);
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();
        
        var lending1 = book.BorrowCopy(borrower);
        lending1.MarkAsReturned();
        
        var lending2 = book.BorrowCopy(borrower); // not returned
        
        var lending3 = book2.BorrowCopy(borrower);
        lending3.MarkAsReturned();

        await context.SaveChangesAsync();
        var result = (await repository.GetByBookIdAsync(book.Id)).ToList();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(lending1.Id);
    }
}

