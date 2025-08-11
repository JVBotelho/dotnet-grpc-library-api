using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Api.Protos;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.IntegrationTests;

public class LendingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly Fixture _fixture;

    public LendingControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _fixture = new Fixture();
    }

    [Fact]
    public async Task ReturnBook_WhenLendingExists_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var book = _fixture.Build<Book>().Without(b => b.LendingActivities).Create();
        var borrower = _fixture.Build<Borrower>().Without(b => b.LendingActivities).Create();
        await context.Books.AddAsync(book);
        await context.Borrowers.AddAsync(borrower);
        await context.SaveChangesAsync();

        var lendingResponse = await _client.PostAsJsonAsync("/api/lending",
            new CreateLendingDto { BookId = book.Id, BorrowerId = borrower.Id });
        var lending = await lendingResponse.Content.ReadFromJsonAsync<LendingActivityResponse>();

        // Act
        var returnResponse = await _client.PutAsync($"/api/lending/{lending!.Id}/return", null);

        // Assert
        returnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedLending = await returnResponse.Content.ReadFromJsonAsync<LendingActivityResponse>();
        returnedLending.Should().NotBeNull();
        returnedLending!.ReturnedDate.Should().NotBeNull();
    }
}