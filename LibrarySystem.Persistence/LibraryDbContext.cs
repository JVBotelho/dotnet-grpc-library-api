using LibrarySystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Persistence;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Borrower> Borrowers { get; set; }
    public DbSet<LendingActivity> LendingActivities { get; set; }
}