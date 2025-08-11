namespace LibrarySystem.Persistence.Entities;

public class LendingActivity
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }

    public DateTime BorrowedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
}