using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Represents a single log entry to be sent to CloudWatch Logs.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ExceptionDetail { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
