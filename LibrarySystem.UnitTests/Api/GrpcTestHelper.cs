using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Testing;

namespace LibrarySystem.UnitTests.Api;

public static class GrpcTestHelper
{
    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return TestCalls.AsyncUnaryCall(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );
    }
}
