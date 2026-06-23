using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Grpc.Core;
using LibrarySystem.Api.Controllers;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Contracts.Protos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LibrarySystem.UnitTests.Api.Controllers;

public class LendingControllerTests
{
    private readonly Mock<Library.LibraryClient> _grpcClientMock;
    private readonly LendingController _sut;
    private readonly IFixture _fixture;

    public LendingControllerTests()
    {
        _grpcClientMock = new Mock<Library.LibraryClient>();
        _sut = new LendingController(_grpcClientMock.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task CreateLending_ValidInput_ReturnsCreatedAtAction()
    {
        var dto = _fixture.Create<CreateLendingDto>();
        var reply = _fixture.Create<LendingActivityResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.CreateLendingAsync(It.Is<CreateLendingRequest>(r => r.BookId == dto.BookId && r.BorrowerId == dto.BorrowerId), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.CreateLending(dto);
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(LendingController.CreateLending));
        createdResult.RouteValues!["id"].Should().Be(reply.Id);
        createdResult.Value.Should().BeEquivalentTo(reply);
    }

    [Fact]
    public async Task CreateLending_WhenNotFoundException_ReturnsBadRequest()
    {
        var dto = _fixture.Create<CreateLendingDto>();
        var exception = new RpcException(new Status(StatusCode.NotFound, "Not Found"));

        _grpcClientMock.Setup(x => x.CreateLendingAsync(It.IsAny<CreateLendingRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.CreateLending(dto);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Not Found");
    }

    [Fact]
    public async Task CreateLending_WhenFailedPreconditionException_ReturnsBadRequest()
    {
        var dto = _fixture.Create<CreateLendingDto>();
        var exception = new RpcException(new Status(StatusCode.FailedPrecondition, "Failed Precondition"));

        _grpcClientMock.Setup(x => x.CreateLendingAsync(It.IsAny<CreateLendingRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.CreateLending(dto);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Failed Precondition");
    }

    [Fact]
    public async Task ReturnBook_ValidInput_ReturnsOkResult()
    {
        var id = 1;
        var reply = _fixture.Create<LendingActivityResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.ReturnBookAsync(It.Is<ReturnBookRequest>(r => r.LendingActivityId == id), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.ReturnBook(id);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply);
    }

    [Fact]
    public async Task ReturnBook_WhenNotFoundException_ReturnsBadRequest()
    {
        var id = 1;
        var exception = new RpcException(new Status(StatusCode.NotFound, "Not Found"));

        _grpcClientMock.Setup(x => x.ReturnBookAsync(It.IsAny<ReturnBookRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.ReturnBook(id);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Not Found");
    }

    [Fact]
    public async Task ReturnBook_WhenFailedPreconditionException_ReturnsBadRequest()
    {
        var id = 1;
        var exception = new RpcException(new Status(StatusCode.FailedPrecondition, "Failed Precondition"));

        _grpcClientMock.Setup(x => x.ReturnBookAsync(It.IsAny<ReturnBookRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.ReturnBook(id);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Failed Precondition");
    }
}

