using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Reports.EstimateReading;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class EstimateReadingRateQueryHandlerTests
{
    private readonly Mock<ILendingRepository> _lendingRepoMock;
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly EstimateReadingRateQueryHandler _handler;

    public EstimateReadingRateQueryHandlerTests()
    {
        _lendingRepoMock = new Mock<ILendingRepository>();
        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new EstimateReadingRateQueryHandler(_lendingRepoMock.Object, _bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBookNotFound_ShouldThrowKeyNotFoundException()
    {
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        var query = new EstimateReadingRateQuery(1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNoCompletedLoans_ShouldReturnZero()
    {
        var book = new Book("T", "A", 2000, 300, 1);
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        
        _lendingRepoMock.Setup(x => x.GetByBookIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LendingActivity>());

        var query = new EstimateReadingRateQuery(1);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenHasCompletedLoans_ShouldCalculateRate()
    {
        var book = new Book("T", "A", 2000, 300, 2);
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        
        var borrower = new Borrower("User", "user@email.com");
        book.BorrowCopy(borrower);
        var loan = book.LendingActivities.First();
        
        book.ReturnCopy(loan.Id);
        
        _lendingRepoMock.Setup(x => x.GetByBookIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book.LendingActivities.ToList());

        var query = new EstimateReadingRateQuery(1);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().Be(300); // 1 loan * 300 pages / max(1 day, ~0) = 300
    }
}
