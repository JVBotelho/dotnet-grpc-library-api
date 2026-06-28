using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Application.UseCases.Kiosk;
using KioskContracts = LibrarySystem.Contracts.Protos.Kiosk;

namespace LibrarySystem.Grpc.Services;

[Authorize]
public class KioskService : KioskContracts.KioskBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KioskService> _logger;

    public KioskService(IMediator mediator, ILogger<KioskService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<ValidateMemberResponse> ValidateMember(ValidateMemberRequest request, ServerCallContext context)
    {
        var result = await _mediator.Send(new ValidateMemberQuery(request.DeviceId, request.CardUid));
        
        // Always attempt parse; return identical error for both cases to prevent timing oracle
        if (!result.Valid)
        {
            return new ValidateMemberResponse
            {
                Valid = false,
                BorrowerId = 0,
                DisplayName = string.Empty,
                Reason = "Invalid credentials."
            };
        }

        return new ValidateMemberResponse
        {
            Valid = true,
            BorrowerId = result.BorrowerId,
            DisplayName = result.DisplayName ?? string.Empty,
            Reason = string.Empty
        };
    }

    public override async Task<BulkReturnSummary> BulkReturn(IAsyncStreamReader<ReturnScan> requestStream, ServerCallContext context)
    {
        var scans = new List<ReturnScanDto>();
        string? deviceId = null;
        var callerCn = context.GetHttpContext().User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        const int MaxStreamMessages = 1000;
        int messageCount = 0;

        await foreach (var scan in requestStream.ReadAllAsync())
        {
            if (++messageCount > MaxStreamMessages)
            {
                context.Status = new Status(StatusCode.ResourceExhausted, "Stream message limit exceeded.");
                return new BulkReturnSummary();
            }

            if (callerCn != null && (!scan.IdempotencyKey.StartsWith(callerCn) || scan.IdempotencyKey.Length > 100))
            {
                context.Status = new Status(StatusCode.InvalidArgument, "Invalid idempotency key.");
                return new BulkReturnSummary();
            }

            deviceId ??= scan.DeviceId;
            scans.Add(new ReturnScanDto(scan.BookId, scan.ScannedAt.ToDateTime(), scan.IdempotencyKey));
        }

        if (deviceId == null) return new BulkReturnSummary();

        var result = await _mediator.Send(new BulkReturnCommand(deviceId, scans));

        var summary = new BulkReturnSummary
        {
            Accepted = result.Accepted,
            Rejected = result.Rejected,
            DuplicatesSkipped = result.DuplicatesSkipped
        };
        summary.UnknownBookIds.AddRange(result.UnknownBookIds);
        
        return summary;
    }

    public override async Task<SyncSummary> SyncOfflineQueue(IAsyncStreamReader<BufferedEvent> requestStream, ServerCallContext context)
    {
        var callerCn = context.GetHttpContext().User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        string deviceId = callerCn ?? string.Empty;

        const int MaxStreamMessages = 1000;
        int messageCount = 0;
        int applied = 0;
        int duplicates = 0;

        await foreach (var evt in requestStream.ReadAllAsync())
        {
            if (++messageCount > MaxStreamMessages)
            {
                context.Status = new Status(StatusCode.ResourceExhausted, "Stream message limit exceeded.");
                return new SyncSummary { Applied = applied, DuplicatesSkipped = duplicates };
            }

            if (callerCn != null && (!evt.IdempotencyKey.StartsWith(callerCn) || evt.IdempotencyKey.Length > 100))
            {
                context.Status = new Status(StatusCode.InvalidArgument, "Invalid idempotency key.");
                return new SyncSummary { Applied = applied, DuplicatesSkipped = duplicates };
            }

            ReturnScanDto? returnScan = null;
            DeviceFrameDto? frame = null;

            if (evt.PayloadCase == BufferedEvent.PayloadOneofCase.ReturnScan)
            {
                returnScan = new ReturnScanDto(evt.ReturnScan.BookId, evt.ReturnScan.ScannedAt.ToDateTime(), evt.ReturnScan.IdempotencyKey);
            }
            else if (evt.PayloadCase == BufferedEvent.PayloadOneofCase.Frame)
            {
                frame = new DeviceFrameDto(
                    evt.Frame.DeviceId, 
                    evt.Frame.SampledAt.ToDateTime(), 
                    evt.Frame.CanId, 
                    evt.Frame.BeltMotorTempC, 
                    evt.Frame.ScannerRpm, 
                    evt.Frame.SafetyDoorClosed, 
                    evt.Frame.BayOccupancy, 
                    evt.Frame.FaultFlags);
            }

            var bufferedEvent = new BufferedEventDto(evt.IdempotencyKey, returnScan, frame);
            var result = await _mediator.Send(new SyncOfflineQueueCommand(deviceId, new[] { bufferedEvent }));
            
            applied += result.Applied;
            duplicates += result.DuplicatesSkipped;
        }

        return new SyncSummary
        {
            Applied = applied,
            DuplicatesSkipped = duplicates
        };
    }

    public override async Task DeviceLink(IAsyncStreamReader<DeviceFrame> requestStream, IServerStreamWriter<ControlCommand> responseStream, ServerCallContext context)
    {
        using var idleCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, idleCts.Token);

        await foreach (var frame in requestStream.ReadAllAsync(linked.Token))
        {
            idleCts.CancelAfter(TimeSpan.FromMinutes(5)); // reset on each received frame

            var dto = new DeviceFrameDto(
                    frame.DeviceId, 
                    frame.SampledAt.ToDateTime(), 
                    frame.CanId, 
                    frame.BeltMotorTempC, 
                    frame.ScannerRpm, 
                    frame.SafetyDoorClosed, 
                    frame.BayOccupancy, 
                    frame.FaultFlags);

            var idKey = $"{frame.DeviceId}_{frame.SampledAt.Seconds}_{frame.SampledAt.Nanos}";
            var controlCmd = await _mediator.Send(new EvaluateFrameCommand(dto, idKey));
            
            if (controlCmd != null)
            {
                await responseStream.WriteAsync(new ControlCommand
                {
                    Kind = (ControlCommand.Types.Kind)controlCmd.Kind,
                    Reason = controlCmd.Reason,
                    Arg = controlCmd.Arg
                });
            }
        }
    }
}
