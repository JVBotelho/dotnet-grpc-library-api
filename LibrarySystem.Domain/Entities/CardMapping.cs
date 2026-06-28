namespace LibrarySystem.Domain.Entities;

public class CardMapping
{
    private CardMapping() { }

    public CardMapping(string cardUid, int borrowerId)
    {
        if (string.IsNullOrWhiteSpace(cardUid)) throw new ArgumentException("Card UID is required.", nameof(cardUid));

        CardUid = cardUid;
        BorrowerId = borrowerId;
    }

    public string CardUid { get; private set; } = string.Empty;
    public int BorrowerId { get; private set; }
    
    public Borrower? Borrower { get; private set; }
}
