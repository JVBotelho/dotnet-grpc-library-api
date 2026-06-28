using System.Runtime.CompilerServices;
using FluentAssertions;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;
using LibrarySystem.Tools.ViewModels;
using Moq;

namespace LibrarySystem.Tools.UnitTests.ViewModels;

public class WafLogViewModelTests
{
    [Fact]
    public void MonitoringButtonText_WhenNotMonitoring_IsStartMonitoring()
    {
        var sut = new WafLogViewModel(Mock.Of<ILogTailerService>());

        sut.MonitoringButtonText.Should().Be("Start Monitoring");
    }

    [Fact]
    public void MonitoringButtonText_WhenMonitoringStarted_IsStopMonitoring()
    {
        var logMock = new Mock<ILogTailerService>();
        logMock.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
               .Returns<CancellationToken>(static ct => EmptyStream(ct));

        var sut = new WafLogViewModel(logMock.Object);
        sut.IsMonitoring = true;

        sut.MonitoringButtonText.Should().Be("Stop Monitoring");
    }

    [Fact]
    public async Task StartMonitoring_ReceivesLogs_AddsToCollection()
    {
        var entries = new[]
        {
            new WafLogEntry { Action = "ALLOWED", RequestUri = "/api/books", ClientIp = "10.0.0.1" },
            new WafLogEntry { Action = "BLOCKED", RequestUri = "/etc/passwd", ClientIp = "1.2.3.4" },
        };
        var logMock = new Mock<ILogTailerService>();
        logMock.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
               .Returns<CancellationToken>(ct => entries.ToAsyncEnumerable(ct));

        var sut = new WafLogViewModel(logMock.Object);
        sut.IsMonitoring = true;
        await sut.CurrentStreamTask!;

        sut.Logs.Should().HaveCount(2);
    }

    [Fact]
    public void StopMonitoring_SetsStatusTextToStreamPaused()
    {
        var logMock = new Mock<ILogTailerService>();
        logMock.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
               .Returns<CancellationToken>(static ct => EmptyStream(ct));

        var sut = new WafLogViewModel(logMock.Object);
        sut.IsMonitoring = true;
        sut.IsMonitoring = false;

        sut.StatusText.Should().Be("Stream Paused");
    }

    [Fact]
    public void StatusText_DefaultValue_IsSecurityStreamOffline()
    {
        var sut = new WafLogViewModel(Mock.Of<ILogTailerService>());

        sut.StatusText.Should().Be("Security Stream Offline");
    }

    [Fact]
    public async Task StartMonitoring_ServiceThrows_SetsConnectionLostStatus()
    {
        var logMock = new Mock<ILogTailerService>();
        logMock.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
               .Returns<CancellationToken>(static ct => ThrowingStream(ct));

        var sut = new WafLogViewModel(logMock.Object);
        sut.IsMonitoring = true;
        await sut.CurrentStreamTask!;

        sut.StatusText.Should().StartWith("Connection Lost:");
        sut.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public async Task StartMonitoring_MoreThan100Logs_TrimsCollectionTo100()
    {
        var entries = Enumerable.Range(1, 101)
            .Select(i => new WafLogEntry { Action = "ALLOWED", RequestUri = $"/api/{i}" });

        var logMock = new Mock<ILogTailerService>();
        logMock.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
               .Returns<CancellationToken>(ct => entries.ToAsyncEnumerable(ct));

        var sut = new WafLogViewModel(logMock.Object);
        sut.IsMonitoring = true;
        await sut.CurrentStreamTask!;

        sut.Logs.Should().HaveCount(100);
    }

    [Fact]
    public void Dispose_StopsMonitoringAndReleasesResources()
    {
        var logMock = new Mock<ILogTailerService>();
        logMock.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
               .Returns<CancellationToken>(static ct => EmptyStream(ct));

        var sut = new WafLogViewModel(logMock.Object);
        sut.IsMonitoring = true;

        sut.Dispose();

        sut.StatusText.Should().Be("Stream Paused");
    }

    private static async IAsyncEnumerable<WafLogEntry> ThrowingStream(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        throw new InvalidOperationException("Simulated service failure");
#pragma warning disable CS0162 // Unreachable code detected
        yield break; // unreachable — satisfies the compiler that this is an async iterator
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<WafLogEntry> EmptyStream(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        yield break;
    }
}

file static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in source)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return item;
        }
    }
}
