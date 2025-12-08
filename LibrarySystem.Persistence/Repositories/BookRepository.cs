using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Persistence.Repositories;

internal sealed class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context) => _context = context;

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .Include(b => b.LendingActivities)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Books.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        await _context.Books.AddAsync(book, cancellationToken);
    }

    public void Update(Book book) => _context.Books.Update(book);

    public void Remove(Book book) => _context.Books.Remove(book);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetMostBorrowedAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .Include(b => b.LendingActivities)
            .OrderByDescending(b => b.LendingActivities.Count)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetAlsoBorrowedAsync(int bookId, int count, CancellationToken cancellationToken = default)
    {
        var borrowerIds = await _context.LendingActivities
            .Where(la => la.BookId == bookId)
            .Select(la => la.BorrowerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!borrowerIds.Any()) return Enumerable.Empty<Book>();

        var books = await _context.LendingActivities
            .Where(la => borrowerIds.Contains(la.BorrowerId) && la.BookId != bookId)
            .GroupBy(la => la.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(count)
            .ToListAsync(cancellationToken);
            
        var bookIds = books.Select(x => x.BookId).ToList();

        return await _context.Books
            .Where(b => bookIds.Contains(b.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}