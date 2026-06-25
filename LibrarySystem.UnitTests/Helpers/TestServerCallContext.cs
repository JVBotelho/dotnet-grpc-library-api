using Grpc.Core;
using Moq;

namespace LibrarySystem.UnitTests.Helpers;

public class TestServerCallContext : ServerCallContext
{
    private readonly Metadata _requestHeaders;

    private TestServerCallContext(Metadata? requestHeaders)
    {
        _requestHeaders = requestHeaders ?? new Metadata();
    }

    protected override string MethodCore => "TestMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "localhost";
    protected override DateTime DeadlineCore => DateTime.UtcNow.AddHours(1);
    protected override Metadata RequestHeadersCore => _requestHeaders;
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore => new Metadata();
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new AuthContext(null, new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => null!;
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;

    public static ServerCallContext Create(Metadata? requestHeaders = null)
    {
        return new TestServerCallContext(requestHeaders);
    }
}