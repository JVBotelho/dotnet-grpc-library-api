using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.GetBookById;

public class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, BookDto?>
{
    private readonly IBookRepository _bookRepository;

    public GetBookByIdQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookDto?> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (book is null) return null;

        return new BookDto(
            book.Id, 
            book.Title, 
            book.Author, 
            book.PublicationYear, 
            book.Pages, 
            book.TotalCopies,
            book.TotalCopies - book.LendingActivities.Count(x => x.ReturnedDate == null) 
        );
    }
}