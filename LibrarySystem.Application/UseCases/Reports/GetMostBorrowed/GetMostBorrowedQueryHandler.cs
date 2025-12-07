using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetMostBorrowed;

public class GetMostBorrowedQueryHandler : IRequestHandler<GetMostBorrowedQuery, IEnumerable<BookDto>>
{
    private readonly IBookRepository _bookRepository;

    public GetMostBorrowedQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<IEnumerable<BookDto>> Handle(GetMostBorrowedQuery request, CancellationToken cancellationToken)
    {
        var books = await _bookRepository.GetMostBorrowedAsync(request.Count, cancellationToken);

        return books.Select(b => new BookDto(
            b.Id, b.Title, b.Author, b.PublicationYear, b.Pages, b.TotalCopies, 0
        ));
    }
}