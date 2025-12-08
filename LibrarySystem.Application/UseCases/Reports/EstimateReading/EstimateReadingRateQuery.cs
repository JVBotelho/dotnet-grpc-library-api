using MediatR;

namespace LibrarySystem.Application.UseCases.Reports.EstimateReading;

public record EstimateReadingRateQuery(int BookId) : IRequest<double>;