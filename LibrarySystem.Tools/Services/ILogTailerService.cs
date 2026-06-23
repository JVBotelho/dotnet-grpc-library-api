using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.Services;

public interface ILogTailerService
{
    IAsyncEnumerable<WafLogEntry> WatchAsync(CancellationToken ct);
}
