using AutoFixture;
using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Books.GetBookById;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.Application.UnitTests.Books;

public class GetBookByIdQueryHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly Fixture _fixture;
    private readonly GetBookByIdQueryHandler _handler;

    public GetBookByIdQueryHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new GetBookByIdQueryHandler(_bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBookExists_ShouldReturnDto()
    {
        // Arrange
        var book = new Book("Title", "Author", 2020, 100, 5); 
        typeof(Book).GetProperty(nameof(Book.Id))!.SetValue(book, 10);

        _bookRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        var query = new GetBookByIdQuery(10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        result.Title.Should().Be("Title");
    }

    [Fact]
    public async Task Handle_WhenBookDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _bookRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        var query = new GetBookByIdQuery(99);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}