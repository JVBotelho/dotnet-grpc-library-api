using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Abstractions.Repositories;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
