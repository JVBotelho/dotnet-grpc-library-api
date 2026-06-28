using MediatR;
using LibrarySystem.Application.Abstractions.Repositories;

namespace LibrarySystem.Application.UseCases.Kiosk;

public record ValidateMemberResult(bool Valid, int BorrowerId, string DisplayName, string Reason);

public record ValidateMemberQuery(string DeviceId, string CardUid) : IRequest<ValidateMemberResult>;

public class ValidateMemberQueryHandler : IRequestHandler<ValidateMemberQuery, ValidateMemberResult>
{
    private readonly IBorrowerRepository _borrowerRepository;

    public ValidateMemberQueryHandler(IBorrowerRepository borrowerRepository)
    {
        _borrowerRepository = borrowerRepository;
    }

    public async Task<ValidateMemberResult> Handle(ValidateMemberQuery request, CancellationToken cancellationToken)
    {
        var borrower = await _borrowerRepository.GetByCardUidAsync(request.CardUid, cancellationToken);
        if (borrower != null)
        {
            return new ValidateMemberResult(true, borrower.Id, borrower.Name, string.Empty);
        }
        
        return new ValidateMemberResult(false, 0, string.Empty, "Invalid Card UID or Borrower not found");
    }
}
