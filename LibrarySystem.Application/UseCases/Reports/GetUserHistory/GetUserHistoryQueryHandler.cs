using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetUserHistory;

public class GetUserHistoryQueryHandler : IRequestHandler<GetUserHistoryQuery, IEnumerable<LendingHistoryDto>>
{
    private readonly ILendingRepository _lendingRepository;

    public GetUserHistoryQueryHandler(ILendingRepository lendingRepository)
    {
        _lendingRepository = lendingRepository;
    }

    public async Task<IEnumerable<LendingHistoryDto>> Handle(GetUserHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _lendingRepository.GetUserHistoryAsync(
            request.BorrowerId, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken
        );

        return history.Select(h => new LendingHistoryDto(
            new BookDto(h.Book!.Id, h.Book.Title, h.Book.Author, h.Book.PublicationYear, h.Book.Pages, h.Book.TotalCopies, 0),
            h.BorrowedDate,
            h.ReturnedDate
        ));
    }
}