namespace LibrarySystem.Persistence.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int Pages { get; set; } 
    public int TotalCopies { get; set; }

    public ICollection<LendingActivity> LendingActivities { get; set; } = new List<LendingActivity>();
}