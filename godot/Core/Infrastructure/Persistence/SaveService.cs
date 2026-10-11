using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SuanPleng;

namespace SeasonsTogether.Core.Infrastructure.Persistence;

/// <summary>Captures on the session thread; a bounded worker owns every disk replacement.</summary>
public sealed class SaveService : ISaveService
{
    const int MaximumSaveBytes = 2 * 1024 * 1024;
    readonly record struct Snapshot(long Revision, byte[] Bytes);
    readonly Channel<Snapshot> queue = Channel.CreateBounded<Snapshot>(new BoundedChannelOptions(1) {
        FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false });
    readonly ConcurrentQueue<string> failures = new();
    readonly object gate = new();
    readonly SemaphoreSlim storageGate = new(1);
    readonly IGameLog log;
    readonly Task writer;
    TaskCompletionSource progress = NewProgress();
    long requested, completed;
    Exception? lastError;
    bool closing, importing;
    public string SavePath { get; }
    string IdentityPath => Path.Combine(Path.GetDirectoryName(SavePath)!, "player.txt");
    public SaveService(string directory, IGameLog? logger = null)
    {
        Directory.CreateDirectory(directory);
        SavePath = Path.Combine(Path.GetFullPath(directory), "farm.json");
        log = logger ?? NullGameLog.Instance;
        writer = Task.Run(WriteLoop);
    }
    static TaskCompletionSource NewProgress() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    public static Farm Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumSaveBytes) throw new InvalidDataException("เซฟมีขนาดเกินขอบเขตที่รองรับ");
        var farm = JsonSerializer.Deserialize<Farm>(bytes) ?? throw new InvalidDataException("เซฟไม่สมบูรณ์");
        FarmInvariants.Validate(farm); return farm;
    }
    public Farm Load()
    {
        var farm = File.Exists(SavePath) ? Deserialize(ReadBounded(SavePath)) : new Farm();
        foreach (var player in farm.Players) { player.LastSeen = 0; player.Sleeping = false; }
        farm.Paused = false; return farm;
    }
    static byte[] ReadBounded(string path)
    {
        if (new FileInfo(path).Length > MaximumSaveBytes) throw new InvalidDataException("เซฟมีขนาดเกินขอบเขตที่รองรับ");
        return File.ReadAllBytes(path);
    }
    public string LoadPlayerSession()
    {
        string identity = File.Exists(IdentityPath) ? File.ReadAllText(IdentityPath).Trim() : "";
        if (Guid.TryParseExact(identity, "N", out _)) return identity;
        identity = Guid.NewGuid().ToString("N");
        // Startup only; identity is not written every frame or every load.
        File.WriteAllText(IdentityPath, identity); return identity;
    }
    public void RequestSave(Farm state)
    {
        FarmInvariants.Validate(state);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(state);
        if (bytes.Length > MaximumSaveBytes) throw new InvalidDataException("เซฟมีขนาดเกินขอบเขตที่รองรับ");
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (importing) throw new InvalidOperationException("กำลังนำเข้าเซฟ");
            if (!queue.Writer.TryWrite(new Snapshot(++requested, bytes))) throw new IOException("ช่องบันทึกปิดแล้ว");
        }
    }
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        long target;
        lock (gate) target = requested;
        while (true)
        {
            Task wait;
            lock (gate)
            {
                if (completed >= target)
                {
                    if (lastError != null) throw new IOException("บันทึกไม่สำเร็จ เซฟล่าสุดยังอยู่ โปรดลองอีกครั้ง", lastError);
                    return;
                }
                wait = progress.Task;
            }
            await wait.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    async Task WriteLoop()
    {
        await foreach (var snapshot in queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            Exception? error = null;
            await storageGate.WaitAsync().ConfigureAwait(false);
            try { await ReplaceAsync(SavePath, snapshot.Bytes, SavePath + ".bak").ConfigureAwait(false); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                error = e;
                if (failures.Count < 8) failures.Enqueue("บันทึกไม่ได้ ตรวจพื้นที่ว่างและสิทธิ์โฟลเดอร์เซฟ");
                log.Write("error", "save.failed", e.GetType().Name);
            }
            finally { storageGate.Release(); }
            lock (gate)
            {
                completed = snapshot.Revision; lastError = error;
                var previous = progress; progress = NewProgress(); previous.TrySetResult();
            }
        }
    }
    static async Task ReplaceAsync(string path, byte[] bytes, string? backup = null)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                8192, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            if (File.Exists(path)) File.Replace(temporary, path, backup, true);
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public bool TryReadFailure(out string error) => failures.TryDequeue(out error!);
    public async Task ImportLegacyAsync(string sourceDirectory, CancellationToken cancellationToken = default)
    {
        lock (gate) { ObjectDisposedException.ThrowIf(closing, this); if (importing) throw new InvalidOperationException("กำลังนำเข้าเซฟ"); importing = true; }
        try
        {
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            string sourceFarm = Path.Combine(sourceDirectory, "farm.json"), sourceIdentity = Path.Combine(sourceDirectory, "player.txt");
            if (Path.GetFullPath(sourceFarm).Equals(SavePath, StringComparison.OrdinalIgnoreCase)) throw new IOException("เลือกเซฟต้นทางคนละโปรไฟล์");
            byte[] bytes = await Task.Run(() => ReadBounded(sourceFarm), cancellationToken).ConfigureAwait(false);
            _ = Deserialize(bytes);
            byte[]? identity = File.Exists(sourceIdentity) ? await File.ReadAllBytesAsync(sourceIdentity, cancellationToken).ConfigureAwait(false) : null;
            if (identity != null && !Guid.TryParseExact(System.Text.Encoding.UTF8.GetString(identity).Trim(), "N", out _)) throw new InvalidDataException("identity เซฟเดิมไม่ถูกต้อง");
            await storageGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                byte[]? original = File.Exists(SavePath) ? await File.ReadAllBytesAsync(SavePath, cancellationToken).ConfigureAwait(false) : null;
                byte[]? oldIdentity = File.Exists(IdentityPath) ? await File.ReadAllBytesAsync(IdentityPath, cancellationToken).ConfigureAwait(false) : null;
                string backup = SavePath + ".before-import-" + Guid.NewGuid().ToString("N");
                if (original != null) await ReplaceAsync(backup, original).ConfigureAwait(false);
                if (oldIdentity != null) await ReplaceAsync(backup + ".player", oldIdentity).ConfigureAwait(false);
                try
                {
                    // Once replacement starts, complete or rollback both files, even if UI cancellation arrives.
                    await ReplaceAsync(SavePath, bytes, SavePath + ".bak").ConfigureAwait(false);
                    if (identity != null) await ReplaceAsync(IdentityPath, identity).ConfigureAwait(false);
                }
                catch
                {
                    if (original != null) await ReplaceAsync(SavePath, original).ConfigureAwait(false);
                    else if (File.Exists(SavePath)) File.Delete(SavePath);
                    if (oldIdentity != null) await ReplaceAsync(IdentityPath, oldIdentity).ConfigureAwait(false);
                    else if (File.Exists(IdentityPath)) File.Delete(IdentityPath);
                    throw;
                }
                log.Write("info", "save.imported");
            }
            finally { storageGate.Release(); }
        }
        finally { lock (gate) importing = false; }
    }
    public async ValueTask DisposeAsync()
    {
        lock (gate) { if (closing) return; closing = true; queue.Writer.TryComplete(); }
        await writer.ConfigureAwait(false);
        storageGate.Dispose();
    }
}
