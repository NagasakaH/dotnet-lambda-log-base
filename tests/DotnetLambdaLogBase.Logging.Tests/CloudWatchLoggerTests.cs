using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging.Tests;

public class CloudWatchLoggerTests
{
    [Fact]
    public void Log_InformationLevel_AddsToBuffer()
    {
        var buffer = new LogBuffer(100);
        var options = new CloudWatchLoggerOptions { MinimumLevel = LogLevel.Information };
        var logger = new CloudWatchLogger("TestCategory", buffer, options);

        logger.LogInformation("Hello World");

        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotAddToBuffer()
    {
        var buffer = new LogBuffer(100);
        var options = new CloudWatchLoggerOptions { MinimumLevel = LogLevel.Warning };
        var logger = new CloudWatchLogger("TestCategory", buffer, options);

        logger.LogInformation("Should be filtered");

        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void IsEnabled_AboveMinimum_ReturnsTrue()
    {
        var options = new CloudWatchLoggerOptions { MinimumLevel = LogLevel.Information };
        var logger = new CloudWatchLogger("Test", new LogBuffer(100), options);

        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void IsEnabled_BelowMinimum_ReturnsFalse()
    {
        var options = new CloudWatchLoggerOptions { MinimumLevel = LogLevel.Warning };
        var logger = new CloudWatchLogger("Test", new LogBuffer(100), options);

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void IsEnabled_None_ReturnsFalse()
    {
        var options = new CloudWatchLoggerOptions { MinimumLevel = LogLevel.Information };
        var logger = new CloudWatchLogger("Test", new LogBuffer(100), options);

        Assert.False(logger.IsEnabled(LogLevel.None));
    }

    [Fact]
    public void Log_WithException_IncludesExceptionDetail()
    {
        var buffer = new LogBuffer(100);
        var options = new CloudWatchLoggerOptions();
        var logger = new CloudWatchLogger("Test", buffer, options);

        try { throw new InvalidOperationException("test error"); }
        catch (Exception ex) { logger.LogError(ex, "Error occurred"); }

        var entries = buffer.Drain();
        Assert.Single(entries);
        Assert.NotNull(entries[0].ExceptionDetail);
        Assert.Contains("InvalidOperationException", entries[0].ExceptionDetail);
    }

    [Fact]
    public void Log_SetsCorrectCategory()
    {
        var buffer = new LogBuffer(100);
        var options = new CloudWatchLoggerOptions();
        var logger = new CloudWatchLogger("MyApp.Services.UserService", buffer, options);

        logger.LogInformation("Test");

        var entries = buffer.Drain();
        Assert.Equal("MyApp.Services.UserService", entries[0].Category);
    }
}
