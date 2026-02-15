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
        const int eventOverhead = 26; // bytes per event overhead

        var batches = new List<List<InputLogEvent>>();
        var currentBatch = new List<InputLogEvent>();
        var currentBatchSize = 0;

        foreach (var evt in events)
        {
            var eventSize = System.Text.Encoding.UTF8.GetByteCount(evt.Message) + eventOverhead;

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
}
