using Grpc.Core;
using LibrarySystem.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Grpc.Services;

[Authorize(Policy = "InspectorOnly")]
public class TelemetryGrpcService : Telemetry.TelemetryBase
{
    private readonly ITelemetryHub _telemetryHub;

    public TelemetryGrpcService(ITelemetryHub telemetryHub)
    {
        _telemetryHub = telemetryHub;
    }

    public override async Task WatchDeviceFrames(
        WatchDeviceFramesRequest request, 
        IServerStreamWriter<DeviceFrame> responseStream, 
        ServerCallContext context)
    {
        await foreach (var frame in _telemetryHub.SubscribeAsync(request.DeviceId, context.CancellationToken))
        {
            var protoFrame = new DeviceFrame
            {
                DeviceId = frame.DeviceId,
                CanId = frame.CanId,
                BeltMotorTempC = frame.BeltMotorTempC,
                ScannerRpm = frame.ScannerRpm,
                SafetyDoorClosed = frame.SafetyDoorClosed,
                BayOccupancy = frame.BayOccupancy,
                FaultFlags = frame.FaultFlags
            };

            // Convert DateTime to Timestamp
            var timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(frame.SampledAt.ToUniversalTime());
            protoFrame.SampledAt = timestamp;

            await responseStream.WriteAsync(protoFrame);
        }
    }
}
