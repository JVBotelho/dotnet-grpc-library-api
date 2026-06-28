using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Books.Delete;
using LibrarySystem.Domain.Entities;
using MediatR;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class DeleteBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly DeleteBookCommandHandler _handler;

    public DeleteBookCommandHandlerTests()
    {
        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new DeleteBookCommandHandler(_bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBookExists_ShouldRemoveAndSaveChanges()
    {
        var book = new Book("Title", "Author", 2020, 100, 5);
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var command = new DeleteBookCommand(1);
        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().Be(Unit.Value);
        _bookRepoMock.Verify(x => x.Remove(book), Times.Once);
        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBookNotFound_ShouldThrowKeyNotFoundException()
    {
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        var command = new DeleteBookCommand(1);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _bookRepoMock.Verify(x => x.Remove(It.IsAny<Book>()), Times.Never);
        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

