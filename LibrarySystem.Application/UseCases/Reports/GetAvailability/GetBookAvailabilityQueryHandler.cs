using LibrarySystem.Application.Abstractions.Repositories;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetAvailability;

public class GetBookAvailabilityQueryHandler : IRequestHandler<GetBookAvailabilityQuery, BookAvailabilityDto>
{
    private readonly IBookRepository _bookRepository;
    private readonly ILendingRepository _lendingRepository;

    public GetBookAvailabilityQueryHandler(IBookRepository bookRepository, ILendingRepository lendingRepository)
    {
        _bookRepository = bookRepository;
        _lendingRepository = lendingRepository;
    }

    public async Task<BookAvailabilityDto> Handle(GetBookAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);

        if (book is null)
            throw new KeyNotFoundException($"Book with ID {request.BookId} not found.");

        var borrowedCount = await _lendingRepository.GetBorrowedCopiesCountAsync(request.BookId, cancellationToken);

        return new BookAvailabilityDto(
            book.TotalCopies,
            borrowedCount,
            book.TotalCopies - borrowedCount
        );
    }
}