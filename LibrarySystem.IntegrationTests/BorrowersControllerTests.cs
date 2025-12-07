using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Grpc.Core;
using LibrarySystem.Contracts.Protos;
using Moq;

namespace LibrarySystem.IntegrationTests;

public class BorrowersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<Library.LibraryClient> _grpcMock;
    private readonly Fixture _fixture = new();

    public BorrowersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _grpcMock = factory.LibraryClientMock;
        _grpcMock.Reset();
    }

    [Fact]
    public async Task GetTopBorrowers_ReturnsCorrectlyOrderedBorrowers()
    {
        // Arrange
        var grpcResponse = new GetTopBorrowersResponse();

        grpcResponse.TopBorrowers.Add(new TopBorrowerInfo
        {
            Borrower = new BorrowerResponse { Id = 101, Name = "Most Active User" },
            BorrowCount = 10
        });
        
        grpcResponse.TopBorrowers.Add(new TopBorrowerInfo
        {
            Borrower = new BorrowerResponse { Id = 102, Name = "Less Active User" },
            BorrowCount = 2
        });

        SetupGrpcCall(c => c.GetTopBorrowersAsync(It.IsAny<GetTopBorrowersRequest>(), It.IsAny<CallOptions>()), grpcResponse);

        var startDate = DateTime.UtcNow.AddDays(-1).ToString("o");
        var endDate = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await _client.GetAsync($"/api/borrowers/most-active?startDate={startDate}&endDate={endDate}&count=2");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Failed: {response.StatusCode} - {error}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var topBorrowers = await response.Content.ReadFromJsonAsync<List<TopBorrowerInfo>>();

        topBorrowers.Should().NotBeNull().And.HaveCount(2);
        
        topBorrowers!.First().Borrower.Id.Should().Be(101);
        topBorrowers.First().BorrowCount.Should().Be(10);
        
        topBorrowers.Last().Borrower.Id.Should().Be(102);
        topBorrowers.Last().BorrowCount.Should().Be(2);
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