namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Sends log entries to CloudWatch Logs.
/// </summary>
public interface ILogSender
{
    Task SendAsync(IReadOnlyList<LogEntry> entries, string logGroupName, string logStreamName, CancellationToken cancellationToken = default);
    Task EnsureLogStreamExistsAsync(string logGroupName, string logStreamName, CancellationToken cancellationToken = default);
}
