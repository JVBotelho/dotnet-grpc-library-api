using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.GetBookById;

public record GetBookByIdQuery(int Id) : IRequest<BookDto?>;