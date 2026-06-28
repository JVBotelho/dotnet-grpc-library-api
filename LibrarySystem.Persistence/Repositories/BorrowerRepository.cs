using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Persistence.Repositories;

internal sealed class BorrowerRepository : IBorrowerRepository
{
    private readonly LibraryDbContext _context;

    public BorrowerRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Borrower?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Borrowers.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Borrower?> GetByCardUidAsync(string cardUid, CancellationToken cancellationToken = default)
    {
        var mapping = await _context.CardMappings
            .Include(m => m.Borrower)
            .FirstOrDefaultAsync(m => m.CardUid == cardUid, cancellationToken);
        
        return mapping?.Borrower;
    }
}