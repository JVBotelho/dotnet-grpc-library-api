using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Lending.BorrowBook;

public record BorrowBookCommand(int BookId, int BorrowerId) : IRequest<LendingDto>;