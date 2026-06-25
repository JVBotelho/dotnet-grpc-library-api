using MediatR;
using LibrarySystem.Application.Abstractions.Repositories;

namespace LibrarySystem.Application.UseCases.Kiosk;

public record SyncOfflineQueueCommand(string DeviceId, IEnumerable<BufferedEventDto> Events) : IRequest<SyncSummaryResult>;

public record BufferedEventDto(string IdempotencyKey, ReturnScanDto? ReturnScan, DeviceFrameDto? Frame);

public record SyncSummaryResult(int Applied, int DuplicatesSkipped);

public class SyncOfflineQueueCommandHandler : IRequestHandler<SyncOfflineQueueCommand, SyncSummaryResult>
{
    private readonly IMediator _mediator;
    private readonly IProcessedEventRepository _processedEventRepository;

    public SyncOfflineQueueCommandHandler(IMediator mediator, IProcessedEventRepository processedEventRepository)
    {
        _mediator = mediator;
        _processedEventRepository = processedEventRepository;
    }

    public async Task<SyncSummaryResult> Handle(SyncOfflineQueueCommand request, CancellationToken cancellationToken)
    {
        int applied = 0;
        int duplicates = 0;

        foreach (var evt in request.Events)
        {
            if (evt.ReturnScan != null)
            {
                var result = await _mediator.Send(new BulkReturnCommand(request.DeviceId, new[] { evt.ReturnScan }), cancellationToken);
                applied += result.Accepted;
                duplicates += result.DuplicatesSkipped;
            }
            else if (evt.Frame != null)
            {
                // Pre-check idempotency so we can report an accurate duplicate count.
                // EvaluateFrameCommand returns null for both "duplicate" and "no command needed",
                // so we cannot distinguish them from its return value alone.
                bool isDuplicate = !string.IsNullOrEmpty(evt.IdempotencyKey)
                    && await _processedEventRepository.ExistsAsync(evt.IdempotencyKey, cancellationToken);

                await _mediator.Send(new EvaluateFrameCommand(evt.Frame, evt.IdempotencyKey), cancellationToken);

                if (isDuplicate)
                    duplicates++;
                else
                    applied++;
            }
        }

        return new SyncSummaryResult(applied, duplicates);
    }
}
