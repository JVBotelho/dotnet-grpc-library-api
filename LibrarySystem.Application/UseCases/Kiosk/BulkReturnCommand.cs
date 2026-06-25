using MediatR;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.UseCases.Kiosk;

public record ReturnScanDto(int BookId, DateTime ScannedAt, string IdempotencyKey);

public record BulkReturnCommand(string DeviceId, IEnumerable<ReturnScanDto> Scans) : IRequest<BulkReturnResult>;

public record BulkReturnResult(int Accepted, int Rejected, List<int> UnknownBookIds, int DuplicatesSkipped);

public class BulkReturnCommandHandler : IRequestHandler<BulkReturnCommand, BulkReturnResult>
{
    private readonly IBookRepository _bookRepository;
    private readonly IProcessedEventRepository _processedEventRepository;

    public BulkReturnCommandHandler(IBookRepository bookRepository, IProcessedEventRepository processedEventRepository)
    {
        _bookRepository = bookRepository;
        _processedEventRepository = processedEventRepository;
    }

    public async Task<BulkReturnResult> Handle(BulkReturnCommand request, CancellationToken cancellationToken)
    {
        int accepted = 0;
        int rejected = 0;
        int duplicates = 0;
        var unknownBookIds = new List<int>();

        var bookIds = request.Scans.Select(s => s.BookId).Distinct();
        var books = await _bookRepository.GetByIdsAsync(bookIds, cancellationToken);
        var booksDict = books.ToDictionary(b => b.Id);

        foreach (var scan in request.Scans)
        {
            if (await _processedEventRepository.ExistsAsync(scan.IdempotencyKey, cancellationToken))
            {
                duplicates++;
                continue;
            }

            // Claim the idempotency slot before mutating book state.
            // Concurrent replays will race here; the unique constraint on IdempotencyKey
            // ensures only one caller wins. If we lose the race, treat it as a duplicate.
            await _processedEventRepository.AddAsync(new ProcessedEvent(scan.IdempotencyKey), cancellationToken);
            try
            {
                await _processedEventRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("duplicate key") == true
                   || ex.InnerException?.Message.Contains("23505") == true)
            {
                duplicates++;
                continue;
            }

            if (!booksDict.TryGetValue(scan.BookId, out var book))
            {
                unknownBookIds.Add(scan.BookId);
                rejected++;
                continue;
            }

            var activeLending = book.LendingActivities.FirstOrDefault(l => l.ReturnedDate == null);
            if (activeLending != null)
            {
                book.ReturnCopy(activeLending.Id);
                accepted++;
            }
            else
            {
                rejected++;
            }
        }

        if (accepted > 0)
        {
            await _bookRepository.SaveChangesAsync(cancellationToken);
        }

        return new BulkReturnResult(accepted, rejected, unknownBookIds, duplicates);
    }
}
