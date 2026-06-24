using System.Threading.Channels;
using LibrarySystem.Application.Abstractions;
using LibrarySystem.Application.UseCases.Kiosk;

namespace LibrarySystem.Grpc.Services;

public class TelemetryHub : ITelemetryHub
{
    private readonly List<Channel<DeviceFrameDto>> _subscribers = new();
    private readonly object _lock = new();

    public void BroadcastFrame(DeviceFrameDto frame)
    {
        lock (_lock)
        {
            // Clean up closed channels while broadcasting
            _subscribers.RemoveAll(c => c.Reader.Completion.IsCompleted);

            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryWrite(frame);
            }
        }
    }

    public async IAsyncEnumerable<DeviceFrameDto> SubscribeAsync(string? deviceId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<DeviceFrameDto>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_lock)
        {
            _subscribers.Add(channel);
        }

        try
        {
            await foreach (var frame in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (string.IsNullOrEmpty(deviceId) || frame.DeviceId == deviceId)
                {
                    yield return frame;
                }
            }
        }
        finally
        {
            channel.Writer.TryComplete();
            lock (_lock)
            {
                _subscribers.Remove(channel);
            }
        }
    }
}
