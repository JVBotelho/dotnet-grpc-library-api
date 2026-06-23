using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Reports.GetMostBorrowed;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class GetMostBorrowedQueryHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly GetMostBorrowedQueryHandler _handler;

    public GetMostBorrowedQueryHandlerTests()
    {
        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new GetMostBorrowedQueryHandler(_bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMostBorrowedBooks()
    {
        var books = new List<Book>
        {
            new Book("Title1", "Author1", 2021, 100, 5)
        };
        
        _bookRepoMock.Setup(x => x.GetMostBorrowedAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(books);

        var query = new GetMostBorrowedQuery(10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Title1");
    }
}
