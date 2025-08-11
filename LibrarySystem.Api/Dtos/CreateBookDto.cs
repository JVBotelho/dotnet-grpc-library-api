namespace LibrarySystem.Api.Dtos;

public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int Pages { get; set; }
    public int TotalCopies { get; set; }
}