using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.GetAll;

public class GetAllBooksQueryHandler : IRequestHandler<GetAllBooksQuery, IEnumerable<BookDto>>
{
    private readonly IBookRepository _bookRepository;

    public GetAllBooksQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<IEnumerable<BookDto>> Handle(GetAllBooksQuery request, CancellationToken cancellationToken)
    {
        var books = await _bookRepository.GetAllAsync(cancellationToken);

        return books.Select(b => new BookDto(
            b.Id,
            b.Title,
            b.Author,
            b.PublicationYear,
            b.Pages,
            b.TotalCopies,
            b.TotalCopies - b.LendingActivities.Count(la => la.ReturnedDate == null)
        ));
    }
}