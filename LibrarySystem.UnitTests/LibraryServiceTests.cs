using AutoFixture;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Grpc.Services;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Entities;
using LibrarySystem.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.UnitTests;

public class LibraryServiceTests
{
    private readonly DbContextOptions<LibraryDbContext> _dbOptions;
    private readonly Fixture _fixture;

    public LibraryServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _dbOptions = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CreateBook_ShouldAddBookToDatabase_AndReturnCorrectResponse()
    {
        // Arrange
        var createRequest = _fixture.Create<CreateBookRequest>();

        await using var context = new LibraryDbContext(_dbOptions);

        var sut = new LibraryService(context);

        // Act
        var response = await sut.CreateBook(createRequest, TestServerCallContext.Create());

        // Assert
        response.Should().NotBeNull();
        response.Title.Should().Be(createRequest.Title);
        response.Author.Should().Be(createRequest.Author);
        response.Id.Should().BeGreaterThan(0);

        var bookInDb = await context.Books.FindAsync(response.Id);
        bookInDb.Should().NotBeNull();
        bookInDb!.Title.Should().Be(createRequest.Title);
    }

    [Fact]
    public async Task UpdateBook_WhenBookExists_ShouldUpdateAndReturnBook()
    {
        // Arrange
        var book = _fixture.Create<Book>();
        await using var context = new LibraryDbContext(_dbOptions);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var updateRequest = _fixture.Build<UpdateBookRequest>()
            .With(r => r.Id, book.Id)
            .Create();
        var sut = new LibraryService(context);

        // Act
        var response = await sut.UpdateBook(updateRequest, TestServerCallContext.Create());

        // Assert
        response.Title.Should().Be(updateRequest.Title);
        var bookInDb = await context.Books.FindAsync(book.Id);
        bookInDb!.Title.Should().Be(updateRequest.Title);
    }

    [Fact]
    public async Task DeleteBook_WhenBookExists_ShouldRemoveBookFromDatabase()
    {
        // Arrange
        var book = _fixture.Create<Book>();
        await using var context = new LibraryDbContext(_dbOptions);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var deleteRequest = new DeleteBookRequest { Id = book.Id };
        var sut = new LibraryService(context);

        // Act
        await sut.DeleteBook(deleteRequest, TestServerCallContext.Create());

        // Assert
        var bookInDb = await context.Books.FindAsync(book.Id);
        bookInDb.Should().BeNull();
    }

    [Fact]
    public async Task GetMostBorrowedBooks_ShouldReturnBooksOrderedByBorrowCount()
    {
        // Arrange
        await using var context = new LibraryDbContext(_dbOptions);

        var books = _fixture.Build<Book>().Without(b => b.LendingActivities).CreateMany(3).ToList();
        var borrowers = _fixture.Build<Borrower>().Without(b => b.LendingActivities).CreateMany(2).ToList();

        await context.Books.AddRangeAsync(books);
        await context.Borrowers.AddRangeAsync(borrowers);
        await context.SaveChangesAsync();

        var lendingActivities = new List<LendingActivity>
        {
            new() { BookId = books[0].Id, BorrowerId = borrowers[0].Id },
            new() { BookId = books[0].Id, BorrowerId = borrowers[1].Id },
            new() { BookId = books[0].Id, BorrowerId = borrowers[0].Id },
            new() { BookId = books[1].Id, BorrowerId = borrowers[1].Id },
            new() { BookId = books[1].Id, BorrowerId = borrowers[0].Id },
            new() { BookId = books[2].Id, BorrowerId = borrowers[1].Id }
        };
        await context.LendingActivities.AddRangeAsync(lendingActivities);
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new GetMostBorrowedBooksRequest { Count = 3 };

        // Act
        var response = await sut.GetMostBorrowedBooks(request, TestServerCallContext.Create());

        // Assert
        response.MostBorrowedBooks.Should().HaveCount(3);
        response.MostBorrowedBooks.Should().BeInDescendingOrder(b => b.BorrowCount);
        response.MostBorrowedBooks.First().Book.Id.Should().Be(books[0].Id);
        response.MostBorrowedBooks.First().BorrowCount.Should().Be(3);
        response.MostBorrowedBooks.Last().Book.Id.Should().Be(books[2].Id);
        response.MostBorrowedBooks.Last().BorrowCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateLending_WhenBookIsAvailable_ShouldCreateLendingActivity()
    {
        // Arrange
        var book = _fixture.Build<Book>().With(b => b.TotalCopies, 1).Create();
        var borrower = _fixture.Create<Borrower>();

        await using var context = new LibraryDbContext(_dbOptions);
        context.Books.Add(book);
        context.Borrowers.Add(borrower);
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new CreateLendingRequest { BookId = book.Id, BorrowerId = borrower.Id };

        // Act
        var response = await sut.CreateLending(request, TestServerCallContext.Create());

        // Assert
        response.Should().NotBeNull();
        response.BookId.Should().Be(book.Id);
        var activityInDb = await context.LendingActivities.FindAsync(response.Id);
        activityInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateLending_WhenNoCopiesAvailable_ShouldThrowRpcException()
    {
        // Arrange
        var book = _fixture.Build<Book>().With(b => b.TotalCopies, 1).Create();
        var borrower1 = _fixture.Create<Borrower>();
        var borrower2 = _fixture.Create<Borrower>();

        await using var context = new LibraryDbContext(_dbOptions);
        context.Books.Add(book);
        context.Borrowers.AddRange(borrower1, borrower2);
        context.LendingActivities.Add(new LendingActivity
            { BookId = book.Id, BorrowerId = borrower1.Id, BorrowedDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new CreateLendingRequest { BookId = book.Id, BorrowerId = borrower2.Id };

        // Act & Assert
        var action = async () => await sut.CreateLending(request, TestServerCallContext.Create());

        await action.Should().ThrowAsync<RpcException>()
            .Where(ex => ex.StatusCode == StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task GetBookAvailability_WhenBookExists_ReturnsCorrectCounts()
    {
        // Arrange
        var book = _fixture.Build<Book>().With(b => b.TotalCopies, 5).Create();
        var borrower = _fixture.Create<Borrower>();

        await using var context = new LibraryDbContext(_dbOptions);
        context.Books.Add(book);
        context.Borrowers.Add(borrower);
        context.LendingActivities.AddRange(
            new LendingActivity { BookId = book.Id, BorrowerId = borrower.Id, BorrowedDate = DateTime.UtcNow },
            new LendingActivity { BookId = book.Id, BorrowerId = borrower.Id, BorrowedDate = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new GetBookAvailabilityRequest { BookId = book.Id };

        // Act
        var response = await sut.GetBookAvailability(request, TestServerCallContext.Create());

        // Assert
        response.Should().NotBeNull();
        response.TotalCopies.Should().Be(5);
        response.BorrowedCopies.Should().Be(2);
        response.AvailableCopies.Should().Be(3);
    }

    [Fact]
    public async Task GetTopBorrowers_WithValidDateRange_ReturnsCorrectlyOrderedBorrowers()
    {
        // Arrange
        await using var context = new LibraryDbContext(_dbOptions);

        var book = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var borrower1 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        var borrower2 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        var borrower3 =
            _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();

        await context.Books.AddAsync(book);
        await context.Borrowers.AddRangeAsync(borrower1, borrower2, borrower3);
        await context.SaveChangesAsync();

        var today = DateTime.UtcNow;
        var lendingActivities = new List<LendingActivity>
        {
            new() { BookId = book.Id, BorrowerId = borrower1.Id, BorrowedDate = today.AddDays(-2) },
            new() { BookId = book.Id, BorrowerId = borrower1.Id, BorrowedDate = today.AddDays(-3) },
            new() { BookId = book.Id, BorrowerId = borrower2.Id, BorrowedDate = today.AddDays(-1) },
            new() { BookId = book.Id, BorrowerId = borrower3.Id, BorrowedDate = today.AddDays(-20) }
        };
        await context.LendingActivities.AddRangeAsync(lendingActivities);
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new GetTopBorrowersRequest
        {
            Count = 5,
            StartDate = Timestamp.FromDateTime(today.AddDays(-10)),
            EndDate = Timestamp.FromDateTime(today)
        };

        // Act
        var response = await sut.GetTopBorrowers(request, TestServerCallContext.Create());

        // Assert
        response.TopBorrowers.Should().HaveCount(2);
        response.TopBorrowers.Should().BeInDescendingOrder(b => b.BorrowCount);
        response.TopBorrowers.First().Borrower.Id.Should().Be(borrower1.Id);
        response.TopBorrowers.First().BorrowCount.Should().Be(2);
        response.TopBorrowers.Last().Borrower.Id.Should().Be(borrower2.Id);
        response.TopBorrowers.Last().BorrowCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUserLendingHistory_WithValidRequest_ReturnsOnlyActivitiesInRange()
    {
        // Arrange
        var book1 = _fixture.Create<Book>();
        var book2 = _fixture.Create<Book>();
        var borrower = _fixture.Create<Borrower>();

        var today = DateTime.UtcNow;
        var lendingActivities = new List<LendingActivity>
        {
            new() { Book = book1, Borrower = borrower, BorrowedDate = today.AddDays(-5) },
            new() { Book = book2, Borrower = borrower, BorrowedDate = today.AddDays(-30) }
        };

        await using var context = new LibraryDbContext(_dbOptions);
        await context.LendingActivities.AddRangeAsync(lendingActivities);
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new GetUserLendingHistoryRequest
        {
            BorrowerId = borrower.Id,
            StartDate = Timestamp.FromDateTime(today.AddDays(-10)),
            EndDate = Timestamp.FromDateTime(today)
        };

        // Act
        var response = await sut.GetUserLendingHistory(request, TestServerCallContext.Create());

        // Assert
        response.History.Should().HaveCount(1);
        response.History.First().Book.Id.Should().Be(book1.Id);
    }

    [Fact]
    public async Task GetAlsoBorrowedBooks_ShouldReturnCorrectlyRankedBooks()
    {
        // Arrange
        await using var context = new LibraryDbContext(_dbOptions);

        var mainBook = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var alsoBorrowedBook1 = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var alsoBorrowedBook2 = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var borrower1 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        var borrower2 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();

        await context.Books.AddRangeAsync(mainBook, alsoBorrowedBook1, alsoBorrowedBook2);
        await context.Borrowers.AddRangeAsync(borrower1, borrower2);
        await context.SaveChangesAsync();

        var activities = new List<LendingActivity>
        {
            new() { BookId = mainBook.Id, BorrowerId = borrower1.Id },
            new() { BookId = alsoBorrowedBook1.Id, BorrowerId = borrower1.Id },
            new() { BookId = mainBook.Id, BorrowerId = borrower2.Id },
            new() { BookId = alsoBorrowedBook1.Id, BorrowerId = borrower2.Id },
            new() { BookId = alsoBorrowedBook2.Id, BorrowerId = borrower2.Id }
        };
        await context.LendingActivities.AddRangeAsync(activities);
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new GetAlsoBorrowedBooksRequest { BookId = mainBook.Id, Count = 5 };

        // Act
        var response = await sut.GetAlsoBorrowedBooks(request, TestServerCallContext.Create());

        // Assert
        response.AlsoBorrowedBooks.Should().HaveCount(2);
        response.AlsoBorrowedBooks.Should().BeInDescendingOrder(b => b.CommonBorrowersCount);
        response.AlsoBorrowedBooks.First().Book.Id.Should().Be(alsoBorrowedBook1.Id);
        response.AlsoBorrowedBooks.First().CommonBorrowersCount.Should().Be(2);
        response.AlsoBorrowedBooks.Last().Book.Id.Should().Be(alsoBorrowedBook2.Id);
        response.AlsoBorrowedBooks.Last().CommonBorrowersCount.Should().Be(1);
    }

    [Fact]
    public async Task EstimateReadingRate_WithCompletedLoans_ReturnsCorrectRate()
    {
        // Arrange
        await using var context = new LibraryDbContext(_dbOptions);

        var book = _fixture.Build<Book>()
            .Without(b => b.LendingActivities)
            .With(b => b.Pages, 200)
            .Create();
        var borrower = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();

        await context.Books.AddAsync(book);
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();

        var activities = new List<LendingActivity>
        {
            new()
            {
                BookId = book.Id, BorrowerId = borrower.Id, BorrowedDate = DateTime.UtcNow.AddDays(-10),
                ReturnedDate = DateTime.UtcNow
            },
            new()
            {
                BookId = book.Id, BorrowerId = borrower.Id, BorrowedDate = DateTime.UtcNow.AddDays(-20),
                ReturnedDate = DateTime.UtcNow.AddDays(-10)
            }
        };

        await context.LendingActivities.AddRangeAsync(activities);
        await context.SaveChangesAsync();

        var sut = new LibraryService(context);
        var request = new EstimateReadingRateRequest { BookId = book.Id };

        // Act
        var response = await sut.EstimateReadingRate(request, TestServerCallContext.Create());

        // Assert
        response.PagesPerDay.Should().BeApproximately(20, 0.01);
    }
}