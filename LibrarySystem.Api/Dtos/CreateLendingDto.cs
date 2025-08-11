namespace LibrarySystem.Api.Dtos;

public class CreateLendingDto
{
    public int BookId { get; set; }
    public int BorrowerId { get; set; }
}