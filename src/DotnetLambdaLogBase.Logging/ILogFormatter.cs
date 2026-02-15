namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Formats log entries into string representation.
/// </summary>
public interface ILogFormatter
{
    string Format(LogEntry entry);
}
