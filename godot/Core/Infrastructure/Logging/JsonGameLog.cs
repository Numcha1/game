using System.Text.Json;
using System.Threading.Channels;
using SeasonsTogether.Core.Application;

namespace SeasonsTogether.Core.Infrastructure.Logging;

/// <summary>Bounded, asynchronous, rotating diagnostics. No identities, bearer secrets or request bodies.</summary>
public sealed class JsonGameLog : IGameLog, IAsyncDisposable
{
    readonly record struct Entry(DateTimeOffset Time, string Level, string Code, string? ExceptionType);
    readonly Channel<Entry> entries = Channel.CreateBounded<Entry>(new BoundedChannelOptions(128) {
        FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
    readonly string path;
    readonly Task writer;
    public JsonGameLog(string directory)
    {
        Directory.CreateDirectory(directory); path = Path.Combine(directory, "events.jsonl"); writer = Task.Run(Drain);
    }
    public void Write(string level, string code, string? exceptionType = null) => entries.Writer.TryWrite(new(DateTimeOffset.UtcNow, level, code, exceptionType));
    async Task Drain()
    {
        await foreach (var entry in entries.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024) File.Move(path, path + ".1", true);
                await File.AppendAllTextAsync(path, JsonSerializer.Serialize(entry) + Environment.NewLine).ConfigureAwait(false);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* Optional logging cannot prevent gameplay or saving. */ }
        }
    }
    public async ValueTask DisposeAsync() { entries.Writer.TryComplete(); await writer.ConfigureAwait(false); }
}
