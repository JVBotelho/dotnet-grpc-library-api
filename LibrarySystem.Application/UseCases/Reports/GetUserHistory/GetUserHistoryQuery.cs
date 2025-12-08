using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetUserHistory;

public record LendingHistoryDto(BookDto Book, DateTime BorrowedDate, DateTime? ReturnedDate);

public record GetUserHistoryQuery(int BorrowerId, DateTime StartDate, DateTime EndDate)
    : IRequest<IEnumerable<LendingHistoryDto>>;