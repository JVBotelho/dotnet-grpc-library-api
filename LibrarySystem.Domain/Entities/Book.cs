namespace LibrarySystem.Domain.Entities;

public class Book
{
    // Backing field for EF Core
    private readonly List<LendingActivity> _lendingActivities = new();

    // EF Core needs a parameterless constructor for reflection
    private Book()
    {
    }

    public Book(string title, string author, int publicationYear, int pages, int totalCopies)
    {
        // Guard Clauses (Shift Left: Fail Fast)
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("Author cannot be empty.", nameof(author));
        if (totalCopies < 0) throw new ArgumentException("Total copies cannot be negative.", nameof(totalCopies));

        Title = title;
        Author = author;
        PublicationYear = publicationYear;
        Pages = pages;
        TotalCopies = totalCopies;
    }

    public int Id { get; private set; }
    public string Title { get; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public int PublicationYear { get; private set; }
    public int Pages { get; private set; }
    public int TotalCopies { get; }

    // Public read-only access
    public IReadOnlyCollection<LendingActivity> LendingActivities => _lendingActivities.AsReadOnly();

    /// <summary>
    ///     Domain Logic: Tries to borrow a copy of the book.
    /// </summary>
    public LendingActivity BorrowCopy(Borrower borrower)
    {
        // Business Rule: Check availability
        var currentlyBorrowed = _lendingActivities.Count(x => x.ReturnedDate == null);

        if (currentlyBorrowed >= TotalCopies)
            // You can replace this with a DomainException later
            throw new InvalidOperationException($"No copies available for book '{Title}'.");

        var lending = new LendingActivity(this, borrower);
        _lendingActivities.Add(lending);

        return lending;
    }

    public void ReturnCopy(int lendingId)
    {
        var lending = _lendingActivities.FirstOrDefault(x => x.Id == lendingId);

        if (lending == null) throw new ArgumentException("Lending record not found.", nameof(lendingId));

        lending.MarkAsReturned();
    }
}