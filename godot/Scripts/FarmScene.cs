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
    public bool ReadyToPlay => Session != null;
    FarmWorldPresenter renderer = null!;
    WeatherLighting weather = null!;
    EffectPool effects = null!;
    SaveService saves = null!;
    JsonGameLog log = null!;
    GraphicsSettings settings = null!;
    readonly List<double> frameTimes = new(1800);
    int lastDay = -1;
    long lastTimestamp;
    bool operationInProgress, finalCheckpoint;
    Task pendingOperation = Task.CompletedTask;
    public string DataDirectory { get; private set; } = "";
    string[] arguments = Array.Empty<string>();
    bool sceneActive;
    public override void _Ready()
    {
        sceneActive = true;
        GetTree().AutoAcceptQuit = false;
        arguments = OS.GetCmdlineUserArgs();
        DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuanPleng", "godot-slice");
        if (arguments.Contains("--smoke") || arguments.Contains("--showcase")) DataDirectory = Path.Combine(ProjectSettings.GlobalizePath("res://reports"), "smoke-profile-" + Guid.NewGuid().ToString("N"));
        try { log = new JsonGameLog(DataDirectory); saves = new SaveService(DataDirectory, log); settings = new GraphicsSettings(DataDirectory); Session = new GameSession(saves, log); }
        catch (Exception e) when (e is IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            GD.PushError("Cannot load farm: " + e.Message);
            var dialog = new AcceptDialog { DialogText = "เปิดเซฟไม่ได้: " + e.Message + "\nโปรดสำรองและตรวจไฟล์ farm.json ก่อนลองอีกครั้ง" };
            AddChild(dialog); dialog.Confirmed += () => GetTree().Quit(1); dialog.PopupCentered(); return;
        }
        Engine.MaxFps = 60;
        Art = new AssetCatalog(); renderer = new FarmWorldPresenter(); AddChild(renderer); renderer.Initialize(this, Session.Read, Art);
        Farmer = GetNode<FarmerController>("Actors/Farmer"); Farmer.Initialize(Session, Art.FarmerFrames());
        weather = new WeatherLighting { Read = Session.Read, Ambient = GetNode<CanvasModulate>("Ambient") }; AddChild(weather);
        weather.AddLamp(new Vector2(112, 82), Art.Glow); weather.AddLamp(new Vector2(656, 82), Art.Glow);
        effects = new EffectPool { ZIndex = 8 }; AddChild(effects);
        Hud = GetNode<FarmHud>("HUD/Control"); Hud.Initialize(this);
        lastDay = Session.Read.World.Day; Session.StateChanged += OnStateChanged; Session.PersistenceFailed += Notify;
        SetQuality(settings.Load());
        if (arguments.Contains("--smoke")) _ = RunSmoke();
        if (arguments.Contains("--showcase")) PopulateShowcase();
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
        long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        if (lastTimestamp != 0 && frameTimes.Count < 3600) frameTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(lastTimestamp, timestamp).TotalMilliseconds);
        lastTimestamp = timestamp;
        if (!operationInProgress) Session.Tick(delta);
    }
    public override void _UnhandledInput(InputEvent ev)
    {
        if (!ReadyToPlay || operationInProgress || Hud.ModalVisible) return;
        if (ev is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            Vector2 mouse = GetViewport().GetCanvasTransform().AffineInverse() * click.Position;
            int x = (int)Math.Round(mouse.X / 32), y = (int)Math.Round(mouse.Y / 32);
            if (x >= 8 && x <= 13 && y >= 6 && y <= 9) Perform(Tool, (y - 6) * 6 + x - 8);
        }
        if (ev is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.PhysicalKeycode >= Key.Key1 && key.PhysicalKeycode <= Key.Key7) { Tool = FarmHud.Tools[(int)(key.PhysicalKeycode - Key.Key1)].Action; Hud.Refresh(); }
            switch (key.PhysicalKeycode)
            {
                case Key.Tab: Seed = CropKeys[(Array.IndexOf(CropKeys, Seed) + 1) % 8]; Hud.Refresh(); break;
                case Key.E: Perform("sell"); break;
                case Key.Space: Perform(Session.Read.Player.Sleeping ? "wake" : "sleep"); break;
                case Key.B: case Key.I: Hud.OpenBag(); break;
                case Key.O: Hud.OpenShop(); break;
                case Key.F3: Hud.OpenSettings(); break;
                case Key.Escape: Perform("pause"); break;
                case Key.F1: Hud.OpenHelp(); break;
            }
        }
    }
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
        if (Session == null) { if (saves != null) _ = saves.DisposeAsync(); if (log != null) _ = log.DisposeAsync(); return; }
        Session.StateChanged -= OnStateChanged; Session.PersistenceFailed -= Notify;
        if (!operationInProgress && !finalCheckpoint) { try { _ = Session.SaveAsync(); } catch (Exception e) { log.Write("error", "save.exit", e.GetType().Name); } }
        _ = ReleaseServices(pendingOperation, Session, log);
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
        if (Session == null) { GetTree().Quit(); return; }
        operationInProgress = true; Farmer.InputBlocked = true;
        try {
            await Session.SaveAsync();
            if (import) await saves.ImportLegacyAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuanPleng"));
            finalCheckpoint = true;
            if (sceneActive) { if (import) GetTree().ReloadCurrentScene(); else GetTree().Quit(); }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) {
            if (sceneActive) { operationInProgress = false; Farmer.InputBlocked = Hud.ModalVisible; Notify("บันทึกหรือนำเข้าไม่ได้: " + e.Message); }
            log.Write("error", "save.operation", e.GetType().Name);
        }
    }
}
