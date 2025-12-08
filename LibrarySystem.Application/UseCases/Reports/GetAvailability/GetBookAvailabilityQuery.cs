using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.GetAvailability;

public record BookAvailabilityDto(int TotalCopies, int BorrowedCopies, int AvailableCopies);

public record GetBookAvailabilityQuery(int BookId) : IRequest<BookAvailabilityDto>;