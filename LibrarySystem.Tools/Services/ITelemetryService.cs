using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.Services;

public interface ITelemetryService
{
    IAsyncEnumerable<DeviceFrame> WatchDeviceFramesAsync(string? deviceId, CancellationToken cancellationToken);
}
