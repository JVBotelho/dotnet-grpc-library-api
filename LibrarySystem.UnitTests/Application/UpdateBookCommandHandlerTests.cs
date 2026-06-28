using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Books.Update;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class UpdateBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly UpdateBookCommandHandler _handler;

    public UpdateBookCommandHandlerTests()
    {
        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new UpdateBookCommandHandler(_bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBookExists_ShouldUpdateAndSaveChanges()
    {
        var book = new Book("Old Title", "Old Author", 2000, 100, 5);
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var command = new UpdateBookCommand(1, "New Title", "New Author", 2023, 200, 10);
        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        result.Author.Should().Be("New Author");
        book.Title.Should().Be("New Title");

        _bookRepoMock.Verify(x => x.Update(book), Times.Once);
        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBookNotFound_ShouldThrowKeyNotFoundException()
    {
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        var command = new UpdateBookCommand(1, "New Title", "New Author", 2023, 200, 10);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _bookRepoMock.Verify(x => x.Update(It.IsAny<Book>()), Times.Never);
        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

