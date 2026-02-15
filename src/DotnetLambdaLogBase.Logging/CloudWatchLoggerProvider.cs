using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// ILoggerProvider that creates CloudWatchLogger instances and manages flush lifecycle.
/// On FlushAsync, sends all logs to all-logs group and error+ logs to error group.
/// </summary>
public class CloudWatchLoggerProvider : ILoggerProvider, IAsyncDisposable
{
    private readonly CloudWatchLoggerOptions _options;
    private readonly ILogSender _sender;
    private readonly LogBuffer _buffer;
    private readonly ConcurrentDictionary<string, CloudWatchLogger> _loggers = new();
    private readonly string _logStreamName;
    private bool _disposed;

    public CloudWatchLoggerProvider(CloudWatchLoggerOptions options, ILogSender sender)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _buffer = new LogBuffer(options.MaxBufferSize);

        var functionName = options.FunctionName ?? "unknown";
        _logStreamName = $"{DateTime.UtcNow:yyyy/MM/dd}/{functionName}/{Guid.NewGuid():N}";
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new CloudWatchLogger(name, _buffer, _options));
    }

    /// <summary>
    /// Flushes buffered logs to CloudWatch Logs.
    /// All logs go to all-logs group; Error+ logs also go to error group.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var entries = _buffer.Drain();
        if (entries.Count == 0) return;

        // Ensure log streams exist
        await _sender.EnsureLogStreamExistsAsync(_options.AllLogsGroupName, _logStreamName, cancellationToken);

        // Send all logs to all-logs group
        await _sender.SendAsync(entries, _options.AllLogsGroupName, _logStreamName, cancellationToken);

        // Filter error+ logs and send to error group
        var errorEntries = entries.Where(e => e.Level >= _options.ErrorGroupMinimumLevel).ToList();
        if (errorEntries.Count > 0)
        {
            await _sender.EnsureLogStreamExistsAsync(_options.ErrorLogsGroupName, _logStreamName, cancellationToken);
            await _sender.SendAsync(errorEntries, _options.ErrorLogsGroupName, _logStreamName, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await FlushAsync();
        _disposed = true;
    }
}
