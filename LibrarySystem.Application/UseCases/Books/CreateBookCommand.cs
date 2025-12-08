using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.CreateBook;

public record CreateBookCommand(
    string Title, 
    string Author, 
    int PublicationYear, 
    int Pages, 
    int TotalCopies
) : IRequest<BookDto>;