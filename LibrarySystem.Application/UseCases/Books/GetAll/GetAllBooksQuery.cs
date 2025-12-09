using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.GetAll;

public record GetAllBooksQuery : IRequest<IEnumerable<BookDto>>;