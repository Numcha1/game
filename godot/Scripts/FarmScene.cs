using Godot;
using SeasonsTogether.Core;
using SuanPleng;
using Environment = System.Environment;

namespace SeasonsTogether;

public partial class FarmScene : Node2D
{
    [Signal] public delegate void FarmChangedEventHandler();
    [Signal] public delegate void MessageEventHandler(string text);
    public LocalFarmSession Session { get; private set; } = null!;
    public AssetCatalog Art { get; private set; } = null!;
    public FarmerController Farmer { get; private set; } = null!;
    public FarmHud Hud { get; private set; } = null!;
    public string Tool { get; set; } = "plant";
    public string Seed { get; set; } = "carrot";
    public int Quality { get; private set; } = 2;
    public string[] CropKeys { get; } = Farm.Crops.Keys.ToArray();
    public bool ReadyToPlay => Session != null;
    TileMapLayer ground = null!, details = null!;
    Node2D actors = null!;
    WeatherLighting weather = null!;
    EffectPool effects = null!;
    readonly Sprite2D[] crops = new Sprite2D[24];
    readonly Sprite2D[] stakes = new Sprite2D[24];
    readonly List<Sprite2D> trees = new();
    readonly List<Sprite2D> treeShadows = new();
    readonly Vector2[] treeLocations = { new(38,170), new(100,335), new(42,477), new(740,180), new(737,354), new(615,490), new(370,50), new(485,45), new(208,460), new(431,461) };
    readonly List<double> frameTimes = new(1800);
    int season = -1, lastDay = -1;
    double uiElapsed, saveErrorCooldown;
    long lastTimestamp;
    ShaderMaterial wind = null!;
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
        try { Session = new LocalFarmSession(DataDirectory); }
        catch (Exception e) when (e is IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            GD.PushError("Cannot load farm: " + e.Message);
            var dialog = new AcceptDialog { DialogText = "เปิดเซฟไม่ได้: " + e.Message + "\nโปรดสำรองและตรวจไฟล์ farm.json ก่อนลองอีกครั้ง" };
            AddChild(dialog); dialog.Confirmed += () => GetTree().Quit(1); dialog.PopupCentered(); return;
        }
        Engine.MaxFps = 60;
        Art = new AssetCatalog(); ground = GetNode<TileMapLayer>("Ground"); details = GetNode<TileMapLayer>("Details"); actors = GetNode<Node2D>("Actors");
        wind = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/Wind.gdshader") };
        ground.TileSet = GD.Load<TileSet>("res://Assets/FarmTiles.tres"); details.TileSet = ground.TileSet;
        BuildEnvironment();
        Farmer = GetNode<FarmerController>("Actors/Farmer"); Farmer.Initialize(Session, Art.FarmerFrames());
        weather = new WeatherLighting { State = Session.State, Ambient = GetNode<CanvasModulate>("Ambient") }; AddChild(weather);
        weather.AddLamp(new Vector2(112, 82), GD.Load<Texture2D>("res://Assets/glow.png"));
        weather.AddLamp(new Vector2(656, 82), GD.Load<Texture2D>("res://Assets/glow.png"));
        effects = new EffectPool { ZIndex = 8 }; AddChild(effects);
        Hud = GetNode<FarmHud>("HUD/Control"); Hud.Initialize(this);
        RefreshSeason(); RefreshPlots();
        string preferences = Path.Combine(DataDirectory, "graphics.json");
        try { if (System.IO.File.Exists(preferences)) SetQuality(System.Text.Json.JsonSerializer.Deserialize<int>(System.IO.File.ReadAllText(preferences))); }
        catch (Exception e) when (e is IOException or System.Text.Json.JsonException) { GD.Print("Graphics preferences reset: " + e.Message); }
        if (arguments.Contains("--smoke")) _ = RunSmoke();
        if (arguments.Contains("--showcase"))
        {
            Session.State.Greenhouse = true;
            for (int i = 0; i < 24; i++) { var p = Session.State.Plots[i]; p.Crop = CropKeys[i % 8]; p.Growth = Farm.Crops[p.Crop].Days; p.RipeDay = Session.State.Day; }
            RefreshPlots();
        }
    }
    Sprite2D Prop(string asset, Vector2 position, float width, int z = 0)
    {
        var texture = Art.Region(asset);
        var sprite = new Sprite2D { Texture = texture, Position = position, Offset = new Vector2(0, -texture.GetHeight() / 2f), Scale = Vector2.One * (width / texture.GetWidth()), ZIndex = z };
        actors.AddChild(sprite); return sprite;
    }
    void BuildEnvironment()
    {
        Prop("house", new Vector2(112, 128), 120);
        Prop("shop", new Vector2(656, 128), 120);
        Prop("crate", new Vector2(640, 256), 46);
        Prop("well", new Vector2(192, 144), 38);
        Prop("barrel", new Vector2(50, 116), 24);
        Prop("sign", new Vector2(607, 126), 30);
        var pond = new Sprite2D { Texture = GD.Load<Texture2D>("res://Assets/pond.png"), Position = new Vector2(624, 448), ZIndex = -1 };
        pond.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/Water.gdshader") }; AddChild(pond);
        for (int i = 0; i < treeLocations.Length; i++)
        {
            var shadow = new Sprite2D { Texture = GD.Load<Texture2D>("res://Assets/shadow.png"), Position = treeLocations[i] + new Vector2(8, -2), Scale = new Vector2(1.6f, .8f), ZIndex = -2 }; AddChild(shadow); treeShadows.Add(shadow);
            var tree = Prop("spring", treeLocations[i], i % 3 == 0 ? 95 : 78); tree.Material = wind; trees.Add(tree);
        }
        var rng = new Random(621);
        for (int i = 0; i < 32; i++)
        {
            var p = new Vector2(rng.Next(30, 760), rng.Next(118, 533));
            if (p.X > 210 && p.X < 475 && p.Y > 160 && p.Y < 347 || p.X > 535 && p.Y > 365) continue;
            Prop(i % 5 == 0 ? "rocks" : "flowers", p, i % 5 == 0 ? 22 : 15);
        }
        for (int i = 0; i < 24; i++)
        {
            var t = Farm.Tile(i); var pos = new Vector2(t.X * 32, t.Y * 32);
            crops[i] = new Sprite2D { Position = pos, Visible = false, Material = wind }; actors.AddChild(crops[i]);
            stakes[i] = new Sprite2D { Texture = GD.Load<Texture2D>("res://Assets/stake.png"), Position = pos + new Vector2(9, -10), Visible = false }; actors.AddChild(stakes[i]);
        }
    }
    void RefreshSeason()
    {
        if (season == Session.State.Season) return; season = Session.State.Season;
        string[] variants = { "spring", "summer", "autumn", "winter" };
        for (int i = 0; i < trees.Count; i++)
        {
            var texture = Art.Region(variants[season]); float width = i % 3 == 0 ? 95 : 78;
            trees[i].Texture = texture; trees[i].Offset = new Vector2(0, -texture.GetHeight() / 2f); trees[i].Scale = Vector2.One * (width / texture.GetWidth());
        }
        for (int y = 0; y < 19; y++) for (int x = 0; x < 26; x++)
        {
            bool path = y == 4 || y == 10 || x == 6 || x == 18;
            int variation = (x * 31 + y * 17) % 4;
            ground.SetCell(new Vector2I(x, y), 0, new Vector2I(path ? 4 + variation : variation, season));
            if (y == 0 || y == 17) details.SetCell(new Vector2I(x, y), 0, new Vector2I(12, season));
        }
        ground.UpdateInternals();
    }
    public void RefreshPlots()
    {
        for (int i = 0; i < 24; i++)
        {
            var plot = Session.State.Plots[i]; var tile = Farm.Tile(i);
            ground.SetCell(new Vector2I(tile.X, tile.Y), 0, new Vector2I(plot.Moisture >= 80 ? 10 : plot.Moisture >= 35 ? 9 : 8, season));
            crops[i].Visible = plot.Crop != ""; stakes[i].Visible = plot.Stake;
            if (!crops[i].Visible) continue;
            var texture = Art.Region(plot.Crop);
            float progress = (float)Math.Clamp(plot.Growth / Farm.Crops[plot.Crop].Days, 0, 1);
            if (progress < .33f) texture = Art.Region("sprout");
            else if (progress < .66f) texture = Art.Region("young-" + plot.Crop);
            crops[i].Texture = texture;
            float width = progress < .33f ? 12 : progress < .66f ? 20 : plot.Crop == "corn" ? 31 : 30;
            crops[i].Scale = Vector2.One * (width / texture.GetWidth());
            crops[i].Offset = new Vector2(0, -texture.GetHeight() / 2f + 5);
            crops[i].Modulate = plot.Disease ? new Color(.75f, .65f, .6f) : Colors.White;
        }
        EmitSignal(SignalName.FarmChanged);
    }
    public override void _Process(double delta)
    {
        if (!ReadyToPlay || Farmer == null) return;
        long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        if (lastTimestamp != 0 && frameTimes.Count < 3600) frameTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(lastTimestamp, timestamp).TotalMilliseconds);
        lastTimestamp = timestamp;
        saveErrorCooldown -= delta;
        try { Session.Tick(delta); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { if (saveErrorCooldown <= 0) { Notify("บันทึกไม่ได้: " + e.Message); saveErrorCooldown = 10; } }
        RefreshSeason();
        if (lastDay != Session.State.Day) {
            if (lastDay >= 0 && Quality > 0) GetNode<AnimationPlayer>("Transitions/AnimationPlayer").Play("dawn");
            lastDay = Session.State.Day; RefreshPlots();
        }
        uiElapsed += delta;
        if (uiElapsed >= .25) { uiElapsed = 0; Hud.Refresh(); }
    }
    public override void _UnhandledInput(InputEvent ev)
    {
        if (!ReadyToPlay || Hud.ModalVisible) return;
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
                case Key.Space: Perform(Session.Player.Sleeping ? "wake" : "sleep"); break;
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
        try
        {
            long sequence = Session.State.EffectSequence;
            Notify(Session.Execute(action, plot, Seed));
            if (Session.State.EffectSequence > sequence)
            {
                var location = Farmer.Position;
                if (plot >= 0) { var t = Farm.Tile(plot); location = new Vector2(t.X * 32, t.Y * 32); }
                effects.Burst(location, action); Farmer.Gesture();
            }
            RefreshPlots(); Hud.Refresh();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notify("บันทึกไม่ได้: " + e.Message); }
    }
    public void SetQuality(int value) { Quality = Math.Clamp(value, 0, 2); weather.Quality = Quality; effects.Quality = Quality; wind.SetShaderParameter("strength", Quality == 0 ? 0 : .8f); foreach (var shadow in treeShadows) shadow.Visible = Quality > 0; }
    public void Notify(string message) => EmitSignal(SignalName.Message, message);
    public void SaveQuality(int value)
    {
        SetQuality(value);
        try { System.IO.File.WriteAllText(Path.Combine(DataDirectory, "graphics.json"), System.Text.Json.JsonSerializer.Serialize(Quality)); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notify("บันทึกการตั้งค่าภาพไม่ได้: " + e.Message); }
    }
    public override void _ExitTree() => sceneActive = false;
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            try { Session?.Save(); GetTree().Quit(); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notify("ปิดเกมไม่ได้ เพราะเซฟไม่สำเร็จ: " + e.Message); }
        }
    }
    public void ImportLegacySave()
    {
        string source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuanPleng", "farm.json");
        try
        {
            // Validate before touching any slice data. Source is never written.
            _ = Farm.Load(source);
            if (!File.Exists(source)) throw new IOException("ไม่พบเซฟเกมเดิม");
            Session.Save(); System.IO.File.Copy(Session.SavePath, Session.SavePath + ".before-import-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), true);
            System.IO.File.Copy(source, Session.SavePath, true);
            string identity = Path.Combine(Path.GetDirectoryName(source)!, "player.txt");
            if (File.Exists(identity)) System.IO.File.Copy(identity, Path.Combine(DataDirectory, "player.txt"), true);
            GetTree().ReloadCurrentScene();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { Notify("นำเข้าไม่ได้: " + e.Message); }
    }
}
