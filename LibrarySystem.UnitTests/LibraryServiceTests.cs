using AutoFixture;
using FluentAssertions;
using Grpc.Core;
using LibrarySystem.Application.DTOs;
using LibrarySystem.Application.UseCases.Books.CreateBook;
using LibrarySystem.Application.UseCases.Books.GetBookById;
using LibrarySystem.Application.UseCases.Lending.BorrowBook;
using LibrarySystem.Application.UseCases.Reports.GetMostBorrowed;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Grpc.Services;
using LibrarySystem.UnitTests.Helpers;
using MediatR;
using Moq;

namespace LibrarySystem.UnitTests;

public class LibraryServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<ISender> _senderMock;
    private readonly LibraryService _sut;

    public LibraryServiceTests()
    {
        _fixture = new Fixture();

        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _senderMock = new Mock<ISender>();

        _sut = new LibraryService(_senderMock.Object);
    }

    [Fact]
    public async Task GetBookById_WhenBookExists_ShouldReturnCorrectResponse()
    {
        // Arrange
        var bookId = 1;
        var bookDto = _fixture.Create<BookDto>();

        _senderMock
            .Setup(x => x.Send(It.Is<GetBookByIdQuery>(q => q.Id == bookId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookDto);

        var request = new GetBookByIdRequest { Id = bookId };

        // Act
        var response = await _sut.GetBookById(request, TestServerCallContext.Create());

        // Assert
        response.Should().NotBeNull();
        response.Title.Should().Be(bookDto.Title);
        response.Id.Should().Be(bookDto.Id);
    }

    [Fact]
    public async Task GetBookById_WhenBookDoesNotExist_ShouldThrowRpcException_NotFound()
    {
        // Arrange
        var bookId = 99;

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetBookByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookDto?)null);

        var request = new GetBookByIdRequest { Id = bookId };

        // Act & Assert
        var action = async () => await _sut.GetBookById(request, TestServerCallContext.Create());

        await action.Should().ThrowAsync<RpcException>()
            .Where(ex => ex.StatusCode == StatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBook_ShouldSendCommand_AndReturnResponse()
    {
        // Arrange
        var request = _fixture.Create<CreateBookRequest>();
        var expectedDto = _fixture.Create<BookDto>();

        _senderMock
            .Setup(x => x.Send(It.IsAny<CreateBookCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var response = await _sut.CreateBook(request, TestServerCallContext.Create());

        // Assert
        response.Should().NotBeNull();
        response.Title.Should().Be(expectedDto.Title);

        // Verify
        _senderMock.Verify(x => x.Send(
            It.Is<CreateBookCommand>(c =>
                c.Title == request.Title &&
                c.Author == request.Author),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLending_WhenDomainThrowsInvalidOperation_ShouldThrowRpcFailedPrecondition()
    {
        // Arrange
        var request = _fixture.Create<CreateLendingRequest>();

        _senderMock
            .Setup(x => x.Send(It.IsAny<BorrowBookCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No copies available"));

        // Act & Assert
        var action = async () => await _sut.CreateLending(request, TestServerCallContext.Create());

        await action.Should().ThrowAsync<RpcException>()
            .Where(ex => ex.StatusCode == StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task CreateLending_WhenBorrowerNotFound_ShouldThrowRpcNotFound()
    {
        // Arrange
        var request = _fixture.Create<CreateLendingRequest>();

        _senderMock
            .Setup(x => x.Send(It.IsAny<BorrowBookCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Borrower not found"));

        // Act & Assert
        var action = async () => await _sut.CreateLending(request, TestServerCallContext.Create());

        await action.Should().ThrowAsync<RpcException>()
            .Where(ex => ex.StatusCode == StatusCode.NotFound);
    }

    [Fact]
    public async Task GetMostBorrowedBooks_ShouldMapResponseCorrectly()
    {
        // Arrange
        var booksDto = _fixture.CreateMany<BookDto>(3).ToList();
        var request = new GetMostBorrowedBooksRequest { Count = 3 };

        _senderMock
            .Setup(x => x.Send(It.Is<GetMostBorrowedQuery>(q => q.Count == 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(booksDto);

        // Act
        var response = await _sut.GetMostBorrowedBooks(request, TestServerCallContext.Create());

        // Assert
        response.MostBorrowedBooks.Should().HaveCount(3);
        response.MostBorrowedBooks.First().Book.Title.Should().Be(booksDto.First().Title);
    }
}