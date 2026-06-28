using MediatR;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.UseCases.Kiosk;

public record DeviceFrameDto(string DeviceId, DateTime SampledAt, uint CanId, double BeltMotorTempC, double ScannerRpm, bool SafetyDoorClosed, uint BayOccupancy, uint FaultFlags);

public enum ControlKind { Noop = 0, StopMotor = 1, RaiseAlarm = 2, ThrottleIntake = 3, ClearFault = 4 }

public record ControlCommandDto(ControlKind Kind, string Reason, uint Arg);

public record EvaluateFrameCommand(DeviceFrameDto Frame, string? IdempotencyKey = null) : IRequest<ControlCommandDto?>;

public record DeviceFrameReceivedEvent(DeviceFrameDto Frame) : INotification;

public class EvaluateFrameCommandHandler : IRequestHandler<EvaluateFrameCommand, ControlCommandDto?>
{
    private readonly IProcessedEventRepository _processedEventRepository;
    private readonly IPublisher _publisher;

    public EvaluateFrameCommandHandler(IProcessedEventRepository processedEventRepository, IPublisher publisher)
    {
        _processedEventRepository = processedEventRepository;
        _publisher = publisher;
    }

    public async Task<ControlCommandDto?> Handle(EvaluateFrameCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            if (await _processedEventRepository.ExistsAsync(request.IdempotencyKey, cancellationToken))
            {
                return null;
            }
            await _processedEventRepository.AddAsync(new ProcessedEvent(request.IdempotencyKey), cancellationToken);
            try
            {
                await _processedEventRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) 
                when (ex.InnerException?.Message.Contains("duplicate key") == true 
                   || ex.InnerException?.Message.Contains("23505") == true)
            {
                return null;
            }
        }

        // Broadcast for telemetry (Inspector UI)
        await _publisher.Publish(new DeviceFrameReceivedEvent(request.Frame), cancellationToken);

        // Kiosk rule evaluation
        if (request.Frame.BeltMotorTempC > 80)
        {
            return new ControlCommandDto(ControlKind.StopMotor, "Over-temp detected", 0);
        }
        
        if (request.Frame.FaultFlags != 0)
        {
            return new ControlCommandDto(ControlKind.RaiseAlarm, "Fault flags set", request.Frame.FaultFlags);
        }

        return null;
    }
}
