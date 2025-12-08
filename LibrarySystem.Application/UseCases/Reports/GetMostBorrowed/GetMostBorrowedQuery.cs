using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetMostBorrowed;

public record GetMostBorrowedQuery(int Count) : IRequest<IEnumerable<BookDto>>;