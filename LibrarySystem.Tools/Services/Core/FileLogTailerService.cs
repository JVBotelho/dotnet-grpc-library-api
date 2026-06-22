using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.Services.Core;

// Not registered in DI. GrpcLogTailerService is the active implementation.
internal sealed class FileLogTailerService : ILogTailerService
{
    public IAsyncEnumerable<WafLogEntry> WatchAsync(CancellationToken ct) =>
        throw new NotSupportedException("Use GrpcLogTailerService.");
}
