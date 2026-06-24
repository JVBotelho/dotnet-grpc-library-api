using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Persistence.Repositories;

public class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly LibraryDbContext _context;

    public ProcessedEventRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _context.ProcessedEvents.AnyAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
    {
        await _context.ProcessedEvents.AddAsync(processedEvent, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
