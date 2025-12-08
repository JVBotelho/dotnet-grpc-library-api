using LibrarySystem.Application.DTOs;
using MediatR;

namespace LibrarySystem.Application.UseCases.Lending.ReturnBook;

public record ReturnBookCommand(int LendingActivityId) : IRequest<LendingDto>;