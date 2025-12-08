using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Abstractions.Repositories;

public interface ILendingRepository
{
    Task<LendingActivity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<int> GetBorrowedCopiesCountAsync(int bookId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LendingActivity>> GetTopBorrowersAsync(DateTime start, DateTime end, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<LendingActivity>> GetUserHistoryAsync(int borrowerId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IEnumerable<LendingActivity>> GetByBookIdAsync(int bookId, CancellationToken cancellationToken = default);
}