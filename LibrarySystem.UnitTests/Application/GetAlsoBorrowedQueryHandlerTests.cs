using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Reports.GetAlsoBorrowed;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class GetAlsoBorrowedQueryHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly GetAlsoBorrowedQueryHandler _handler;

    public GetAlsoBorrowedQueryHandlerTests()
    {
        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new GetAlsoBorrowedQueryHandler(_bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlsoBorrowedBooks()
    {
        var books = new List<Book>
        {
            new Book("Title1", "Author1", 2021, 100, 5)
        };
        
        _bookRepoMock.Setup(x => x.GetAlsoBorrowedAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(books);

        var query = new GetAlsoBorrowedQuery(1, 5);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Title1");
    }
}
