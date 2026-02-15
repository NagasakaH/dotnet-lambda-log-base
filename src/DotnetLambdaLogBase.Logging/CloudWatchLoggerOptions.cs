using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Configuration options for CloudWatch Logger.
/// </summary>
public class CloudWatchLoggerOptions
{
    /// <summary>CloudWatch Logs group for all logs (DELIVERY class).</summary>
    public string AllLogsGroupName { get; set; } = "/lambda/app/all-logs";

    /// <summary>CloudWatch Logs group for error logs (STANDARD class, shared).</summary>
    public string ErrorLogsGroupName { get; set; } = "/lambda/shared/error-logs";

    /// <summary>Lambda function name (used for log stream naming).</summary>
    public string? FunctionName { get; set; }

    /// <summary>Minimum log level to record.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>Minimum log level to send to error log group.</summary>
    public LogLevel ErrorGroupMinimumLevel { get; set; } = LogLevel.Error;

    /// <summary>Maximum buffer size before oldest entries are discarded.</summary>
    public int MaxBufferSize { get; set; } = 10000;
}
