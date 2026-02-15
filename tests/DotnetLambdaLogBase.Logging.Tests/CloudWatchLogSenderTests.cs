using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetLambdaLogBase.Logging.Tests;

public class CloudWatchLogSenderTests
{
    private readonly Mock<IAmazonCloudWatchLogs> _mockClient = new();

    [Fact]
    public async Task SendAsync_WithEntries_CallsPutLogEvents()
    {
        _mockClient.Setup(c => c.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutLogEventsResponse());

        var sender = new CloudWatchLogSender(_mockClient.Object);
        var entries = new List<LogEntry> { CreateEntry(LogLevel.Information, "Hello") };

        await sender.SendAsync(entries, "/test/group", "test-stream");

        _mockClient.Verify(c => c.PutLogEventsAsync(
            It.Is<PutLogEventsRequest>(r => r.LogGroupName == "/test/group" && r.LogStreamName == "test-stream"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_EmptyList_DoesNotCallApi()
    {
        var sender = new CloudWatchLogSender(_mockClient.Object);

        await sender.SendAsync(new List<LogEntry>(), "/test/group", "test-stream");

        _mockClient.Verify(c => c.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_ApiFailure_DoesNotThrow()
    {
        _mockClient.Setup(c => c.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonCloudWatchLogsException("error"));

        var sender = new CloudWatchLogSender(_mockClient.Object);
        var entries = new List<LogEntry> { CreateEntry(LogLevel.Information, "Hello") };

        var ex = await Record.ExceptionAsync(() => sender.SendAsync(entries, "/test/group", "test-stream"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task EnsureLogStreamExistsAsync_CallsCreateLogStream()
    {
        _mockClient.Setup(c => c.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateLogStreamResponse());

        var sender = new CloudWatchLogSender(_mockClient.Object);

        await sender.EnsureLogStreamExistsAsync("/test/group", "test-stream");

        _mockClient.Verify(c => c.CreateLogStreamAsync(
            It.Is<CreateLogStreamRequest>(r => r.LogGroupName == "/test/group" && r.LogStreamName == "test-stream"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureLogStreamExistsAsync_AlreadyExists_DoesNotThrow()
    {
        _mockClient.Setup(c => c.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceAlreadyExistsException("exists"));

        var sender = new CloudWatchLogSender(_mockClient.Object);

        var ex = await Record.ExceptionAsync(() => sender.EnsureLogStreamExistsAsync("/test/group", "test-stream"));

        Assert.Null(ex);
    }

    private static LogEntry CreateEntry(LogLevel level, string msg) => new()
    {
        Timestamp = DateTime.UtcNow,
        Level = level,
        Category = "Test",
        Message = msg
    };
}
