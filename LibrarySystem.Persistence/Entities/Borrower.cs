namespace LibrarySystem.Persistence.Entities;

public class Borrower
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<LendingActivity> LendingActivities { get; set; } = new List<LendingActivity>();
}