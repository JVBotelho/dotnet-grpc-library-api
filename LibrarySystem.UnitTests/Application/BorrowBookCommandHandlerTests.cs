using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Lending.BorrowBook;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.Application.UnitTests.Lending;

public class BorrowBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly Mock<IBorrowerRepository> _borrowerRepoMock;
    private readonly BorrowBookCommandHandler _handler;

    public BorrowBookCommandHandlerTests()
    {
        _bookRepoMock = new Mock<IBookRepository>();
        _borrowerRepoMock = new Mock<IBorrowerRepository>();
        _handler = new BorrowBookCommandHandler(_bookRepoMock.Object, _borrowerRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBookAndBorrowerExist_ShouldBorrowAndSaveChanges()
    {
        // Arrange
        var book = new Book("Clean Code", "Uncle Bob", 2008, 400, 5);
        var borrower = new Borrower("John Doe", "john@email.com");

        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _borrowerRepoMock.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(borrower);

        var command = new BorrowBookCommand(1, 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BookId.Should().Be(book.Id);
        result.BorrowerId.Should().Be(borrower.Id);

        book.LendingActivities.Should().HaveCount(1);

        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBookNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _bookRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        var command = new BorrowBookCommand(1, 2);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBorrowerNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var book = new Book("Title", "Author", 2000, 100, 1);
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(book);

        _borrowerRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Borrower?)null);

        var command = new BorrowBookCommand(1, 2);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}