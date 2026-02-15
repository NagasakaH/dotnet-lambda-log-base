using System.Collections.Concurrent;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Thread-safe log buffer using ConcurrentQueue.
/// Discards oldest entries when max size is exceeded.
/// </summary>
public class LogBuffer
{
    private readonly ConcurrentQueue<LogEntry> _queue = new();
    private readonly int _maxSize;

    public LogBuffer(int maxSize = 10000)
    {
        _maxSize = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize));
    }

    public int Count => _queue.Count;

    public void Add(LogEntry entry)
    {
        _queue.Enqueue(entry);

        // Discard oldest if over max size
        while (_queue.Count > _maxSize && _queue.TryDequeue(out _))
        {
        }
    }

    public List<LogEntry> Drain()
    {
        var entries = new List<LogEntry>();
        while (_queue.TryDequeue(out var entry))
        {
            entries.Add(entry);
        }
        return entries;
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
        }
    }
}
