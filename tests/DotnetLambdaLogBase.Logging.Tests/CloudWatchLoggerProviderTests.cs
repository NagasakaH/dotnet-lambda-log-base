using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetLambdaLogBase.Logging.Tests;

public class CloudWatchLoggerProviderTests
{
    private readonly Mock<ILogSender> _mockSender = new();

    [Fact]
    public void CreateLogger_ReturnsSameInstanceForSameCategory()
    {
        var options = new CloudWatchLoggerOptions();
        var provider = new CloudWatchLoggerProvider(options, _mockSender.Object);

        var logger1 = provider.CreateLogger("Test");
        var logger2 = provider.CreateLogger("Test");

        Assert.Same(logger1, logger2);
    }

    [Fact]
    public async Task FlushAsync_SendsAllLogsToAllLogsGroup()
    {
        _mockSender.Setup(s => s.EnsureLogStreamExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyList<LogEntry>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new CloudWatchLoggerOptions
        {
            AllLogsGroupName = "/test/all-logs",
            ErrorLogsGroupName = "/test/error-logs"
        };
        var provider = new CloudWatchLoggerProvider(options, _mockSender.Object);

        var logger = provider.CreateLogger("Test");
        logger.LogInformation("Info message");

        await provider.FlushAsync();

        // All logs sent to all-logs group
        _mockSender.Verify(s => s.SendAsync(
            It.Is<IReadOnlyList<LogEntry>>(e => e.Count == 1),
            "/test/all-logs",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Info log NOT sent to error group
        _mockSender.Verify(s => s.SendAsync(
            It.IsAny<IReadOnlyList<LogEntry>>(),
            "/test/error-logs",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FlushAsync_ErrorLogs_SentToBothGroups()
    {
        _mockSender.Setup(s => s.EnsureLogStreamExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyList<LogEntry>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new CloudWatchLoggerOptions
        {
            AllLogsGroupName = "/test/all-logs",
            ErrorLogsGroupName = "/test/error-logs"
        };
        var provider = new CloudWatchLoggerProvider(options, _mockSender.Object);

        var logger = provider.CreateLogger("Test");
        logger.LogError("Error message");

        await provider.FlushAsync();

        // All logs sent to all-logs group
        _mockSender.Verify(s => s.SendAsync(
            It.Is<IReadOnlyList<LogEntry>>(e => e.Count == 1),
            "/test/all-logs",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Error logs also sent to error group
        _mockSender.Verify(s => s.SendAsync(
            It.Is<IReadOnlyList<LogEntry>>(e => e.Count == 1),
            "/test/error-logs",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FlushAsync_EmptyBuffer_DoesNotCallSender()
    {
        var options = new CloudWatchLoggerOptions();
        var provider = new CloudWatchLoggerProvider(options, _mockSender.Object);

        await provider.FlushAsync();

        _mockSender.Verify(s => s.SendAsync(
            It.IsAny<IReadOnlyList<LogEntry>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
