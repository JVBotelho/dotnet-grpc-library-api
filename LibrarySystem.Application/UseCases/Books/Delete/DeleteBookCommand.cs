using MediatR;

namespace LibrarySystem.Application.UseCases.Books.Delete;

public record DeleteBookCommand(int Id) : IRequest<Unit>;