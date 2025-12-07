using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetTopBorrowers;

public record TopBorrowerDto(int BorrowerId, string Name, int BorrowCount);

public record GetTopBorrowersQuery(DateTime StartDate, DateTime EndDate, int Count) 
    : IRequest<IEnumerable<TopBorrowerDto>>;