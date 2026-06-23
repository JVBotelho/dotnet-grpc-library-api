using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Books.GetAll;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.UnitTests.Application;

public class GetAllBooksQueryHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly GetAllBooksQueryHandler _handler;

    public GetAllBooksQueryHandlerTests()
    {
        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new GetAllBooksQueryHandler(_bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllBooksAsDtos()
    {
        var books = new List<Book>
        {
            new Book("Title1", "Author1", 2021, 100, 5),
            new Book("Title2", "Author2", 2022, 200, 3)
        };
        
        _bookRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(books);

        var query = new GetAllBooksQuery();
        var result = await _handler.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Title1");
    }
}

