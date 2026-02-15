using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Formats log entries as JSON strings for CloudWatch Logs.
/// </summary>
public class JsonLogFormatter : ILogFormatter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public string Format(LogEntry entry)
    {
        var output = new JsonLogOutput
        {
            Timestamp = entry.Timestamp.ToString("O"),
            Level = entry.Level.ToString(),
            Category = entry.Category,
            Message = entry.Message,
            Exception = entry.ExceptionDetail,
            Properties = entry.Properties
        };

        return JsonSerializer.Serialize(output, s_options);
    }

    private class JsonLogOutput
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }
}
