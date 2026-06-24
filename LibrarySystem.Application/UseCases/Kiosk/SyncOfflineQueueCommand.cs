using MediatR;
using LibrarySystem.Application.Abstractions.Repositories;

namespace LibrarySystem.Application.UseCases.Kiosk;

public record SyncOfflineQueueCommand(string DeviceId, IEnumerable<BufferedEventDto> Events) : IRequest<SyncSummaryResult>;

public record BufferedEventDto(string IdempotencyKey, ReturnScanDto? ReturnScan, DeviceFrameDto? Frame);

public record SyncSummaryResult(int Applied, int DuplicatesSkipped);

public class SyncOfflineQueueCommandHandler : IRequestHandler<SyncOfflineQueueCommand, SyncSummaryResult>
{
    private readonly IMediator _mediator;

    public SyncOfflineQueueCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
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
                // EvaluateFrameCommand currently doesn't return a duplicate count, but it handles idempotency.
                // We'll just assume 1 applied for now.
                await _mediator.Send(new EvaluateFrameCommand(evt.Frame, evt.IdempotencyKey), cancellationToken);
                applied++;
            }
        }

        return new SyncSummaryResult(applied, duplicates);
    }
}
