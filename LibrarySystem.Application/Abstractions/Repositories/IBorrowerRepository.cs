using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Abstractions.Repositories;

public interface IBorrowerRepository
{
    Task<Borrower?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}