using LibrarySystem.Application.Abstractions.Repositories;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.EstimateReading;

public class EstimateReadingRateQueryHandler : IRequestHandler<EstimateReadingRateQuery, double>
{
    private readonly ILendingRepository _lendingRepository;
    private readonly IBookRepository _bookRepository;

    public EstimateReadingRateQueryHandler(ILendingRepository lendingRepository, IBookRepository bookRepository)
    {
        _lendingRepository = lendingRepository;
        _bookRepository = bookRepository;
    }

    public async Task<double> Handle(EstimateReadingRateQuery request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book is null) 
            throw new KeyNotFoundException($"Book with ID {request.BookId} not found.");

        var loans = await _lendingRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        
        var completedLoans = loans.Where(l => l.ReturnedDate.HasValue).ToList();

        if (!completedLoans.Any()) return 0;

        double totalDays = completedLoans.Sum(la => (la.ReturnedDate!.Value - la.BorrowedDate).TotalDays);
        
        if (totalDays < 1) totalDays = 1;

        return (completedLoans.Count * book.Pages) / totalDays;
    }
}