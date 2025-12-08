using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Persistence.Repositories;

internal sealed class LendingRepository : ILendingRepository
{
    private readonly LibraryDbContext _context;

    public LendingRepository(LibraryDbContext context) => _context = context;

    public async Task<LendingActivity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Include Book para calcular datas se necessário, ou navegar
        return await _context.LendingActivities
            .Include(la => la.Book)
            .FirstOrDefaultAsync(la => la.Id == id, cancellationToken);
    }

    public async Task<int> GetBorrowedCopiesCountAsync(int bookId, CancellationToken cancellationToken = default)
    {
        return await _context.LendingActivities
            .CountAsync(la => la.BookId == bookId && la.ReturnedDate == null, cancellationToken);
    }

    public async Task<IEnumerable<LendingActivity>> GetTopBorrowersAsync(DateTime start, DateTime end, int count, CancellationToken cancellationToken = default)
    {
        // Trazendo as atividades agrupadas para memória (devido à complexidade do GroupBy com Entidades completas)
        // Em produção real, faríamos projeção DTO direto, mas aqui retornaremos Entidades para seguir o padrão.
        return await _context.LendingActivities
            .Include(la => la.Borrower)
            .Where(la => la.BorrowedDate >= start && la.BorrowedDate <= end)
            .ToListAsync(cancellationToken); 
            // O agrupamento final será feito no Handler para simplificar o EF translation
    }

    public async Task<IEnumerable<LendingActivity>> GetUserHistoryAsync(int borrowerId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return await _context.LendingActivities
            .Include(la => la.Book)
            .Where(la => la.BorrowerId == borrowerId && la.BorrowedDate >= start && la.BorrowedDate <= end)
            .OrderByDescending(la => la.BorrowedDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<LendingActivity>> GetByBookIdAsync(int bookId, CancellationToken cancellationToken = default)
    {
        return await _context.LendingActivities
            .Where(la => la.BookId == bookId && la.ReturnedDate != null)
            .ToListAsync(cancellationToken);
    }
}