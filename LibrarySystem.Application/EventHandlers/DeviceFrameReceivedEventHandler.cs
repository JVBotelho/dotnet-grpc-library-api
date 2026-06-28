using MediatR;
using LibrarySystem.Application.Abstractions;
using LibrarySystem.Application.UseCases.Kiosk;

namespace LibrarySystem.Application.EventHandlers;

public class DeviceFrameReceivedEventHandler : INotificationHandler<DeviceFrameReceivedEvent>
{
    private readonly ITelemetryHub _telemetryHub;

    public DeviceFrameReceivedEventHandler(ITelemetryHub telemetryHub)
    {
        _telemetryHub = telemetryHub;
    }

    public Task Handle(DeviceFrameReceivedEvent notification, CancellationToken cancellationToken)
    {
        _telemetryHub.BroadcastFrame(notification.Frame);
        return Task.CompletedTask;
    }
}
