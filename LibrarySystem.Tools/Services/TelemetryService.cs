using Grpc.Core;
using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.Services;

public class TelemetryService : ITelemetryService
{
    private readonly Telemetry.TelemetryClient _client;

    public TelemetryService(Telemetry.TelemetryClient client)
    {
        _client = client;
    }

    public async IAsyncEnumerable<DeviceFrame> WatchDeviceFramesAsync(string? deviceId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new WatchDeviceFramesRequest { DeviceId = deviceId ?? string.Empty };
        
        using var call = _client.WatchDeviceFrames(request, cancellationToken: cancellationToken);
        
        await foreach (var frame in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return frame;
        }
    }
}
