using LibrarySystem.Application.Abstractions.Repositories;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetTopBorrowers;

public class GetTopBorrowersQueryHandler : IRequestHandler<GetTopBorrowersQuery, IEnumerable<TopBorrowerDto>>
{
    private readonly ILendingRepository _lendingRepository;

    public GetTopBorrowersQueryHandler(ILendingRepository lendingRepository)
    {
        _lendingRepository = lendingRepository;
    }

    public async Task<IEnumerable<TopBorrowerDto>> Handle(GetTopBorrowersQuery request, CancellationToken cancellationToken)
    {
        var activities = await _lendingRepository.GetTopBorrowersAsync(
            request.StartDate, 
            request.EndDate, 
            request.Count, 
            cancellationToken
        );

        return activities
            .GroupBy(la => la.Borrower)
            .Where(g => g.Key != null)
            .Select(g => new TopBorrowerDto(
                BorrowerId: g.Key!.Id,
                Name: g.Key.Name,
                BorrowCount: g.Count()
            ))
            .OrderByDescending(x => x.BorrowCount)
            .Take(request.Count);
    }
}