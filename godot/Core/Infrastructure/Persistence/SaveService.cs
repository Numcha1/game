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
    const int MaximumIdentityBytes = 256;
    readonly record struct Snapshot(long Revision, byte[] Bytes, bool Extended);
    BuildingState loadedBuildings = new();
    public BuildingState LoadBuildings() => loadedBuildings;
    readonly Channel<Snapshot> queue = Channel.CreateBounded<Snapshot>(new BoundedChannelOptions(1) {
        FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false });
    readonly ConcurrentQueue<string> failures = new();
    readonly object gate = new();
    readonly SemaphoreSlim storageGate = new(1);
    readonly IGameLog log;
    readonly Task writer;
    readonly FileStream profileLease;
    TaskCompletionSource progress = NewProgress();
    TaskCompletionSource? importFinished;
    long requested, completed;
    Exception? lastError;
    bool closing, importing;
    public string SavePath { get; }
    string IdentityPath => Path.Combine(Path.GetDirectoryName(SavePath)!, "player.txt");
    string ImportJournalPath => SavePath + ".import-pending";
    sealed record ImportJournal(int Version, byte[]? Farm, byte[]? Identity);
    public SaveService(string directory, IGameLog? logger = null)
    {
        Directory.CreateDirectory(directory);
        SavePath = Path.Combine(Path.GetFullPath(directory), "farm.json");
        log = logger ?? NullGameLog.Instance;
        profileLease = AcquireProfile(SavePath);
        try { RecoverInterruptedImport(); writer = Task.Run(WriteLoop); }
        catch { profileLease.Dispose(); throw; }
    }
    static FileStream AcquireProfile(string path) => new(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    static TaskCompletionSource NewProgress() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    public static Farm Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumSaveBytes) throw new InvalidDataException("เซฟมีขนาดเกินขอบเขตที่รองรับ");
        return BuildingSaveDocument.Decode(bytes).Farm;
    }
    public Farm Load()
    {
        var loaded = File.Exists(SavePath) ? BuildingSaveDocument.Decode(ReadBounded(SavePath)) : (new Farm(), new BuildingState());
        var farm=loaded.Item1;loadedBuildings=loaded.Item2;
        foreach (var player in farm.Players) { player.LastSeen = 0; player.Sleeping = false; }
        farm.Paused = false; return farm;
    }
    static byte[] ReadBounded(string path, int maximum = MaximumSaveBytes)
    {
        if (new FileInfo(path).Length > maximum) throw new InvalidDataException("เซฟมีขนาดเกินขอบเขตที่รองรับ");
        return File.ReadAllBytes(path);
    }
    public string LoadPlayerSession()
    {
        string identity = File.Exists(IdentityPath) ? DecodeIdentity(ReadBounded(IdentityPath, MaximumIdentityBytes)) : "";
        if (Guid.TryParseExact(identity, "N", out _)) {
            if (File.Exists(SavePath)) {
                var farm = Deserialize(ReadBounded(SavePath));
                if (farm.Players.Count > 0 && farm.Find(identity) == null)
                    throw new InvalidDataException("player.txt ไม่ตรงกับผู้เล่นในฟาร์ม โปรดกู้คืนไฟล์ที่เป็นคู่กัน");
            }
            return identity;
        }
        if (File.Exists(IdentityPath) || (File.Exists(SavePath) && Deserialize(ReadBounded(SavePath)).Players.Count > 0))
            throw new InvalidDataException("ข้อมูลผู้เล่นสูญหายหรือเสียหาย โปรดกู้คืน player.txt คู่กับเซฟเดิม ไม่มีการสร้างผู้เล่นใหม่ทับข้อมูล");
        identity = Guid.NewGuid().ToString("N");
        // Startup only; identity is not written every frame or every load.
        File.WriteAllText(IdentityPath, identity); return identity;
    }
    static string DecodeIdentity(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), System.Text.Encoding.UTF8, true);
        return reader.ReadToEnd().Trim();
    }
    public void RequestSave(Farm state)
        => RequestSave(state,loadedBuildings);
    public void RequestSave(Farm state, BuildingState buildings)
    {
        FarmInvariants.Validate(state);
        byte[] bytes = BuildingSaveDocument.Encode(state,buildings);
        if (bytes.Length > MaximumSaveBytes) throw new InvalidDataException("เซฟมีขนาดเกินขอบเขตที่รองรับ");
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (importing) throw new InvalidOperationException("กำลังนำเข้าเซฟ");
            if (!queue.Writer.TryWrite(new Snapshot(++requested, bytes, buildings.Interiors.Count>0))) throw new IOException("ช่องบันทึกปิดแล้ว");
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
            try {
                string original=SavePath+".v1-before-buildings";
                if(snapshot.Extended && File.Exists(SavePath) && !File.Exists(original)) {
                    byte[] previous=ReadBounded(SavePath);
                    if(BuildingSaveDocument.Decode(previous).Buildings.Interiors.Count==0)
                        await ReplaceAsync(original,previous).ConfigureAwait(false);
                }
                await ReplaceAsync(SavePath, snapshot.Bytes, SavePath + ".bak").ConfigureAwait(false);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
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
        lock (gate) { ObjectDisposedException.ThrowIf(closing, this); if (importing) throw new InvalidOperationException("กำลังนำเข้าเซฟ"); importing = true; importFinished = NewProgress(); }
        try
        {
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            string sourceFarm = Path.Combine(sourceDirectory, "farm.json"), sourceIdentity = Path.Combine(sourceDirectory, "player.txt");
            if (Path.GetFullPath(sourceFarm).Equals(SavePath, StringComparison.OrdinalIgnoreCase)) throw new IOException("เลือกเซฟต้นทางคนละโปรไฟล์");
            byte[] bytes = await Task.Run(() => ReadBounded(sourceFarm), cancellationToken).ConfigureAwait(false);
            var importedFarm = Deserialize(bytes);
            byte[]? identity = File.Exists(sourceIdentity) ? await Task.Run(() => ReadBounded(sourceIdentity, MaximumIdentityBytes), cancellationToken).ConfigureAwait(false) : null;
            if (identity != null && !Guid.TryParseExact(DecodeIdentity(identity), "N", out _)) throw new InvalidDataException("identity เซฟเดิมไม่ถูกต้อง");
            if (importedFarm.Players.Count > 0 && (identity == null || importedFarm.Find(DecodeIdentity(identity)) == null))
                throw new InvalidDataException("เซฟเดิมต้องมี player.txt ที่ตรงกับผู้เล่นในฟาร์ม");
            await storageGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                byte[]? original = File.Exists(SavePath) ? ReadBounded(SavePath) : null;
                byte[]? oldIdentity = File.Exists(IdentityPath) ? ReadBounded(IdentityPath, MaximumIdentityBytes) : null;
                string backup = SavePath + ".before-import-" + Guid.NewGuid().ToString("N");
                if (original != null) await ReplaceAsync(backup, original).ConfigureAwait(false);
                if (oldIdentity != null) await ReplaceAsync(backup + ".player", oldIdentity).ConfigureAwait(false);
                ValidateJournal(new ImportJournal(1, original, oldIdentity));
                // A durable undo record is installed before either live file changes. Deleting it commits the pair.
                await ReplaceAsync(ImportJournalPath, JsonSerializer.SerializeToUtf8Bytes(new ImportJournal(1, original, oldIdentity))).ConfigureAwait(false);
                try
                {
                    // Once replacement starts, complete or rollback both files, even if UI cancellation arrives.
                    await ReplaceAsync(SavePath, bytes, SavePath + ".bak").ConfigureAwait(false);
                    if (identity != null) await ReplaceAsync(IdentityPath, identity).ConfigureAwait(false);
                    File.Delete(ImportJournalPath);
                }
                catch
                {
                    if (original != null) await ReplaceAsync(SavePath, original).ConfigureAwait(false);
                    else if (File.Exists(SavePath)) File.Delete(SavePath);
                    if (oldIdentity != null) await ReplaceAsync(IdentityPath, oldIdentity).ConfigureAwait(false);
                    else if (File.Exists(IdentityPath)) File.Delete(IdentityPath);
                    File.Delete(ImportJournalPath);
                    throw;
                }
                log.Write("info", "save.imported");
            }
            finally { storageGate.Release(); }
        }
        finally { lock (gate) { importing = false; importFinished!.TrySetResult(); } }
    }
    static void ValidateJournal(ImportJournal journal)
    {
        if (journal.Version != 1) throw new InvalidDataException("เวอร์ชันบันทึกกู้คืนไม่รองรับ");
        if (journal.Farm != null) _ = Deserialize(journal.Farm);
        if (journal.Identity != null && (journal.Identity.Length > MaximumIdentityBytes ||
            !Guid.TryParseExact(DecodeIdentity(journal.Identity), "N", out _)))
            throw new InvalidDataException("ข้อมูลผู้เล่นในบันทึกกู้คืนไม่ถูกต้อง");
    }
    void RecoverInterruptedImport()
    {
        if (!File.Exists(ImportJournalPath)) return;
        if (new FileInfo(ImportJournalPath).Length > MaximumSaveBytes * 2)
            throw new InvalidDataException("บันทึกกู้คืนมีขนาดเกินขอบเขตที่รองรับ");
        var journal = SaveJsonContract.Read<ImportJournal>(File.ReadAllBytes(ImportJournalPath),"Version","Farm","Identity");
        ValidateJournal(journal); // Validate both files before touching either; no paths come from the journal.
        Restore(SavePath, journal.Farm);
        Restore(IdentityPath, journal.Identity);
        File.Delete(ImportJournalPath);
        log.Write("warning", "save.import.recovered");
    }
    static void Restore(string path, byte[]? bytes)
    {
        if (bytes != null) ReplaceAsync(path, bytes).GetAwaiter().GetResult();
        else if (File.Exists(path)) File.Delete(path);
    }
    public bool CanRestoreBackup
    {
        get { try { ValidateBackup(SavePath); return !File.Exists(ImportJournalPath); }
            catch (Exception e) when (e is IOException or InvalidDataException or JsonException or UnauthorizedAccessException) { return false; } }
    }
    static byte[] ValidateBackup(string savePath)
    {
        byte[] bytes = ReadBounded(savePath + ".bak");
        var farm = Deserialize(bytes);
        string identityPath = Path.Combine(Path.GetDirectoryName(savePath)!, "player.txt");
        if (farm.Players.Count > 0 || File.Exists(identityPath))
        {
            string identity = DecodeIdentity(ReadBounded(identityPath, MaximumIdentityBytes));
            if (!Guid.TryParseExact(identity, "N", out _) || farm.Players.Count > 0 && farm.Find(identity) == null)
                throw new InvalidDataException("ข้อมูลผู้เล่นไม่ตรงกับเซฟสำรอง ต้องกู้คืนไฟล์เป็นคู่");
        }
        return bytes;
    }
    // Call only after the live service is closed. Recovery is explicit; startup never substitutes a backup silently.
    public static async Task RestoreBackupAsync(string directory)
    {
        string path = Path.Combine(Path.GetFullPath(directory), "farm.json");
        using var recoveryLease = AcquireProfile(path);
        if (File.Exists(path + ".import-pending")) throw new InvalidDataException("ต้องกู้คืนการนำเข้าให้เสร็จก่อน");
        byte[] bytes = await Task.Run(() => ValidateBackup(path)).ConfigureAwait(false);
        string preserved = path + ".corrupt-" + Guid.NewGuid().ToString("N");
        if (File.Exists(path)) File.Move(path, preserved); // Retain exact damaged bytes for diagnosis/manual recovery.
        try { await ReplaceAsync(path, bytes).ConfigureAwait(false); }
        catch { if (File.Exists(preserved) && !File.Exists(path)) File.Move(preserved, path); throw; }
    }
    public async ValueTask DisposeAsync()
    {
        Task importingTask;
        lock (gate) { if (closing) return; closing = true; queue.Writer.TryComplete(); importingTask = importFinished?.Task ?? Task.CompletedTask; }
        try { await writer.ConfigureAwait(false); await importingTask.ConfigureAwait(false); }
        finally { storageGate.Dispose(); profileLease.Dispose(); }
    }
}
