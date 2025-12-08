using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetAlsoBorrowed;

public class GetAlsoBorrowedQueryHandler : IRequestHandler<GetAlsoBorrowedQuery, IEnumerable<BookDto>>
{
    private readonly IBookRepository _bookRepository;

    public GetAlsoBorrowedQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<IEnumerable<BookDto>> Handle(GetAlsoBorrowedQuery request, CancellationToken cancellationToken)
    {
        var books = await _bookRepository.GetAlsoBorrowedAsync(request.BookId, request.Count, cancellationToken);

        return books.Select(b => new BookDto(
            b.Id, b.Title, b.Author, b.PublicationYear, b.Pages, b.TotalCopies, 0
        ));
    }
}