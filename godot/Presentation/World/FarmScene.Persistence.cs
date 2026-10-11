using Godot;
using Environment = System.Environment;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Infrastructure.Logging;
using SeasonsTogether.Core.Infrastructure.Persistence;
namespace SeasonsTogether;

public partial class FarmScene
{
    Task pendingGraphics = Task.CompletedTask;
    Task pendingManualSave = Task.CompletedTask;
    public void SaveQuality(int value)
    {
        if(!ReadyToPlay || operationInProgress)return;
        SetQuality(value);
        pendingGraphics=PersistQuality(Quality,pendingGraphics);
    }
    async Task PersistQuality(int quality,Task previous)
    {
        await previous;
        try { await settings.SaveAsync(quality); }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException) {
            log.Write("error","settings.graphics.save.failed",e.GetType().Name);
            Notify("บันทึกการตั้งค่าภาพไม่ได้: "+e.Message);
        }
    }
    public void SaveNow()
    {
        if(ReadyToPlay && !operationInProgress)pendingManualSave=PersistManualSave();
    }
    async Task PersistManualSave()
    {
        try { await Session.SaveAsync(); Notify("บันทึกแล้ว"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notify("บันทึกไม่ได้: " + e.Message); }
    }
    public override void _ExitTree()
    {
        sceneActive = false;
        sceneLifetime.Cancel();
        if (Session == null) { shutdown = ReleaseStartupServices(saves, log); return; }
        Session.StateChanged -= OnStateChanged; Session.PersistenceFailed -= Notify;
        if (!operationInProgress && !finalCheckpoint) { try { _ = Session.SaveAsync(); } catch (Exception e) { log.Write("error", "save.exit", e.GetType().Name); } }
        shutdown = ReleaseServices(Task.WhenAll(pendingOperation, pendingDisplay, pendingGraphics, pendingManualSave,pendingTransition), Session, log);
    }
    static async Task ReleaseStartupServices(SaveService? storage, JsonGameLog? logger)
    {
        try { if (storage != null) await storage.DisposeAsync().ConfigureAwait(false); }
        finally { if (logger != null) await logger.DisposeAsync().ConfigureAwait(false); }
    }
    static async Task ReleaseServices(Task pending, GameSession session, JsonGameLog logger)
    {
        try { await pending.ConfigureAwait(false); }
        catch (Exception e) { logger.Write("error", "persistence.pending.failed", e.GetType().Name); }
        finally {
            try { await session.DisposeAsync().ConfigureAwait(false); }
            catch (Exception e) { logger.Write("error", "session.dispose", e.GetType().Name); }
            finally { await logger.DisposeAsync().ConfigureAwait(false); }
        }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && !operationInProgress) pendingOperation = CompleteOperation(false);
    }
    public void ImportLegacySave() { if (!operationInProgress) pendingOperation = CompleteOperation(true); }
    async Task CompleteOperation(bool import)
    {
        if (!ReadyToPlay) { GetTree().Quit(); return; }
        operationInProgress = true; Farmer.InputBlocked = true;
        try {
            await Task.WhenAll(pendingDisplay,pendingGraphics,pendingManualSave,pendingTransition); await Session.SaveAsync();
            if (import) await saves.ImportLegacyAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuanPleng"));
            finalCheckpoint = true;
            if (sceneActive) { if (import) GetTree().ReloadCurrentScene(); else GetTree().Quit(); }
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException) {
            if (sceneActive) { operationInProgress = false; Farmer.InputBlocked = Hud.ModalVisible; Notify("บันทึกหรือนำเข้าไม่ได้: " + e.Message); }
            log.Write("error", "save.operation", e.GetType().Name);
        }
    }
}
