using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Sends log entries to CloudWatch Logs using PutLogEvents API.
/// Swallows exceptions to prevent logging failures from affecting the application.
/// </summary>
public class CloudWatchLogSender : ILogSender
{
    private readonly IAmazonCloudWatchLogs _client;
    private readonly ILogFormatter _formatter;

    public CloudWatchLogSender(IAmazonCloudWatchLogs client, ILogFormatter? formatter = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _formatter = formatter ?? new JsonLogFormatter();
    }

    public async Task SendAsync(IReadOnlyList<LogEntry> entries, string logGroupName, string logStreamName, CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0) return;

        try
        {
            var logEvents = entries
                .OrderBy(e => e.Timestamp)
                .Select(e => new InputLogEvent
                {
                    Timestamp = e.Timestamp,
                    Message = _formatter.Format(e)
                })
                .ToList();

            var batches = SplitIntoBatches(logEvents);

            foreach (var batch in batches)
            {
                var request = new PutLogEventsRequest
                {
                    LogGroupName = logGroupName,
                    LogStreamName = logStreamName,
                    LogEvents = batch
                };

                await _client.PutLogEventsAsync(request, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Swallow exceptions - logging should not break the application
            Console.Error.WriteLine($"[CloudWatchLogSender] Failed to send logs: {ex.Message}");
        }
    }

    public async Task EnsureLogStreamExistsAsync(string logGroupName, string logStreamName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.CreateLogStreamAsync(new CreateLogStreamRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName
            }, cancellationToken);
        }
        catch (ResourceAlreadyExistsException)
        {
            // Stream already exists - this is fine
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CloudWatchLogSender] Failed to create log stream: {ex.Message}");
        }
    }

    private static List<List<InputLogEvent>> SplitIntoBatches(List<InputLogEvent> events)
    {
        const int maxBatchCount = 10000;
        const int maxBatchBytes = 1_048_576; // 1MB
        const int maxEventBytes = 262_144;   // 256KB per event
        const int eventOverhead = 26; // bytes per event overhead

        var batches = new List<List<InputLogEvent>>();
        var currentBatch = new List<InputLogEvent>();
        var currentBatchSize = 0;

        foreach (var evt in events)
        {
            var eventSize = System.Text.Encoding.UTF8.GetByteCount(evt.Message) + eventOverhead;

            // Truncate oversized events to 256KB limit
            if (eventSize > maxEventBytes)
            {
                var maxMessageBytes = maxEventBytes - eventOverhead;
                var truncated = TruncateUtf8(evt.Message, maxMessageBytes);
                evt.Message = truncated + "... [TRUNCATED]";
                eventSize = System.Text.Encoding.UTF8.GetByteCount(evt.Message) + eventOverhead;
                Console.Error.WriteLine("[CloudWatchLogSender] Log event exceeded 256KB limit, truncated");
            }

            if (currentBatch.Count >= maxBatchCount ||
                (currentBatchSize + eventSize > maxBatchBytes && currentBatch.Count > 0))
            {
                batches.Add(currentBatch);
                currentBatch = new List<InputLogEvent>();
                currentBatchSize = 0;
            }

            currentBatch.Add(evt);
            currentBatchSize += eventSize;
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    private static string TruncateUtf8(string input, int maxBytes)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        if (bytes.Length <= maxBytes) return input;

        // Find valid UTF-8 boundary
        var length = maxBytes;
        while (length > 0 && (bytes[length] & 0xC0) == 0x80)
            length--;

        return System.Text.Encoding.UTF8.GetString(bytes, 0, length);
    }
}
