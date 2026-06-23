using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Reports.GetAvailability;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class GetBookAvailabilityQueryHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly Mock<ILendingRepository> _lendingRepoMock;
    private readonly GetBookAvailabilityQueryHandler _handler;

    public GetBookAvailabilityQueryHandlerTests()
    {
        _bookRepoMock = new Mock<IBookRepository>();
        _lendingRepoMock = new Mock<ILendingRepository>();
        _handler = new GetBookAvailabilityQueryHandler(_bookRepoMock.Object, _lendingRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBookExists_ShouldReturnAvailability()
    {
        var book = new Book("T", "A", 2000, 300, 5);
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _lendingRepoMock.Setup(x => x.GetBorrowedCopiesCountAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var query = new GetBookAvailabilityQuery(1);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCopies.Should().Be(5);
        result.BorrowedCopies.Should().Be(2);
        result.AvailableCopies.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenBookNotFound_ShouldThrowKeyNotFoundException()
    {
        _bookRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var query = new GetBookAvailabilityQuery(1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None));
    }
}
