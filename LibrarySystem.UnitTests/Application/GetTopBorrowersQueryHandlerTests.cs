using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Reports.GetTopBorrowers;
using LibrarySystem.Domain.Entities;
using Moq;
using System.Reflection;

namespace LibrarySystem.UnitTests.Application;

public class GetTopBorrowersQueryHandlerTests
{
    private readonly Mock<ILendingRepository> _lendingRepoMock;
    private readonly GetTopBorrowersQueryHandler _handler;

    public GetTopBorrowersQueryHandlerTests()
    {
        _lendingRepoMock = new Mock<ILendingRepository>();
        _handler = new GetTopBorrowersQueryHandler(_lendingRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTopBorrowers()
    {
        var book = new Book("T", "A", 2000, 100, 5);
        var borrower1 = new Borrower("User1", "user1@email.com");
        var borrower2 = new Borrower("User2", "user2@email.com");

        typeof(Borrower).GetProperty("Id")?.SetValue(borrower1, 1);
        typeof(Borrower).GetProperty("Id")?.SetValue(borrower2, 2);
        
        book.BorrowCopy(borrower1);
        book.BorrowCopy(borrower1);
        book.BorrowCopy(borrower2);

        var activities = book.LendingActivities.ToList();
        
        typeof(LendingActivity).GetProperty("Borrower")?.SetValue(activities[0], borrower1);
        typeof(LendingActivity).GetProperty("Borrower")?.SetValue(activities[1], borrower1);
        typeof(LendingActivity).GetProperty("Borrower")?.SetValue(activities[2], borrower2);

        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;

        _lendingRepoMock.Setup(x => x.GetTopBorrowersAsync(startDate, endDate, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        var query = new GetTopBorrowersQuery(startDate, endDate, 5);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        var list = result.ToList();
        list.Should().HaveCount(2);
        list[0].BorrowCount.Should().Be(2); 
        list[0].Name.Should().Be("User1");
        list[1].BorrowCount.Should().Be(1);
    }
}
