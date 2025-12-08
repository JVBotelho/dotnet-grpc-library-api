using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Abstractions.Repositories;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Book book, CancellationToken cancellationToken = default);
    void Update(Book book);
    void Remove(Book book);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Book>> GetMostBorrowedAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetAlsoBorrowedAsync(int bookId, int count, CancellationToken cancellationToken = default);
}