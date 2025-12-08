namespace LibrarySystem.Application.DTOs;

public record BookDto(
    int Id,
    string Title,
    string Author,
    int PublicationYear,
    int Pages,
    int TotalCopies,
    int AvailableCopies
);