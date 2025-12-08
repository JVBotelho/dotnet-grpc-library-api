namespace LibrarySystem.Domain.Entities;

public class Borrower
{
    private readonly List<LendingActivity> _lendingActivities = new();

    private Borrower()
    {
    }

    public Borrower(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));

        Name = name;
        Email = email;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public IReadOnlyCollection<LendingActivity> LendingActivities => _lendingActivities.AsReadOnly();
}