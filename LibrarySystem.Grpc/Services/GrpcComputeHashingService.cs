using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using LibrarySystem.Application.Abstractions.Services;
using LibrarySystem.Contracts.Protos;
using Polly;
using Polly.Retry;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Grpc.Services;

public class GrpcComputeHashingService : IImageHashingService
{
    private readonly Compute.ComputeClient _computeClient;
    private readonly ILogger<GrpcComputeHashingService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GrpcComputeHashingService(Compute.ComputeClient computeClient, ILogger<GrpcComputeHashingService> logger)
    {
        _computeClient = computeClient;
        _logger = logger;
        
        _retryPolicy = Policy
            .Handle<RpcException>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Compute node unavailable. Retrying in {Delay}s (Attempt {RetryCount}/2)", timeSpan.TotalSeconds, retryCount);
                });
    }

    public async Task<string> ComputeHashAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = new ComputeNodeImageHashRequest { ImageData = Google.Protobuf.ByteString.CopyFrom(imageData) };
                var response = await _computeClient.ComputeImageHashAsync(request, cancellationToken: cancellationToken);
                return response.PHash;
            });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Compute node failed after retries. Falling back to native managed SHA-256.");
            return ComputeManagedFallbackHash(imageData);
        }
    }

    private string ComputeManagedFallbackHash(byte[] imageData)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(imageData);
        
        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
