using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.Update;

public record UpdateBookCommand(
    int Id, 
    string Title, 
    string Author, 
    int PublicationYear, 
    int Pages, 
    int TotalCopies
) : IRequest<BookDto>;