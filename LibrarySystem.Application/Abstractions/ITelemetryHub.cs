using LibrarySystem.Application.UseCases.Kiosk;

namespace LibrarySystem.Application.Abstractions;

public interface ITelemetryHub
{
    void BroadcastFrame(DeviceFrameDto frame);
    IAsyncEnumerable<DeviceFrameDto> SubscribeAsync(string? deviceId, CancellationToken cancellationToken);
}
