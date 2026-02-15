using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging.Tests;

public class JsonLogFormatterTests
{
    private readonly JsonLogFormatter _formatter = new();

    [Fact]
    public void Format_BasicEntry_ReturnsValidJson()
    {
        var entry = new LogEntry
        {
            Timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Level = LogLevel.Information,
            Category = "TestCategory",
            Message = "Hello World"
        };

        var json = _formatter.Format(entry);

        Assert.Contains("\"level\":\"Information\"", json);
        Assert.Contains("\"message\":\"Hello World\"", json);
        Assert.Contains("\"category\":\"TestCategory\"", json);
        // Should be valid JSON
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void Format_WithException_IncludesExceptionDetail()
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Error,
            Category = "Test",
            Message = "Error occurred",
            ExceptionDetail = "System.Exception: test\n   at Test.Method()"
        };

        var json = _formatter.Format(entry);

        Assert.Contains("\"exception\":", json);
        Assert.Contains("System.Exception", json);
    }

    [Fact]
    public void Format_WithProperties_SerializesProperties()
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Category = "Test",
            Message = "Hello",
            Properties = new Dictionary<string, object> { ["requestId"] = "abc-123" }
        };

        var json = _formatter.Format(entry);

        Assert.Contains("\"properties\":", json);
        Assert.Contains("abc-123", json);
    }

    [Fact]
    public void Format_NullProperties_OmitsPropertiesField()
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Category = "Test",
            Message = "Hello",
            Properties = null
        };

        var json = _formatter.Format(entry);

        // With JsonIgnoreCondition.WhenWritingNull, properties should be omitted
        Assert.DoesNotContain("\"properties\":", json);
    }

    [Fact]
    public void Format_SpecialCharacters_ProducesValidJson()
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Category = "Test",
            Message = "Line1\nLine2\t\"quoted\""
        };

        var json = _formatter.Format(entry);

        // Must be parseable
        var doc = JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("message").GetString();
        Assert.Contains("Line1\nLine2", message);
    }
}
