using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Api.Protos;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.IntegrationTests;

public class BooksControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task CreateBook_WithValidData_ReturnsCreatedResponse()
    {
        // Arrange
        var createBookDto = _fixture.Create<CreateBookDto>();

        // Act
        var response = await _client.PostAsJsonAsync("/api/books", createBookDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("http://localhost/api/Books/");

        var createdBook = await response.Content.ReadFromJsonAsync<BookResponse>();
        createdBook.Should().NotBeNull();
        createdBook!.Title.Should().Be(createBookDto.Title);
        createdBook.Author.Should().Be(createBookDto.Author);
        createdBook.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateBook_WithValidData_ReturnsOkResponse()
    {
        // Arrange: Primeiro, criar um livro para depois o podermos atualizar.
        var createDto = _fixture.Create<CreateBookDto>();
        var createResponse = await _client.PostAsJsonAsync("/api/books", createDto);
        var createdBook = await createResponse.Content.ReadFromJsonAsync<BookResponse>();

        var updateDto = _fixture.Create<UpdateBookDto>();

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/books/{createdBook!.Id}", updateDto);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedBook = await updateResponse.Content.ReadFromJsonAsync<BookResponse>();
        updatedBook!.Title.Should().Be(updateDto.Title);
    }

    [Fact]
    public async Task DeleteBook_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var createDto = _fixture.Create<CreateBookDto>();
        var createResponse = await _client.PostAsJsonAsync("/api/books", createDto);
        var createdBook = await createResponse.Content.ReadFromJsonAsync<BookResponse>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/books/{createdBook!.Id}");

        // Assert 
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/books/{createdBook.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMostBorrowedBooks_ReturnsCorrectlyOrderedBooks()
    {
        // Arrange
        var book1Dto = _fixture.Create<CreateBookDto>();
        var book2Dto = _fixture.Create<CreateBookDto>();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var borrower = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        context.Borrowers.Add(borrower);
        await context.SaveChangesAsync();

        var book1Response = await _client.PostAsJsonAsync("/api/books", book1Dto);
        var book2Response = await _client.PostAsJsonAsync("/api/books", book2Dto);
        var book1 = await book1Response.Content.ReadFromJsonAsync<BookResponse>();
        var book2 = await book2Response.Content.ReadFromJsonAsync<BookResponse>();

        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book1!.Id, BorrowerId = borrower.Id });
        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book1!.Id, BorrowerId = borrower.Id });
        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book2!.Id, BorrowerId = borrower.Id });

        // Act
        var response = await _client.GetAsync("/api/books/most-borrowed?count=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mostBorrowed = await response.Content.ReadFromJsonAsync<List<MostBorrowedBookInfo>>();

        mostBorrowed.Should().HaveCount(2);
        mostBorrowed.Should().BeInDescendingOrder(b => b.BorrowCount);
        mostBorrowed!.First().Book.Id.Should().Be(book1.Id);
        mostBorrowed.First().BorrowCount.Should().Be(2);
    }

    [Fact]
    public async Task GetBookAvailability_ReturnsCorrectAvailabilityCounts()
    {
        // Arrange
        var createBookDto = _fixture.Build<CreateBookDto>().With(b => b.TotalCopies, 10).Create();
        var bookResponse = await _client.PostAsJsonAsync("/api/books", createBookDto);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var borrower = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        context.Borrowers.Add(borrower);
        await context.SaveChangesAsync();

        for (var i = 0; i < 4; i++)
            await _client.PostAsJsonAsync("/api/lending",
                new CreateLendingDto { BookId = book!.Id, BorrowerId = borrower.Id });

        // Act
        var availabilityResponse = await _client.GetAsync($"/api/books/{book!.Id}/availability");

        // Assert
        availabilityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var availability = await availabilityResponse.Content.ReadFromJsonAsync<BookAvailabilityResponse>();

        availability.Should().NotBeNull();
        availability!.TotalCopies.Should().Be(10);
        availability.BorrowedCopies.Should().Be(4);
        availability.AvailableCopies.Should().Be(6);
    }

    [Fact]
    public async Task GetLendingHistory_ReturnsCorrectHistoryForUser()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var book1 = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var book2 = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var borrower = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        await context.Books.AddRangeAsync(book1, book2);
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();

        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book1.Id, BorrowerId = borrower.Id });
        var oldLending = new LendingActivity
            { BookId = book2.Id, BorrowerId = borrower.Id, BorrowedDate = DateTime.UtcNow.AddMonths(-2) };
        context.LendingActivities.Add(oldLending);
        await context.SaveChangesAsync();

        var startDate = DateTime.UtcNow.AddDays(-1).ToString("o");
        var endDate = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response =
            await _client.GetAsync($"/api/borrowers/{borrower.Id}/history?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<UserLendingHistoryItem>>();

        history.Should().NotBeNull().And.HaveCount(1);
        history!.First().Book.Id.Should().Be(book1.Id);
    }

    [Fact]
    public async Task GetAlsoBorrowed_ReturnsCorrectlyRankedBooks()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var mainBook = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var otherBook = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var borrower1 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        var borrower2 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        await context.Books.AddRangeAsync(mainBook, otherBook);
        await context.Borrowers.AddRangeAsync(borrower1, borrower2);
        await context.SaveChangesAsync();

        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = mainBook.Id, BorrowerId = borrower1.Id });
        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = otherBook.Id, BorrowerId = borrower1.Id });
        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = mainBook.Id, BorrowerId = borrower2.Id });

        // Act
        var response = await _client.GetAsync($"/api/books/{mainBook.Id}/also-borrowed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var alsoBorrowed = await response.Content.ReadFromJsonAsync<List<AlsoBorrowedBookInfo>>();

        alsoBorrowed.Should().NotBeNull().And.HaveCount(1);
        alsoBorrowed!.First().Book.Id.Should().Be(otherBook.Id);
        alsoBorrowed!.First().CommonBorrowersCount.Should().Be(1);
    }

    [Fact]
    public async Task GetReadingRate_WithCompletedLoan_ReturnsCorrectRate()
    {
        // Arrange
        var bookDto = _fixture.Build<CreateBookDto>().With(b => b.Pages, 100).Create();
        var bookResponse = await _client.PostAsJsonAsync("/api/books", bookDto);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var borrower = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        context.Borrowers.Add(borrower);
        await context.SaveChangesAsync();

        var lendingResponse = await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book!.Id, BorrowerId = borrower.Id });
        var lending = await lendingResponse.Content.ReadFromJsonAsync<LendingActivityResponse>();

        var activity = await context.LendingActivities.FindAsync(lending!.Id);
        activity!.BorrowedDate = DateTime.UtcNow.AddDays(-10); 
        activity.ReturnedDate = DateTime.UtcNow; 
        await context.SaveChangesAsync();

        // Act
        var rateResponse = await _client.GetAsync($"/api/books/{book.Id}/reading-rate");

        // Assert
        rateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rate = await rateResponse.Content.ReadFromJsonAsync<EstimateReadingRateResponse>();

        rate.Should().NotBeNull();
        rate!.PagesPerDay.Should().BeApproximately(10, 0.01);
    }

    private async Task ResetDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}