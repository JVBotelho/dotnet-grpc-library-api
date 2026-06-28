using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Api.Controllers;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Contracts.Protos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LibrarySystem.UnitTests.Api.Controllers;

public class BooksControllerTests
{
    private readonly Mock<Library.LibraryClient> _grpcClientMock;
    private readonly BooksController _sut;
    private readonly IFixture _fixture;

    public BooksControllerTests()
    {
        _grpcClientMock = new Mock<Library.LibraryClient>();
        _sut = new BooksController(_grpcClientMock.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetBook_WhenBookExists_ReturnsOkResult()
    {
        var id = 1;
        var reply = _fixture.Create<BookResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);
        
        _grpcClientMock.Setup(x => x.GetBookByIdAsync(It.Is<GetBookByIdRequest>(r => r.Id == id), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetBook(id);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply);
    }

    [Fact]
    public async Task GetBook_WhenBookDoesNotExist_ReturnsNotFound()
    {
        var id = 1;
        var exception = new RpcException(new Status(StatusCode.NotFound, "Not Found"));
        
        _grpcClientMock.Setup(x => x.GetBookByIdAsync(It.Is<GetBookByIdRequest>(r => r.Id == id), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.GetBook(id);
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Book not found id: {id}");
    }

    [Fact]
    public async Task GetBook_WhenUnexpectedError_Returns500()
    {
        var id = 1;
        var exception = new Exception("Some error");
        
        _grpcClientMock.Setup(x => x.GetBookByIdAsync(It.Is<GetBookByIdRequest>(r => r.Id == id), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.GetBook(id);
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be($"Error communicating with gRPC service: {exception.Message}");
    }

    [Fact]
    public async Task CreateBook_ValidInput_ReturnsCreatedAtAction()
    {
        var dto = _fixture.Create<CreateBookDto>();
        var reply = _fixture.Create<BookResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.CreateBookAsync(It.IsAny<CreateBookRequest>(), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.CreateBook(dto);
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(BooksController.GetBook));
        createdResult.RouteValues!["id"].Should().Be(reply.Id);
        createdResult.Value.Should().BeEquivalentTo(reply);
    }

    [Fact]
    public async Task GetAllBooks_ReturnsOkResult()
    {
        var reply = _fixture.Create<GetAllBooksResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.GetAllBooksAsync(It.IsAny<Empty>(), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetAllBooks();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply.Books);
    }

    [Fact]
    public async Task UpdateBook_WhenBookExists_ReturnsOkResult()
    {
        var id = 1;
        var dto = _fixture.Create<UpdateBookDto>();
        var reply = _fixture.Create<BookResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.UpdateBookAsync(It.IsAny<UpdateBookRequest>(), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.UpdateBook(id, dto);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply);
    }

    [Fact]
    public async Task UpdateBook_WhenBookDoesNotExist_ReturnsNotFound()
    {
        var id = 1;
        var dto = _fixture.Create<UpdateBookDto>();
        var exception = new RpcException(new Status(StatusCode.NotFound, "Not Found"));

        _grpcClientMock.Setup(x => x.UpdateBookAsync(It.IsAny<UpdateBookRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.UpdateBook(id, dto);
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Not Found");
    }

    [Fact]
    public async Task DeleteBook_WhenBookExists_ReturnsNoContent()
    {
        var id = 1;
        var reply = new Empty();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.DeleteBookAsync(It.IsAny<DeleteBookRequest>(), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.DeleteBook(id);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteBook_WhenBookDoesNotExist_ReturnsNotFound()
    {
        var id = 1;
        var exception = new RpcException(new Status(StatusCode.NotFound, "Not Found"));

        _grpcClientMock.Setup(x => x.DeleteBookAsync(It.IsAny<DeleteBookRequest>(), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.DeleteBook(id);
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Not Found");
    }

    [Fact]
    public async Task GetMostBorrowed_ValidCount_ReturnsOkResult()
    {
        var count = 5;
        var reply = _fixture.Create<GetMostBorrowedBooksResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.GetMostBorrowedBooksAsync(It.Is<GetMostBorrowedBooksRequest>(r => r.Count == count), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetMostBorrowed(count);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply.MostBorrowedBooks);
    }

    [Fact]
    public async Task GetMostBorrowed_InvalidCount_ReturnsBadRequest()
    {
        var result = await _sut.GetMostBorrowed(0);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Count must be a positive number.");
    }

    [Fact]
    public async Task GetBookAvailability_WhenBookExists_ReturnsOkResult()
    {
        var id = 1;
        var reply = _fixture.Create<BookAvailabilityResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.GetBookAvailabilityAsync(It.Is<GetBookAvailabilityRequest>(r => r.BookId == id), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetBookAvailability(id);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply);
    }

    [Fact]
    public async Task GetBookAvailability_WhenRpcException_ReturnsNotFound()
    {
        var id = 1;
        var exception = new RpcException(new Status(StatusCode.NotFound, "Not Found"));

        _grpcClientMock.Setup(x => x.GetBookAvailabilityAsync(It.Is<GetBookAvailabilityRequest>(r => r.BookId == id), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.GetBookAvailability(id);
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Not Found");
    }

    [Fact]
    public async Task GetAlsoBorrowed_ValidInput_ReturnsOkResult()
    {
        var id = 1;
        var count = 5;
        var reply = _fixture.Create<GetAlsoBorrowedBooksResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.GetAlsoBorrowedBooksAsync(It.Is<GetAlsoBorrowedBooksRequest>(r => r.BookId == id && r.Count == count), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetAlsoBorrowed(id, count);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply.AlsoBorrowedBooks);
    }

    [Fact]
    public async Task GetAlsoBorrowed_InvalidCount_ReturnsBadRequest()
    {
        var result = await _sut.GetAlsoBorrowed(1, 0);
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Count must be a positive number.");
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenRpcException_Returns500()
    {
        var id = 1;
        var count = 5;
        var exception = new RpcException(new Status(StatusCode.Internal, "Internal Error"));

        _grpcClientMock.Setup(x => x.GetAlsoBorrowedBooksAsync(It.Is<GetAlsoBorrowedBooksRequest>(r => r.BookId == id && r.Count == count), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.GetAlsoBorrowed(id, count);
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be($"An error occurred: {exception.Status.Detail}");
    }

    [Fact]
    public async Task GetReadingRate_WhenBookExists_ReturnsOkResult()
    {
        var id = 1;
        var reply = _fixture.Create<EstimateReadingRateResponse>();
        var call = GrpcTestHelper.CreateAsyncUnaryCall(reply);

        _grpcClientMock.Setup(x => x.EstimateReadingRateAsync(It.Is<EstimateReadingRateRequest>(r => r.BookId == id), null, null, CancellationToken.None))
            .Returns(call);
        var result = await _sut.GetReadingRate(id);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(reply);
    }

    [Fact]
    public async Task GetReadingRate_WhenBookDoesNotExist_ReturnsNotFound()
    {
        var id = 1;
        var exception = new RpcException(new Status(StatusCode.NotFound, "Not Found"));

        _grpcClientMock.Setup(x => x.EstimateReadingRateAsync(It.Is<EstimateReadingRateRequest>(r => r.BookId == id), null, null, CancellationToken.None))
            .Throws(exception);
        var result = await _sut.GetReadingRate(id);
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Not Found");
    }
}

