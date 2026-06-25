using System.Security.Claims;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace LibrarySystem.Grpc.Interceptors;

public class DeviceIdentityInterceptor : Interceptor
{
    private readonly ILogger<DeviceIdentityInterceptor> _logger;

    public DeviceIdentityInterceptor(ILogger<DeviceIdentityInterceptor> logger)
    {
        _logger = logger;
    }

    private void AuthenticateDevice(ServerCallContext context, string? requestDeviceId)
    {
        var httpContext = context.GetHttpContext();
        var cn = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(cn) || !cn.StartsWith("KIOSK-", StringComparison.OrdinalIgnoreCase))
        {
            // If it's an inspector, let the Authorization policy handle it or allow it
            if (cn != null && cn.StartsWith("INSPECTOR-", StringComparison.OrdinalIgnoreCase))
            {
                if (context.Method.Contains("/Telemetry/"))
                {
                    return;
                }
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Inspectors cannot call Kiosk methods."));
            }
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Client certificate missing or invalid."));
        }

        if (requestDeviceId != null && cn != requestDeviceId)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, $"Device ID mismatch. Cert CN: {cn}, Request DeviceId: {requestDeviceId}"));
        }
    }

    private string? ExtractDeviceId(object request)
    {
        if (request is LibrarySystem.Contracts.Protos.BufferedEvent be)
        {
            if (be.PayloadCase == LibrarySystem.Contracts.Protos.BufferedEvent.PayloadOneofCase.Frame)
                return be.Frame?.DeviceId;
            if (be.PayloadCase == LibrarySystem.Contracts.Protos.BufferedEvent.PayloadOneofCase.ReturnScan)
                return be.ReturnScan?.DeviceId;
            return null;
        }

        var type = request.GetType();
        var prop = type.GetProperty("DeviceId");
        if (prop != null)
        {
            return prop.GetValue(request) as string;
        }
        return null;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, 
        ServerCallContext context, 
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (context.Method.Contains("/Kiosk/") || context.Method.Contains("/Telemetry/"))
        {
            var deviceId = ExtractDeviceId(request);
            AuthenticateDevice(context, deviceId);
        }
        return await continuation(request, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, 
        ServerCallContext context, 
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        if (context.Method.Contains("/Kiosk/") || context.Method.Contains("/Telemetry/"))
        {
            var wrapper = new ValidatingStreamReaderWrapper<TRequest>(requestStream, request =>
            {
                var deviceId = ExtractDeviceId(request);
                AuthenticateDevice(context, deviceId);
            });
            return await continuation(wrapper, context);
        }
        return await continuation(requestStream, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, 
        IServerStreamWriter<TResponse> responseStream, 
        ServerCallContext context, 
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        if (context.Method.Contains("/Kiosk/") || context.Method.Contains("/Telemetry/"))
        {
            var deviceId = ExtractDeviceId(request);
            AuthenticateDevice(context, deviceId);
        }
        await continuation(request, responseStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, 
        IServerStreamWriter<TResponse> responseStream, 
        ServerCallContext context, 
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        if (context.Method.Contains("/Kiosk/") || context.Method.Contains("/Telemetry/"))
        {
            var wrapper = new ValidatingStreamReaderWrapper<TRequest>(requestStream, request =>
            {
                var deviceId = ExtractDeviceId(request);
                AuthenticateDevice(context, deviceId);
            });
            await continuation(wrapper, responseStream, context);
        }
        else
        {
            await continuation(requestStream, responseStream, context);
        }
    }
}

public class ValidatingStreamReaderWrapper<T> : IAsyncStreamReader<T>
{
    private readonly IAsyncStreamReader<T> _inner;
    private readonly Action<T> _onEachMessage;

    public ValidatingStreamReaderWrapper(IAsyncStreamReader<T> inner, Action<T> onEachMessage)
    {
        _inner = inner;
        _onEachMessage = onEachMessage;
    }

    public T Current => _inner.Current;

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        var hasNext = await _inner.MoveNext(cancellationToken);
        if (hasNext)
        {
            // Validate DeviceId == cert CN on every message so a caller cannot spoof
            // a different device identity after the first message has been accepted.
            _onEachMessage(_inner.Current);
        }
        return hasNext;
    }
}
