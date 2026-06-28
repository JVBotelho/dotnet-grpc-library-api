using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Api.Controllers;
using LibrarySystem.Contracts.Protos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LibrarySystem.UnitTests.Api.Controllers;

public class BorrowersControllerTests
{
    private readonly Mock<Library.LibraryClient> _grpcClientMock;
    private readonly BorrowersController _sut;
    private readonly IFixture _fixture;

    public BorrowersControllerTests()
    {
        _grpcClientMock = new Mock<Library.LibraryClient>();
        _sut = new BorrowersController(_grpcClientMock.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetTopBorrowers_ValidInput_ReturnsOkResult()
    {
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var count = 5;
        var reply = _fixture.Create<GetTopBorrowersResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.GetTopBorrowersAsync(It.Is<GetTopBorrowersRequest>(r => r.Count == count), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetTopBorrowers(startDate, endDate, count);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply.TopBorrowers);
    }

    [Fact]
    public async Task GetTopBorrowers_InvalidDates_ReturnsBadRequest()
    {
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-7);
        var count = 5;
        var result = await _sut.GetTopBorrowers(startDate, endDate, count);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid query parameters. Ensure startDate is before endDate and count is positive.");
    }

    [Fact]
    public async Task GetTopBorrowers_InvalidCount_ReturnsBadRequest()
    {
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var count = 0;
        var result = await _sut.GetTopBorrowers(startDate, endDate, count);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid query parameters. Ensure startDate is before endDate and count is positive.");
    }

    [Fact]
    public async Task GetTopBorrowers_WhenRpcException_Returns500()
    {
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var count = 5;
        var exception = new RpcException(new Status(StatusCode.Internal, "Internal Error"));

        _grpcClientMock.Setup(x => x.GetTopBorrowersAsync(It.IsAny<GetTopBorrowersRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.GetTopBorrowers(startDate, endDate, count);
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while communicating with the service.");
    }

    [Fact]
    public async Task GetLendingHistory_ValidInput_ReturnsOkResult()
    {
        var id = 1;
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var reply = _fixture.Create<GetUserLendingHistoryResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.GetUserLendingHistoryAsync(It.Is<GetUserLendingHistoryRequest>(r => r.BorrowerId == id), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetLendingHistory(id, startDate, endDate);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply.History);
    }

    [Fact]
    public async Task GetLendingHistory_InvalidDates_ReturnsBadRequest()
    {
        var id = 1;
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-7);
        var result = await _sut.GetLendingHistory(id, startDate, endDate);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Start date must be before end date.");
    }

    [Fact]
    public async Task GetLendingHistory_WhenRpcException_Returns500()
    {
        var id = 1;
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var exception = new RpcException(new Status(StatusCode.Internal, "Internal Error"));

        _grpcClientMock.Setup(x => x.GetUserLendingHistoryAsync(It.IsAny<GetUserLendingHistoryRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.GetLendingHistory(id, startDate, endDate);
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while communicating with the service.");
    }
}

