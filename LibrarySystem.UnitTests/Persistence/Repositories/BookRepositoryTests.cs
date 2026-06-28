using FluentAssertions;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibrarySystem.UnitTests.Persistence.Repositories;

public class BookRepositoryTests
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
    public async Task GetByIdAsync_ShouldReturnBook_WhenBookExists()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);
        var book = new Book("Title", "Author", 2000, 100, 5);
        await context.Books.AddAsync(book);
        await context.SaveChangesAsync();
        var result = await repository.GetByIdAsync(book.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(book.Id);
        result.Title.Should().Be("Title");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenBookDoesNotExist()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);
        var result = await repository.GetByIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBooks()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);
        await context.Books.AddRangeAsync(
            new Book("T1", "A1", 2001, 100, 1),
            new Book("T2", "A2", 2002, 200, 2)
        );
        await context.SaveChangesAsync();
        var result = await repository.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddAsync_ShouldAddBook()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);
        var book = new Book("Title", "Author", 2000, 100, 5);
        await repository.AddAsync(book);
        await repository.SaveChangesAsync();
        var addedBook = await context.Books.FindAsync(new object[]{book.Id});
        addedBook.Should().NotBeNull();
        addedBook!.Title.Should().Be("Title");
    }

    [Fact]
    public async Task Update_ShouldUpdateBook()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);
        var book = new Book("Title", "Author", 2000, 100, 5);
        await context.Books.AddAsync(book);
        await context.SaveChangesAsync();
        book.UpdateDetails("New Title", "Author", 2000, 100, 5);
        repository.Update(book);
        await repository.SaveChangesAsync();
        var updatedBook = await context.Books.FindAsync(new object[]{book.Id});
        updatedBook!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task Remove_ShouldRemoveBook()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);
        var book = new Book("Title", "Author", 2000, 100, 5);
        await context.Books.AddAsync(book);
        await context.SaveChangesAsync();
        repository.Remove(book);
        await repository.SaveChangesAsync();
        var removedBook = await context.Books.FindAsync(new object[]{book.Id});
        removedBook.Should().BeNull();
    }

    [Fact]
    public async Task GetMostBorrowedAsync_ShouldReturnMostBorrowedBooks()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);
        
        var book1 = new Book("B1", "A1", 2000, 100, 5);
        var book2 = new Book("B2", "A2", 2000, 100, 5);
        var book3 = new Book("B3", "A3", 2000, 100, 5);
        await context.Books.AddRangeAsync(book1, book2, book3);
        
        var borrower = new Borrower("John Doe", "john@test.com");
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();

        // book1: 2 borrows, book2: 1 borrow, book3: 0 borrows
        var la1 = book1.BorrowCopy(borrower);
        la1.MarkAsReturned();
        var la2 = book1.BorrowCopy(borrower);
        var la3 = book2.BorrowCopy(borrower);

        await context.SaveChangesAsync();
        var result = (await repository.GetMostBorrowedAsync(2)).ToList();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(book1.Id);
        result[1].Id.Should().Be(book2.Id);
    }

    [Fact]
    public async Task GetAlsoBorrowedAsync_ShouldReturnBooksBorrowedBySameUsers()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);

        var book1 = new Book("B1", "A1", 2000, 100, 5);
        var book2 = new Book("B2", "A2", 2000, 100, 5);
        var book3 = new Book("B3", "A3", 2000, 100, 5);
        var book4 = new Book("B4", "A4", 2000, 100, 5);
        
        var borrower1 = new Borrower("User1", "u1@test.com");
        var borrower2 = new Borrower("User2", "u2@test.com");

        await context.Books.AddRangeAsync(book1, book2, book3, book4);
        await context.Borrowers.AddRangeAsync(borrower1, borrower2);
        await context.SaveChangesAsync();

        // borrower1 borrows B1, B2, B3
        book1.BorrowCopy(borrower1);
        book2.BorrowCopy(borrower1);
        book3.BorrowCopy(borrower1);

        // borrower2 borrows B1, B3, B4
        book1.BorrowCopy(borrower2);
        book3.BorrowCopy(borrower2);
        book4.BorrowCopy(borrower2);

        await context.SaveChangesAsync();

        // Act: GetAlsoBorrowedAsync for B1. 
        var result = (await repository.GetAlsoBorrowedAsync(book1.Id, 2)).ToList();
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Id == book3.Id);
    }
    
    [Fact]
    public async Task GetAlsoBorrowedAsync_ShouldReturnEmpty_WhenNoOtherBorrowers()
    {
        await using var context = CreateDbContext();
        var repository = new BookRepository(context);

        var book1 = new Book("B1", "A1", 2000, 100, 5);
        await context.Books.AddAsync(book1);
        await context.SaveChangesAsync();
        var result = await repository.GetAlsoBorrowedAsync(book1.Id, 2);
        result.Should().BeEmpty();
    }
}

