using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Contracts.Protos;
using Moq;

namespace LibrarySystem.IntegrationTests;

public class BooksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Fixture _fixture = new();
    private readonly Mock<Library.LibraryClient> _grpcMock;

    public BooksControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _grpcMock = factory.LibraryClientMock;

        // Clean slate for every test
        _grpcMock.Reset();
    }

    [Fact]
    public async Task CreateBook_WithValidData_ReturnsCreatedResponse()
    {
        // Arrange
        var createDto = _fixture.Create<CreateBookDto>();
        var expectedResponse = new BookResponse
        {
            Id = 10,
            Title = createDto.Title,
            Author = createDto.Author,
            PublicationYear = createDto.PublicationYear,
            Pages = createDto.Pages,
            TotalCopies = createDto.TotalCopies
        };

        // Mock: Accept any CreateBookRequest, return the expected BookResponse
        SetupGrpcCall(c => c.CreateBookAsync(It.IsAny<CreateBookRequest>(), It.IsAny<CallOptions>()), expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/books", createDto);

        await EnsureSuccessOrThrow(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify Location Header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Contain("/api/Books/10");

        // Verify Body
        var createdBook = await response.Content.ReadFromJsonAsync<BookResponse>();
        createdBook.Should().NotBeNull();
        createdBook!.Title.Should().Be(createDto.Title);
        createdBook.Id.Should().Be(10);
    }

    [Fact]
    public async Task UpdateBook_WithValidData_ReturnsOkResponse()
    {
        // Arrange
        var updateDto = _fixture.Create<UpdateBookDto>();
        var expectedResponse = new BookResponse
        {
            Id = 1,
            Title = updateDto.Title,
            Author = updateDto.Author
        };

        SetupGrpcCall(c => c.UpdateBookAsync(It.IsAny<UpdateBookRequest>(), It.IsAny<CallOptions>()), expectedResponse);

        // Act
        var response = await _client.PutAsJsonAsync("/api/books/1", updateDto);
        await EnsureSuccessOrThrow(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedBook = await response.Content.ReadFromJsonAsync<BookResponse>();
        updatedBook!.Title.Should().Be(updateDto.Title);
    }

    [Fact]
    public async Task DeleteBook_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        SetupGrpcCall(c => c.DeleteBookAsync(It.IsAny<DeleteBookRequest>(), It.IsAny<CallOptions>()), new Empty());

        // Act
        var response = await _client.DeleteAsync("/api/books/1");

        // Assert 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetBook_WhenNotFound_Returns404()
    {
        // Arrange: Simulate gRPC throwing RpcException(NotFound)
        var rpcException = new RpcException(new Status(StatusCode.NotFound, "Book not found"));

        _grpcMock.Setup(x => x.GetBookByIdAsync(It.IsAny<GetBookByIdRequest>(), It.IsAny<CallOptions>()))
            .Throws(rpcException);

        // Act
        var response = await _client.GetAsync("/api/books/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Book not found");
    }

    [Fact]
    public async Task GetMostBorrowedBooks_ReturnsCorrectlyMappedList()
    {
        // Arrange
        var grpcResponse = new GetMostBorrowedBooksResponse();
        grpcResponse.MostBorrowedBooks.Add(new MostBorrowedBookInfo
        {
            Book = new BookResponse { Id = 1, Title = "Clean Code" },
            BorrowCount = 100
        });
        grpcResponse.MostBorrowedBooks.Add(new MostBorrowedBookInfo
        {
            Book = new BookResponse { Id = 2, Title = "DDD Distilled" },
            BorrowCount = 50
        });

        SetupGrpcCall(
            c => c.GetMostBorrowedBooksAsync(It.IsAny<GetMostBorrowedBooksRequest>(), It.IsAny<CallOptions>()),
            grpcResponse);

        // Act
        var response = await _client.GetAsync("/api/books/most-borrowed?count=2");
        await EnsureSuccessOrThrow(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<MostBorrowedBookInfo>>();

        list.Should().HaveCount(2);
        list!.First().Book.Title.Should().Be("Clean Code");
        list.First().BorrowCount.Should().Be(100);
    }

    [Fact]
    public async Task GetBookAvailability_ReturnsCorrectCounts()
    {
        // Arrange
        var expectedResponse = new BookAvailabilityResponse
        {
            TotalCopies = 10,
            BorrowedCopies = 3,
            AvailableCopies = 7
        };

        SetupGrpcCall(c => c.GetBookAvailabilityAsync(It.IsAny<GetBookAvailabilityRequest>(), It.IsAny<CallOptions>()),
            expectedResponse);

        // Act
        var response = await _client.GetAsync("/api/books/1/availability");
        await EnsureSuccessOrThrow(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var availability = await response.Content.ReadFromJsonAsync<BookAvailabilityResponse>();

        availability!.AvailableCopies.Should().Be(7);
    }

    [Fact]
    public async Task GetLendingHistory_ReturnsCorrectList()
    {
        // Arrange
        var grpcResponse = new GetUserLendingHistoryResponse();
        grpcResponse.History.Add(new UserLendingHistoryItem
        {
            Book = new BookResponse { Id = 5, Title = "History 101" },
            BorrowedDate = Timestamp.FromDateTime(DateTime.UtcNow)
        });

        SetupGrpcCall(
            c => c.GetUserLendingHistoryAsync(It.IsAny<GetUserLendingHistoryRequest>(), It.IsAny<CallOptions>()),
            grpcResponse);

        var start = DateTime.UtcNow.AddDays(-1).ToString("o");
        var end = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await _client.GetAsync($"/api/borrowers/1/history?startDate={start}&endDate={end}");
        await EnsureSuccessOrThrow(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<UserLendingHistoryItem>>();

        history.Should().HaveCount(1);
        history!.First().Book.Title.Should().Be("History 101");
    }

    [Fact]
    public async Task GetAlsoBorrowed_ReturnsRankedList()
    {
        // Arrange
        var grpcResponse = new GetAlsoBorrowedBooksResponse();
        grpcResponse.AlsoBorrowedBooks.Add(new AlsoBorrowedBookInfo
        {
            Book = new BookResponse { Id = 9, Title = "Related Book" },
            CommonBorrowersCount = 10
        });

        SetupGrpcCall(
            c => c.GetAlsoBorrowedBooksAsync(It.IsAny<GetAlsoBorrowedBooksRequest>(), It.IsAny<CallOptions>()),
            grpcResponse);

        // Act
        var response = await _client.GetAsync("/api/books/1/also-borrowed");
        await EnsureSuccessOrThrow(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<AlsoBorrowedBookInfo>>();

        list!.First().Book.Title.Should().Be("Related Book");
    }

    [Fact]
    public async Task GetReadingRate_ReturnsDoubleValue()
    {
        // Arrange
        var expectedResponse = new EstimateReadingRateResponse { PagesPerDay = 42.5 };

        SetupGrpcCall(c => c.EstimateReadingRateAsync(It.IsAny<EstimateReadingRateRequest>(), It.IsAny<CallOptions>()),
            expectedResponse);

        // Act
        var response = await _client.GetAsync("/api/books/1/reading-rate");
        await EnsureSuccessOrThrow(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rate = await response.Content.ReadFromJsonAsync<EstimateReadingRateResponse>();

        rate!.PagesPerDay.Should().Be(42.5);
    }

    // --- HELPERS ---

    // Helper to setup AsyncUnaryCall for gRPC mocks (Reduces boilerplate)
    private void SetupGrpcCall<TResponse>(
        Expression<Func<Library.LibraryClient, AsyncUnaryCall<TResponse>>> expression,
        TResponse returnObject)
    {
        var call = new AsyncUnaryCall<TResponse>(
            Task.FromResult(returnObject),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        _grpcMock.Setup(expression).Returns(call);
    }

    // Helper to throw a readable exception if the API returns 500 or 400 unexpected
    private async Task EnsureSuccessOrThrow(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"API returned {response.StatusCode}. Content: {content}");
        }
    }
}