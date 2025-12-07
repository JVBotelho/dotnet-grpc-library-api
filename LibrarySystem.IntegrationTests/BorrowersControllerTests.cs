using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.IntegrationTests;

public class BorrowersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly Fixture _fixture;

    public BorrowersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetTopBorrowers_ReturnsCorrectlyOrderedBorrowers()
    {
        // Arrange
        // Seed the database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var book = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var borrower1 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create(); // Most active
        var borrower2 = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create(); // Less active
        await context.Books.AddAsync(book);
        await context.Borrowers.AddRangeAsync(borrower1, borrower2);
        await context.SaveChangesAsync();

        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book.Id, BorrowerId = borrower1.Id });
        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book.Id, BorrowerId = borrower1.Id });
        await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book.Id, BorrowerId = borrower2.Id });

        var startDate = DateTime.UtcNow.AddDays(-1).ToString("o"); // ISO 8601 format
        var endDate = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response =
            await _client.GetAsync($"/api/borrowers/most-active?startDate={startDate}&endDate={endDate}&count=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var topBorrowers = await response.Content.ReadFromJsonAsync<List<TopBorrowerInfo>>();

        topBorrowers.Should().NotBeNull().And.HaveCount(2);
        topBorrowers.Should().BeInDescendingOrder(b => b.BorrowCount);
        topBorrowers!.First().Borrower.Id.Should().Be(borrower1.Id);
        topBorrowers.First().BorrowCount.Should().Be(2);
    }
}