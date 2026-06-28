using System.Text;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Grpc.Services;
using LibrarySystem.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibrarySystem.UnitTests.Grpc;

public class WafLogServiceTests : IDisposable
{
    private readonly Mock<ILogger<WafLogService>> _loggerMock;
    private readonly WafLogService _sut;
    // Since the path is hardcoded to /var/log/waf/audit.json in WafLogService, we'll test the "File Not Found" path to get coverage.

    public WafLogServiceTests()
    {
        _loggerMock = new Mock<ILogger<WafLogService>>();
        _sut = new WafLogService(_loggerMock.Object);
    }

    [Fact]
    public async Task WatchWafLogs_WhenLogFileNotFound_ShouldWriteNotFoundMessageAndReturn()
    {
        // Arrange
        var context = TestServerCallContext.Create();
        var responseStreamMock = new Mock<IServerStreamWriter<WafLogEntry>>();
        
        var writtenEntries = new List<WafLogEntry>();
        responseStreamMock.Setup(x => x.WriteAsync(It.IsAny<WafLogEntry>()))
            .Callback<WafLogEntry>(entry => writtenEntries.Add(entry))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.WatchWafLogs(new Empty(), responseStreamMock.Object, context);

        // Assert
        writtenEntries.Should().ContainSingle();
        writtenEntries[0].Details.Should().Contain("Log file not found");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
