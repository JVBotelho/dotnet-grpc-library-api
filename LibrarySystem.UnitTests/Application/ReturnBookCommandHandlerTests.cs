using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Lending.ReturnBook;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class ReturnBookCommandHandlerTests
{
    private readonly Mock<ILendingRepository> _lendingRepoMock;
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly ReturnBookCommandHandler _handler;

    public ReturnBookCommandHandlerTests()
    {
        _lendingRepoMock = new Mock<ILendingRepository>();
        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new ReturnBookCommandHandler(_lendingRepoMock.Object, _bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenLendingAndBookExist_ShouldReturnBook()
    {
        var book = new Book("Title", "Author", 2000, 100, 5);
        var borrower = new Borrower("John", "j@test.com");
        book.BorrowCopy(borrower);
        var lendingActivity = book.LendingActivities.First();

        _lendingRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lendingActivity);
        _bookRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var command = new ReturnBookCommand(lendingActivity.Id);
        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        result.ReturnedDate.Should().NotBeNull();
        lendingActivity.ReturnedDate.Should().NotBeNull();
        
        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLendingNotFound_ShouldThrowKeyNotFoundException()
    {
        _lendingRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LendingActivity?)null);

        var command = new ReturnBookCommand(1);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
    
    [Fact]
    public async Task Handle_WhenBookNotFound_ShouldThrowInvalidOperationException()
    {
        var book = new Book("Title", "Author", 2000, 100, 5);
        var borrower = new Borrower("John", "j@test.com");
        book.BorrowCopy(borrower);
        var lendingActivity = book.LendingActivities.First();

        _lendingRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lendingActivity);
            
        _bookRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        var command = new ReturnBookCommand(lendingActivity.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}

