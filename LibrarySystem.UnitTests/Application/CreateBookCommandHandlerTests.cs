using AutoFixture;
using FluentAssertions;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Books.CreateBook;
using LibrarySystem.Domain.Entities;
using Moq;

namespace LibrarySystem.Application.UnitTests.Books;

public class CreateBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly Fixture _fixture;
    private readonly CreateBookCommandHandler _handler;

    public CreateBookCommandHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _bookRepoMock = new Mock<IBookRepository>();
        _handler = new CreateBookCommandHandler(_bookRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateBook_AndCallRepository()
    {
        // Arrange
        var command = _fixture.Create<CreateBookCommand>();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(command.Title);
        result.TotalCopies.Should().Be(command.TotalCopies);

        _bookRepoMock.Verify(x => x.AddAsync(
                It.Is<Book>(b => b.Title == command.Title && b.Author == command.Author),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _bookRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}