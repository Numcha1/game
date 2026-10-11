using Godot;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using SeasonsTogether.Core.Infrastructure.Logging;
using SeasonsTogether.Core.Infrastructure.Configuration;
using Environment = System.Environment;

namespace SeasonsTogether;

public partial class FarmScene : Node2D
{
    [Signal] public delegate void MessageEventHandler(string text);
    public GameSession Session { get; private set; } = null!;
    public AssetCatalog Art { get; private set; } = null!;
    public FarmerController Farmer { get; private set; } = null!;
    public FarmHud Hud { get; private set; } = null!;
    public string Tool { get; set; } = "plant";
    public string Seed { get; set; } = "carrot";
    public int Quality { get; private set; } = 2;
    public string[] CropKeys { get; } = GameCatalog.Crops.Keys.ToArray();
    public bool IsBusy => operationInProgress;
    public bool ReadyToPlay => Session != null && Farmer != null && Hud != null;
    FarmWorldPresenter renderer = null!;
    WeatherLighting weather = null!;
    EffectPool effects = null!;
    SaveService saves = null!;
    JsonGameLog log = null!;
    GraphicsSettings settings = null!;
    readonly FrameTimeMonitor frameMonitor = new();
    int lastDay = -1;
    bool operationInProgress, finalCheckpoint;
    Task pendingOperation = Task.CompletedTask;
    Task shutdown = Task.CompletedTask;
    public string DataDirectory { get; private set; } = "";
    string[] arguments = Array.Empty<string>();
    bool sceneActive;
    readonly CancellationTokenSource sceneLifetime = new();
    public override void _Ready()
    {
        sceneActive = true;
        GetTree().AutoAcceptQuit = false;
        arguments = OS.GetCmdlineUserArgs();
        DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuanPleng", "godot-slice");
        if (arguments.Contains("--smoke") || arguments.Contains("--showcase") || arguments.Contains("--lifecycle")) DataDirectory = Path.Combine(ProjectSettings.GlobalizePath("res://reports"), "smoke-profile-" + Guid.NewGuid().ToString("N"));
        try { log = new JsonGameLog(DataDirectory); saves = new SaveService(DataDirectory, log); settings = new GraphicsSettings(DataDirectory); Session = new GameSession(saves, log); }
        catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            GD.PushError("Cannot load farm: " + e.Message);
            if (saves?.CanRestoreBackup == true)
            {
                var recovery = new ConfirmationDialog { DialogText = "เปิดเซฟไม่ได้ มีเซฟสำรองที่ตรวจสอบแล้ว\nกู้คืนเซฟสำรองหรือไม่? ข้อมูลล่าสุดอาจหายไปหนึ่งจุดบันทึก\nไฟล์ที่เสียจะเก็บไว้แยก ไม่ลบทิ้ง", OkButtonText = "กู้คืนสำรอง", CancelButtonText = "ออก" };
                AddChild(recovery); recovery.Confirmed += () => _ = RecoverStartupBackup();
                recovery.Canceled += () => GetTree().Quit(1); recovery.PopupCentered(); return;
            }
            var dialog = new AcceptDialog { DialogText = "เปิดเซฟไม่ได้: " + e.Message + "\nโปรดสำรองและตรวจไฟล์ farm.json ก่อนลองอีกครั้ง" };
            AddChild(dialog); dialog.Confirmed += () => GetTree().Quit(1); dialog.PopupCentered(); return;
        }
        Engine.MaxFps = 60;
        Art = AssetCatalog.Shared; renderer = new FarmWorldPresenter(); AddChild(renderer); renderer.Initialize(this, Session.Read, Art);
        Farmer = GetNode<FarmerController>("Actors/Farmer"); Farmer.Initialize(Session, Art.FarmerFrames());
        weather = new WeatherLighting { Read = Session.Read, Ambient = GetNode<CanvasModulate>("Ambient") }; AddChild(weather);
        weather.AddLamp(new Vector2(112, 82), Art.Glow); weather.AddLamp(new Vector2(656, 82), Art.Glow);
        effects = new EffectPool { ZIndex = 8 }; AddChild(effects);
        Hud = GetNode<FarmHud>("HUD/Control"); Hud.Initialize(this);
        lastDay = Session.Read.World.Day; Session.StateChanged += OnStateChanged; Session.PersistenceFailed += Notify;
        SetQuality(settings.Load());
        if (arguments.Contains("--lifecycle") && !lifecycleHarnessRunning) { lifecycleHarnessRunning = true; _ = RunLifecycle(); }
        else if (arguments.Contains("--smoke")) _ = RunSmoke();
        if (arguments.Contains("--showcase")) PopulateShowcase();
    }
    async Task RecoverStartupBackup()
    {
        try {
            await saves.DisposeAsync(); await SaveService.RestoreBackupAsync(DataDirectory);
            if (sceneActive) GetTree().ReloadCurrentScene();
        }
        catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException) {
            log.Write("error", "save.recovery.failed", e.GetType().Name);
            if (sceneActive) {
                var dialog = new AcceptDialog { DialogText = "กู้คืนไม่ได้ ข้อมูลเดิมยังเก็บไว้: " + e.Message };
                AddChild(dialog); dialog.Confirmed += () => GetTree().Quit(1); dialog.PopupCentered();
            }
        }
    }
    void RefreshSeason() => renderer.RefreshSeason();
    public void RefreshPlots() => renderer.RefreshPlots();
    void OnStateChanged(GameChange change)
    {
        if ((change & (GameChange.World | GameChange.Plots)) != 0) { RefreshSeason(); RefreshPlots(); }
        if (lastDay != Session.Read.World.Day) {
            if (lastDay >= 0 && Quality > 0) GetNode<AnimationPlayer>("Transitions/AnimationPlayer").Play("dawn");
            lastDay = Session.Read.World.Day;
        }
    }
    public override void _Process(double delta)
    {
        if (!ReadyToPlay || Farmer == null) return;
        frameMonitor.Record();
        if (!operationInProgress) Session.Tick(delta);
    }
    public override void _UnhandledInput(InputEvent ev) => FarmInputRouter.Handle(this, ev);
    public void Perform(string action, int plot = -1)
    {
        if (operationInProgress) return;
        var result = Session.Execute(new GameCommand(action, plot, Seed)); Notify(result.Message);
        if (result.Effect != null) {
            var location = Farmer.Position;
            if (plot >= 0) { var t = GameCatalog.PlotTile(plot); location = new Vector2(t.X * 32, t.Y * 32); }
            effects.Burst(location, action); Farmer.Gesture();
        }
    }
    public void SetQuality(int value) { Quality = Math.Clamp(value, 0, 2); weather.Quality = Quality; effects.Quality = Quality; renderer.SetQuality(Quality); }
    public void Notify(string message) { if (sceneActive) EmitSignal(SignalName.Message, message); }
    public async void SaveQuality(int value)
    {
        SetQuality(value);
        try { await settings.SaveAsync(Quality); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notify("บันทึกการตั้งค่าภาพไม่ได้: " + e.Message); }
    }
    public async void SaveNow()
    {
        try { await Session.SaveAsync(); Notify("บันทึกแล้ว"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notify("บันทึกไม่ได้: " + e.Message); }
    }
    public override void _ExitTree()
    {
        sceneActive = false;
        sceneLifetime.Cancel();
        if (Session == null) { if (saves != null) _ = saves.DisposeAsync(); if (log != null) _ = log.DisposeAsync(); return; }
        Session.StateChanged -= OnStateChanged; Session.PersistenceFailed -= Notify;
        if (!operationInProgress && !finalCheckpoint) { try { _ = Session.SaveAsync(); } catch (Exception e) { log.Write("error", "save.exit", e.GetType().Name); } }
        shutdown = ReleaseServices(pendingOperation, Session, log);
    }
    static async Task ReleaseServices(Task pending, GameSession session, JsonGameLog logger)
    {
        try { await pending.ConfigureAwait(false); await session.DisposeAsync().ConfigureAwait(false); }
        catch (Exception e) { logger.Write("error", "session.dispose", e.GetType().Name); }
        finally { await logger.DisposeAsync().ConfigureAwait(false); }
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
            await Session.SaveAsync();
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
