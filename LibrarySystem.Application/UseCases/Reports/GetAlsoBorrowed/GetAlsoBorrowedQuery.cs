using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetAlsoBorrowed;

public record GetAlsoBorrowedQuery(int BookId, int Count) : IRequest<IEnumerable<BookDto>>;