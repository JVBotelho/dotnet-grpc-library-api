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
                await _processedEventRepository.AddAsync(new ProcessedEvent(scan.IdempotencyKey), cancellationToken);
                accepted++;
            }
            else
            {
                await _processedEventRepository.AddAsync(new ProcessedEvent(scan.IdempotencyKey), cancellationToken);
                rejected++;
            }
        }

        if (accepted > 0 || rejected > 0)
        {
            await _bookRepository.SaveChangesAsync(cancellationToken);
            await _processedEventRepository.SaveChangesAsync(cancellationToken);
        }
        
        return new BulkReturnResult(accepted, rejected, unknownBookIds, duplicates);
    }
}
