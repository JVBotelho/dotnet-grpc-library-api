using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Reports.GetUserHistory;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class GetUserHistoryQueryHandlerTests
{
    private readonly Mock<ILendingRepository> _lendingRepoMock;
    private readonly GetUserHistoryQueryHandler _handler;

    public GetUserHistoryQueryHandlerTests()
    {
        _lendingRepoMock = new Mock<ILendingRepository>();
        _handler = new GetUserHistoryQueryHandler(_lendingRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserHistory()
    {
        var book = new Book("T", "A", 2000, 100, 5);
        typeof(Book).GetProperty("Id")?.SetValue(book, 10);
        
        var borrower = new Borrower("User", "user@email.com");
        book.BorrowCopy(borrower);
        var activity = book.LendingActivities.First();
        typeof(LendingActivity).GetProperty("Book")?.SetValue(activity, book);

        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;

        _lendingRepoMock.Setup(x => x.GetUserHistoryAsync(1, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LendingActivity> { activity });

        var query = new GetUserHistoryQuery(1, startDate, endDate);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Book.Title.Should().Be("T");
    }
}
