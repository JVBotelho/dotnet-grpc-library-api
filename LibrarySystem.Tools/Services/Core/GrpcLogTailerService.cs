using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.Services.Core;

public class GrpcLogTailerService : ILogTailerService
{
    private readonly Security.SecurityClient _client;

    public GrpcLogTailerService(Security.SecurityClient client) => _client = client;

    public async IAsyncEnumerable<WafLogEntry> WatchAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var call = _client.WatchWafLogs(new Empty(), cancellationToken: ct);
        IAsyncEnumerator<WafLogEntry>? enumerator = null;
        try
        {
            enumerator = call.ResponseStream.ReadAllAsync(ct).GetAsyncEnumerator(ct);
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    // Translate gRPC cancellation into OperationCanceledException so callers
                    // don't need a Grpc.Core reference to handle user-initiated stop.
                    ct.ThrowIfCancellationRequested();
                    yield break;
                }
                if (!hasNext) break;
                yield return enumerator.Current;
            }
        }
        finally
        {
            if (enumerator is not null) await enumerator.DisposeAsync();
        }
    }
}
