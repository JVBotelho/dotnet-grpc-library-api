namespace LibrarySystem.Application.DTOs;

public record LendingDto(
    int Id,
    int BookId,
    int BorrowerId,
    DateTime BorrowedDate,
    DateTime? ReturnedDate
);