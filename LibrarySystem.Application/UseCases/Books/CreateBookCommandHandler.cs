using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using LibrarySystem.Domain.Entities;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.CreateBook;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, BookDto>
{
    private readonly IBookRepository _bookRepository;

    public CreateBookCommandHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookDto> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new Book(
            request.Title,
            request.Author,
            request.PublicationYear,
            request.Pages,
            request.TotalCopies
        );

        await _bookRepository.AddAsync(book, cancellationToken);
        await _bookRepository.SaveChangesAsync(cancellationToken);

        return new BookDto(
            book.Id,
            book.Title,
            book.Author,
            book.PublicationYear,
            book.Pages,
            book.TotalCopies,
            book.TotalCopies
        );
    }
}