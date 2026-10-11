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
    public int SelectedPlot { get; private set; } = -1;
    public int Quality { get; private set; } = 2;
    public string[] CropKeys { get; } = GameCatalog.Crops.Keys.ToArray();
    public bool IsBusy => operationInProgress;
    public bool ReadyToPlay => presentationReady && sceneActive;
    FarmWorldPresenter renderer = null!;
    WeatherLighting weather = null!;
    WeatherAccentPool accents = null!;
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
    bool sceneActive, presentationReady;
    readonly CancellationTokenSource sceneLifetime = new();
    public override void _Ready()
    {
        sceneActive = true;
        GetTree().AutoAcceptQuit = false;
        arguments = OS.GetCmdlineUserArgs();
        DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuanPleng", "godot-slice");
        if (arguments.Contains("--smoke") || arguments.Contains("--showcase") || arguments.Contains("--lifecycle") || arguments.Contains("--startup-checks") || arguments.Contains("--premium-checks")) DataDirectory = Path.Combine(ProjectSettings.GlobalizePath("res://reports"), "smoke-profile-" + Guid.NewGuid().ToString("N"));
        if (arguments.Contains("--startup-checks") && DiagnosticDataDirectory != null) DataDirectory = DiagnosticDataDirectory;
        if (arguments.Contains("--recovery-check")) DataDirectory = PrepareRecoveryDiagnostic();
        try { log = new JsonGameLog(DataDirectory); saves = new SaveService(DataDirectory, log); settings = new GraphicsSettings(DataDirectory); Session = new GameSession(saves, log); }
        catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            GD.PushError("Cannot load farm: " + e.Message);
            if (saves?.CanRestoreBackup == true)
            {
                var recovery = new ConfirmationDialog { DialogText = "เปิดเซฟไม่ได้ มีเซฟสำรองที่ตรวจสอบแล้ว\nกู้คืนเซฟสำรองหรือไม่? ข้อมูลล่าสุดอาจหายไปหนึ่งจุดบันทึก\nไฟล์ที่เสียจะเก็บไว้แยก ไม่ลบทิ้ง", OkButtonText = "กู้คืนสำรอง", CancelButtonText = "ออก" };
                AddChild(recovery); recovery.Confirmed += () => _ = RecoverStartupBackup();
                recovery.Canceled += () => GetTree().Quit(1); recovery.PopupCentered();
                if (arguments.Contains("--recovery-check")) _ = RunRecoveryDialogCheck(recovery);
                return;
            }
            var dialog = new AcceptDialog { DialogText = "เปิดเซฟไม่ได้: " + e.Message + "\nโปรดสำรองและตรวจไฟล์ farm.json ก่อนลองอีกครั้ง" };
            AddChild(dialog); dialog.Confirmed += () => GetTree().Quit(1); dialog.PopupCentered(); return;
        }
        try {
        Engine.MaxFps = 60;
        ValidateSceneComposition();
        if (arguments.Contains("--startup-checks") && DiagnosticAssetFailure) throw new InvalidDataException("Injected asset initialization failure");
        Art = AssetCatalog.Shared; renderer = new FarmWorldPresenter(); AddChild(renderer); renderer.Initialize(this, Session.Read, Art);
        Farmer = GetNode<FarmerController>("Actors/Farmer"); Farmer.Initialize(Session, Art.FarmerFrames());
        weather = new WeatherLighting { Read = Session.Read, Ambient = GetNode<CanvasModulate>("Ambient") }; AddChild(weather);
        weather.AddLamp(new Vector2(112, 82), Art.Glow); weather.AddLamp(new Vector2(656, 82), Art.Glow);
        accents = new WeatherAccentPool { Read=Session.Read,ZIndex=9 }; AddChild(accents);
        effects = new EffectPool { ZIndex = 8 }; AddChild(effects);
        Hud = GetNode<FarmHud>("HUD/Control"); Hud.Initialize(this);
        lastDay = Session.Read.World.Day; Session.StateChanged += OnStateChanged; Session.PersistenceFailed += Notify;
        SetQuality(settings.Load()); InitializeDisplay();
        presentationReady = true;
        }
        catch (Exception e) when (e is InvalidDataException or System.Text.Json.JsonException or KeyNotFoundException) {
            finalCheckpoint = true; // Failed composition must not save a partially initialized session.
            log.Write("error", "assets.startup.failed", e.GetType().Name);
            var dialog = new AcceptDialog { DialogText = "ไฟล์ภาพหรือฉากเกมไม่สมบูรณ์ โปรดติดตั้งเกมใหม่\nเซฟเดิมยังเก็บไว้" };
            AddChild(dialog); dialog.Confirmed += () => GetTree().Quit(1); dialog.PopupCentered(); return;
        }
        if (arguments.Contains("--recovery-check")) _ = RunRecoveredSceneCheck();
        else if (arguments.Contains("--startup-checks") && !startupHarnessRunning) { startupHarnessRunning = true; _ = RunStartupChecks(); }
        else if (arguments.Contains("--lifecycle") && !lifecycleHarnessRunning) { lifecycleHarnessRunning = true; _ = RunLifecycle(); }
        else if (arguments.Contains("--smoke")) _ = RunSmoke();
        else if (arguments.Contains("--premium-checks")) _ = RunPremiumChecks();
        if (arguments.Contains("--showcase")) PopulateShowcase();
    }
    void ValidateSceneComposition()
    {
        if (GetNodeOrNull<Node>("Ground") is not TileMapLayer || GetNodeOrNull<Node>("Details") is not TileMapLayer ||
            GetNodeOrNull<Node>("Actors") is not Node2D || GetNodeOrNull<Node>("Actors/Farmer") is not FarmerController ||
            GetNodeOrNull<Node>("Actors/Farmer/Sprite") is not AnimatedSprite2D || GetNodeOrNull<Node>("Actors/Farmer/Camera") is not Camera2D ||
            GetNodeOrNull<Node>("Ambient") is not CanvasModulate || GetNodeOrNull<Node>("HUD/Control") is not FarmHud ||
            GetNodeOrNull<Node>("Transitions/AnimationPlayer") is not AnimationPlayer)
            throw new InvalidDataException("Required farm scene node missing or has an incompatible type");
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
    public void InspectPlot(int index)
    {
        if (!ReadyToPlay || index < -1 || index >= Session.Read.PlotCount || SelectedPlot == index) return;
        SelectedPlot = index; renderer.SelectPlot(index); Hud.Refresh();
    }
    public void RefreshPlots() => renderer.RefreshPlots();
    void OnStateChanged(GameChange change)
    {
        if ((change & (GameChange.World | GameChange.Plots)) != 0) { RefreshSeason(); RefreshPlots(); }
        if (lastDay != Session.Read.World.Day) {
            if (lastDay >= 0 && Quality > 0 && !Display.ReducedMotion) GetNode<AnimationPlayer>("Transitions/AnimationPlayer").Play("dawn");
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
            effects.Burst(location, action,action=="harvest" && result.Effect.Crop!="" ? Art.CropStage(result.Effect.Crop,3) : null); if (!Display.ReducedMotion) Farmer.Gesture(action);
        }
    }
    public void SetQuality(int value) { Quality = Math.Clamp(value, 0, 2); weather.Quality = Quality; effects.Quality = Quality; accents.Quality=Quality; renderer.SetQuality(Quality); }
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
        if (Session == null) { shutdown = ReleaseStartupServices(saves, log); return; }
        Session.StateChanged -= OnStateChanged; Session.PersistenceFailed -= Notify;
        if (!operationInProgress && !finalCheckpoint) { try { _ = Session.SaveAsync(); } catch (Exception e) { log.Write("error", "save.exit", e.GetType().Name); } }
        shutdown = ReleaseServices(Task.WhenAll(pendingOperation, pendingDisplay), Session, log);
    }
    static async Task ReleaseStartupServices(SaveService? storage, JsonGameLog? logger)
    {
        try { if (storage != null) await storage.DisposeAsync().ConfigureAwait(false); }
        finally { if (logger != null) await logger.DisposeAsync().ConfigureAwait(false); }
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
            await pendingDisplay; await Session.SaveAsync();
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
