using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging.Tests;

public class LogBufferTests
{
    [Fact]
    public void Add_SingleEntry_CountIsOne()
    {
        var buffer = new LogBuffer(100);
        buffer.Add(CreateEntry());
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void Drain_ReturnsAllEntries_AndClearsBuffer()
    {
        var buffer = new LogBuffer(100);
        buffer.Add(CreateEntry("msg1"));
        buffer.Add(CreateEntry("msg2"));

        var entries = buffer.Drain();

        Assert.Equal(2, entries.Count);
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Add_ExceedsMaxSize_DiscardsOldest()
    {
        var buffer = new LogBuffer(2);
        buffer.Add(CreateEntry("msg1"));
        buffer.Add(CreateEntry("msg2"));
        buffer.Add(CreateEntry("msg3"));

        var entries = buffer.Drain();

        Assert.Equal(2, entries.Count);
        // Oldest should be discarded
        Assert.Equal("msg2", entries[0].Message);
        Assert.Equal("msg3", entries[1].Message);
    }

    [Fact]
    public void Drain_EmptyBuffer_ReturnsEmptyList()
    {
        var buffer = new LogBuffer(100);

        var entries = buffer.Drain();

        Assert.Empty(entries);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var buffer = new LogBuffer(100);
        buffer.Add(CreateEntry());
        buffer.Add(CreateEntry());

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Add_ConcurrentAccess_NoDataCorruption()
    {
        var buffer = new LogBuffer(10000);
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => buffer.Add(CreateEntry($"msg{i}"))))
            .ToArray();
        Task.WaitAll(tasks);

        Assert.Equal(100, buffer.Count);
    }

    private static LogEntry CreateEntry(string msg = "test") => new()
    {
        Timestamp = DateTime.UtcNow,
        Level = LogLevel.Information,
        Category = "Test",
        Message = msg
    };
}
