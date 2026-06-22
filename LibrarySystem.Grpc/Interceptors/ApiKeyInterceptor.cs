using Grpc.Core;
using Grpc.Core.Interceptors;

namespace LibrarySystem.Grpc.Interceptors;

public class ApiKeyInterceptor : Interceptor
{
    private readonly string _expectedKey;

    public ApiKeyInterceptor(IConfiguration config)
    {
        _expectedKey = config["Security:GrpcApiKey"]
            ?? throw new InvalidOperationException("Security:GrpcApiKey is not configured.");
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        await continuation(request, responseStream, context);
    }

    private void Authenticate(ServerCallContext context)
    {
        var key = context.RequestHeaders.GetValue("x-api-key");
        if (string.IsNullOrEmpty(key) || key != _expectedKey)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid API key."));
    }
}
