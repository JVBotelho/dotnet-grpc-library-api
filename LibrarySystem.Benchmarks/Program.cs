using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.UseCases.Kiosk;
using LibrarySystem.Domain.Entities;
using MediatR;
using Moq;

namespace LibrarySystem.Benchmarks;

[MemoryDiagnoser]
public class FrameEvaluationBenchmark
{
    private EvaluateFrameCommandHandler _handler = null!;
    private EvaluateFrameCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        var repoMock = new Mock<IProcessedEventRepository>();
        repoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        var publisherMock = new Mock<IPublisher>();

        _handler = new EvaluateFrameCommandHandler(repoMock.Object, publisherMock.Object);
        
        var dto = new DeviceFrameDto("KIOSK-001", DateTime.UtcNow, 0x123, 40.0, 300.0, true, 0, 0);
        _command = new EvaluateFrameCommand(dto, "bench-idempotency-key");
    }

    [Benchmark]
    public async Task<ControlCommandDto?> EvaluateFrame()
    {
        return await _handler.Handle(_command, CancellationToken.None);
    }
}

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<FrameEvaluationBenchmark>();
    }
}
