using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// ILogger implementation that buffers log entries for batch sending to CloudWatch Logs.
/// </summary>
public class CloudWatchLogger : ILogger
{
    private readonly string _category;
    private readonly LogBuffer _buffer;
    private readonly CloudWatchLoggerOptions _options;

    public CloudWatchLogger(string category, LogBuffer buffer, CloudWatchLoggerOptions options)
    {
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _options.MinimumLevel && logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel,
            Category = _category,
            Message = formatter(state, exception),
            ExceptionDetail = exception?.ToString()
        };

        _buffer.Add(entry);
    }
}
