using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Lending.ReturnBook;

public class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, LendingDto>
{
    private readonly ILendingRepository _lendingRepository;
    private readonly IBookRepository _bookRepository;

    public ReturnBookCommandHandler(ILendingRepository lendingRepository, IBookRepository bookRepository)
    {
        _lendingRepository = lendingRepository;
        _bookRepository = bookRepository;
    }

    public async Task<LendingDto> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        var lending = await _lendingRepository.GetByIdAsync(request.LendingActivityId, cancellationToken);
        
        if (lending is null) 
            throw new KeyNotFoundException($"Lending Activity {request.LendingActivityId} not found.");

        var book = await _bookRepository.GetByIdAsync(lending.BookId, cancellationToken);
        
        if (book is null) 
            throw new InvalidOperationException("The book associated with this lending record could not be found.");

        book.ReturnCopy(request.LendingActivityId);

        await _bookRepository.SaveChangesAsync(cancellationToken);

        return new LendingDto(
            lending.Id,
            lending.BookId,
            lending.BorrowerId,
            lending.BorrowedDate,
            lending.ReturnedDate
        );
    }
}