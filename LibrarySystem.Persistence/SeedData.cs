using System.Reflection;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Persistence;
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
            new Borrower("Alice Smith", "alice@example.com"),
            new Borrower("Bob Johnson", "bob@example.com"),
            new Borrower("Charlie Brown", "charlie@example.com")
        };

        var books = new[]
        {
            new Book("1984", "George Orwell", 1949, 328, 3),
            new Book("Demons", "Fyodor Dostoevsky", 1872, 736, 2),
            new Book("The Communist Manifesto", "Karl Marx & Friedrich Engels", 1848, 120, 5),
            new Book("The Metamorphosis", "Franz Kafka", 1915, 200, 4),
            new Book("Theaetetus", "Plato", -369, 250, 2),
            new Book("Phaedo", "Plato", -370, 200, 3),
            new Book("A Treatise of Human Nature", "David Hume", 1739, 700, 2),
            new Book("Laws", "Plato", -348, 450, 1)
        };

        await context.Borrowers.AddRangeAsync(borrowers);
        await context.Books.AddRangeAsync(books);
        
        
        CreateHistoricalLending(books[0], borrowers[0], daysAgoBorrowed: 30, daysAgoReturned: 15);

        CreateHistoricalLending(books[1], borrowers[0], daysAgoBorrowed: 14, daysAgoReturned: 2);

        CreateHistoricalLending(books[3], borrowers[0], daysAgoBorrowed: 40, daysAgoReturned: 30);

        CreateHistoricalLending(books[4], borrowers[1], daysAgoBorrowed: 25, daysAgoReturned: 10);

        CreateHistoricalLending(books[5], borrowers[1], daysAgoBorrowed: 9, daysAgoReturned: 1);

        CreateHistoricalLending(books[6], borrowers[1], daysAgoBorrowed: 50, daysAgoReturned: 20);

        CreateHistoricalLending(books[0], borrowers[1], daysAgoBorrowed: 60, daysAgoReturned: 50);

        CreateHistoricalLending(books[2], borrowers[2], daysAgoBorrowed: 5, daysAgoReturned: null);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to simulate historical data. 
    /// Since the Domain enforces DateTime.UtcNow, we use Reflection to set past dates.
    /// This keeps the Domain pure while allowing realistic seed data.
    /// </summary>
    private static void CreateHistoricalLending(Book book, Borrower borrower, int daysAgoBorrowed, int? daysAgoReturned)
    {
        var lendingActivity = book.BorrowCopy(borrower);

        SetPrivateProperty(lendingActivity, nameof(LendingActivity.BorrowedDate), DateTime.UtcNow.AddDays(-daysAgoBorrowed));

        if (daysAgoReturned.HasValue)
        {
            lendingActivity.MarkAsReturned();
            
            SetPrivateProperty(lendingActivity, nameof(LendingActivity.ReturnedDate), DateTime.UtcNow.AddDays(-daysAgoReturned.Value));
        }
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object value)
    {
        var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (propertyInfo?.GetSetMethod(true) != null)
        {
            propertyInfo.SetValue(instance, value);
        }
    }
}