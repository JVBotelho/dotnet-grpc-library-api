using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class SeedData
{
    public static async Task SeedDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        await context.Database.MigrateAsync();

        if (await context.Books.AnyAsync() || await context.Borrowers.AnyAsync()) return;

        var borrowers = new[]
        {
            new Borrower { Name = "Alice Smith", Email = "alice@example.com" },
            new Borrower { Name = "Bob Johnson", Email = "bob@example.com" },
            new Borrower { Name = "Charlie Brown", Email = "charlie@example.com" }
        };

        var books = new[]
        {
            new Book { Title = "1984", Author = "George Orwell", PublicationYear = 1949, Pages = 328, TotalCopies = 3 },
            new Book
            {
                Title = "Demons", Author = "Fyodor Dostoevsky", PublicationYear = 1872, Pages = 736, TotalCopies = 2
            },
            new Book
            {
                Title = "The Communist Manifesto", Author = "Karl Marx & Friedrich Engels", PublicationYear = 1848,
                Pages = 120, TotalCopies = 5
            },
            new Book
            {
                Title = "The Metamorphosis", Author = "Franz Kafka", PublicationYear = 1915, Pages = 200,
                TotalCopies = 4
            },
            new Book { Title = "Theaetetus", Author = "Plato", PublicationYear = -369, Pages = 250, TotalCopies = 2 },
            new Book { Title = "Phaedo", Author = "Plato", PublicationYear = -370, Pages = 200, TotalCopies = 3 },
            new Book
            {
                Title = "A Treatise of Human Nature", Author = "David Hume", PublicationYear = 1739, Pages = 700,
                TotalCopies = 2
            },
            new Book { Title = "Laws", Author = "Plato", PublicationYear = -348, Pages = 450, TotalCopies = 1 }
        };

        await context.Borrowers.AddRangeAsync(borrowers);
        await context.Books.AddRangeAsync(books);
        await context.SaveChangesAsync();

        var lendingActivities = new[]
        {
            new LendingActivity
            {
                BookId = books[0].Id, BorrowerId = borrowers[0].Id, BorrowedDate = DateTime.UtcNow.AddDays(-30),
                ReturnedDate = DateTime.UtcNow.AddDays(-15)
            },
            new LendingActivity
            {
                BookId = books[1].Id, BorrowerId = borrowers[0].Id, BorrowedDate = DateTime.UtcNow.AddDays(-14),
                ReturnedDate = DateTime.UtcNow.AddDays(-2)
            },
            new LendingActivity
            {
                BookId = books[3].Id, BorrowerId = borrowers[0].Id, BorrowedDate = DateTime.UtcNow.AddDays(-40),
                ReturnedDate = DateTime.UtcNow.AddDays(-30)
            },

            new LendingActivity
            {
                BookId = books[4].Id, BorrowerId = borrowers[1].Id, BorrowedDate = DateTime.UtcNow.AddDays(-25),
                ReturnedDate = DateTime.UtcNow.AddDays(-10)
            },
            new LendingActivity
            {
                BookId = books[5].Id, BorrowerId = borrowers[1].Id, BorrowedDate = DateTime.UtcNow.AddDays(-9),
                ReturnedDate = DateTime.UtcNow.AddDays(-1)
            },
            new LendingActivity
            {
                BookId = books[6].Id, BorrowerId = borrowers[1].Id, BorrowedDate = DateTime.UtcNow.AddDays(-50),
                ReturnedDate = DateTime.UtcNow.AddDays(-20)
            },

            new LendingActivity
            {
                BookId = books[0].Id, BorrowerId = borrowers[1].Id, BorrowedDate = DateTime.UtcNow.AddDays(-60),
                ReturnedDate = DateTime.UtcNow.AddDays(-50)
            },
            new LendingActivity
            {
                BookId = books[2].Id, BorrowerId = borrowers[2].Id, BorrowedDate = DateTime.UtcNow.AddDays(-5),
                ReturnedDate = null
            }
        };

        await context.LendingActivities.AddRangeAsync(lendingActivities);
        await context.SaveChangesAsync();
    }
}