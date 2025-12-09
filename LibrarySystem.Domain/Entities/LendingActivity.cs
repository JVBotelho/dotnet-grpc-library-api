namespace LibrarySystem.Domain.Entities;

public class LendingActivity
{
    // EF Core constructor
    private LendingActivity()
    {
    }

    internal LendingActivity(Book book, Borrower borrower)
    {
        Book = book ?? throw new ArgumentNullException(nameof(book));
        Borrower = borrower ?? throw new ArgumentNullException(nameof(borrower));
        BookId = book.Id;
        BorrowerId = borrower.Id;
        BorrowedDate = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public int BookId { get; private set; }
    public Book? Book { get; private set; }

    public int BorrowerId { get; private set; }
    public Borrower? Borrower { get; private set; }

    public DateTime BorrowedDate { get; private set; }
    public DateTime? ReturnedDate { get; private set; }

    public void MarkAsReturned()
    {
        if (ReturnedDate.HasValue) throw new InvalidOperationException("Book is already returned.");

        ReturnedDate = DateTime.UtcNow;
    }
}