using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Contracts.Protos;
using Moq;
using Xunit;

namespace LibrarySystem.IntegrationTests;

public class LendingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<Library.LibraryClient> _grpcMock;
    private readonly Fixture _fixture = new();

    public LendingControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _grpcMock = factory.LibraryClientMock;
        _grpcMock.Reset();
    }

    [Fact]
    public async Task CreateLending_WithValidData_ReturnsCreatedResponse()
    {
        // Arrange
        var createDto = _fixture.Create<CreateLendingDto>();
        var expectedResponse = new LendingActivityResponse
        {
            Id = 10,
            BookId = createDto.BookId,
            BorrowerId = createDto.BorrowerId,
            BorrowedDate = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        SetupGrpcCall(c => c.CreateLendingAsync(It.IsAny<CreateLendingRequest>(), It.IsAny<CallOptions>()), expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/lending", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var lending = await response.Content.ReadFromJsonAsync<LendingActivityResponse>();
        lending.Should().NotBeNull();
        lending!.Id.Should().Be(10);
        lending.BookId.Should().Be(createDto.BookId);
        
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task ReturnBook_WhenLendingExists_ReturnsOk()
    {
        // Arrange
        var lendingId = 123;
        var expectedResponse = new LendingActivityResponse
        {
            Id = lendingId,
            BookId = 1,
            BorrowerId = 1,
            BorrowedDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            ReturnedDate = Timestamp.FromDateTime(DateTime.UtcNow) // Returned now
        };

        SetupGrpcCall(c => c.ReturnBookAsync(It.IsAny<ReturnBookRequest>(), It.IsAny<CallOptions>()), expectedResponse);

        // Act
        var returnResponse = await _client.PutAsync($"/api/lending/{lendingId}/return", null);

        // Diagnostics
        if (!returnResponse.IsSuccessStatusCode)
        {
            var content = await returnResponse.Content.ReadAsStringAsync();
            throw new Exception($"API Failed with {returnResponse.StatusCode}: {content}");
        }

        // Assert
        returnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var returnedLending = await returnResponse.Content.ReadFromJsonAsync<LendingActivityResponse>();
        returnedLending.Should().NotBeNull();
        returnedLending!.ReturnedDate.Should().NotBeNull();
        returnedLending.Id.Should().Be(lendingId);
    }
    
    [Fact]
    public async Task ReturnBook_WhenAlreadyReturned_ReturnsBadRequest()
    {
        // Arrange
        var rpcException = new RpcException(new Status(StatusCode.FailedPrecondition, "Book already returned"));

        _grpcMock.Setup(x => x.ReturnBookAsync(It.IsAny<ReturnBookRequest>(), It.IsAny<CallOptions>()))
            .Throws(rpcException);

        // Act
        var response = await _client.PutAsync("/api/lending/1/return", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private void SetupGrpcCall<TResponse>(
        System.Linq.Expressions.Expression<Func<Library.LibraryClient, AsyncUnaryCall<TResponse>>> expression, 
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
}