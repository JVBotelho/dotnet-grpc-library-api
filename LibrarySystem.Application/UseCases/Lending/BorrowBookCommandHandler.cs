using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Lending.BorrowBook;

public class BorrowBookCommandHandler : IRequestHandler<BorrowBookCommand, LendingDto>
{
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowerRepository _borrowerRepository;

    public BorrowBookCommandHandler(
        IBookRepository bookRepository, 
        IBorrowerRepository borrowerRepository)
    {
        _bookRepository = bookRepository;
        _borrowerRepository = borrowerRepository;
    }

    public async Task<LendingDto> Handle(BorrowBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book is null) 
            throw new KeyNotFoundException($"Book with ID {request.BookId} not found.");

        var borrower = await _borrowerRepository.GetByIdAsync(request.BorrowerId, cancellationToken);
        if (borrower is null) 
            throw new KeyNotFoundException($"Borrower with ID {request.BorrowerId} not found.");

        var lendingActivity = book.BorrowCopy(borrower);

        await _bookRepository.SaveChangesAsync(cancellationToken);

        return new LendingDto(
            lendingActivity.Id,
            lendingActivity.BookId,
            lendingActivity.BorrowerId,
            lendingActivity.BorrowedDate,
            lendingActivity.ReturnedDate
        );
    }
}