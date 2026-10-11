using Godot;
using SuanPleng;
namespace SeasonsTogether;
public partial class FarmScene
{
    List<double> frameTimes => frameMonitor.Samples;
    void PopulateShowcase()
    {
        Session.State.Greenhouse = true;
        for (int i = 0; i < 24; i++) { var p = Session.State.Plots[i]; p.Crop = CropKeys[i % 8]; p.Growth = Farm.Crops[p.Crop].Days; p.RipeDay = Session.State.Day; }
        RefreshPlots();
    }
    Task NextFrame(StringName signal) => AwaitSignal(GetTree(),signal);
    Task NextDraw() => AwaitSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
    async Task AwaitSignal(GodotObject emitter, StringName signal)
    {
        if (!sceneActive) throw new OperationCanceledException();
        // Signals and scene cancellation both arrive on the Godot thread, matching ToSignal timing.
        var completion = new TaskCompletionSource();
        var callback = Callable.From(() => completion.TrySetResult());
        emitter.Connect(signal,callback,(uint)GodotObject.ConnectFlags.OneShot);
        using var registration = sceneLifetime.Token.Register(() => completion.TrySetCanceled(sceneLifetime.Token));
        try { await completion.Task; }
        finally { if (GodotObject.IsInstanceValid(emitter) && emitter.IsConnected(signal,callback)) emitter.Disconnect(signal,callback); }
        if (!sceneActive) throw new OperationCanceledException();
    }
    async Task RunSmoke()
    {
        await NextFrame(SceneTree.SignalName.ProcessFrame);
        var checks = new List<string>();
        void Check(bool pass, string label) { if (!pass) throw new InvalidOperationException(label); checks.Add(label); }
        try
        {
            Check(renderer.GroundCellCount == 494, "494 ground tiles loaded");
            Check(Session.Read.PlotCount == 24 && renderer.TreeCount == 10, "24 plots and ten seasonal trees");
            double startX = Session.Player.X;
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = true });
            for (int i = 0; i < 20; i++) await NextFrame(SceneTree.SignalName.PhysicsFrame);
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = false });
            Check(Session.Player.X > startX, "WASD through Godot physics input");
            Check(Farmer.Camera.GlobalPosition.X >= startX * 32, "camera follows player");
            Session.Move(8, 6); Tool = "plant";
            _UnhandledInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = GetViewport().GetCanvasTransform() * new Vector2(256, 192) });
            Check(Session.State.Plots[0].Crop == "carrot", "mouse picking uses camera inverse and original plot zero");
            Check(SelectedPlot == 0 && Hud.PlotInformation.Contains("แครอต") && renderer.SelectedPlot == 0, "plot inspection shows selected crop and aligned highlight");
            _UnhandledInput(new InputEventMouseMotion { Position = GetViewport().GetCanvasTransform() * new Vector2(416, 288) });
            Check(SelectedPlot == 23 && Hud.PlotInformation.Contains("แปลง 24"), "hover inspects last plot using camera inverse");
            Tool = "clear";
            _UnhandledInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = GetViewport().GetCanvasTransform() * new Vector2(256, 192) });
            Check(Hud.ModalVisible && Farmer.InputBlocked && Session.State.Plots[0].Crop == "carrot", "clear opens confirmation without spending or removing crop");
            Hud.CloseModal(); Check(Session.State.Plots[0].Crop == "carrot", "cancel clear preserves crop");
            Hud.RequestPlotAction("clear", 0);
            Hud.FindChildren("*", "Button", true, false).OfType<Button>().First(b => b.Text == "ยืนยันถอนต้น").EmitSignal(Button.SignalName.Pressed);
            Check(!Hud.ModalVisible && Session.State.Plots[0].Crop == "", "confirm clear delegates original rule once");
            Tool = "plant"; Perform("plant", 0);
            Perform("water", 0); Check(Session.State.Plots[0].Moisture == 95, "water original +45 rule");
            Session.State.Plots[0].Growth = 3; Session.State.Plots[0].RipeDay = Session.State.Day;
            Perform("harvest", 0); Check(Session.Player.Bag.GetValueOrDefault("carrot") == 3, "harvest original quality yield");
            Session.Move(20, 8); int before = Session.State.Coins; Perform("sell"); Check(Session.State.Coins > before, "sell at original shipping location");
            Session.Player.Bag["carrot"] = 4; Session.Move(4, 4); int sleepDay = Session.Read.World.Day;
            _UnhandledInput(new InputEventKey { PhysicalKeycode = Key.Space, Pressed = true });
            Check(Hud.ModalVisible && Session.Read.World.Day == sleepDay, "sleep warns about inventory spoilage before advancing day");
            Hud.CloseModal(); Check(Session.Player.Bag["carrot"] == 4, "cancel sleep preserves inventory");
            Hud.RequestSleep();
            Hud.FindChildren("*", "Button", true, false).OfType<Button>().First(b => b.Text == "ยืนยันเข้านอน").EmitSignal(Button.SignalName.Pressed);
            Check(Session.Read.World.Day == sleepDay + 1 && Session.Player.Bag["carrot"] == 3 && Session.Read.Player.Energy == 100, "confirmed sleep preserves original day, spoilage and energy rules");
            Session.Player.Bag.Clear(); Hud.OpenJournal(); Check(Hud.ModalVisible && Farmer.InputBlocked, "farm journal routes through modal input blocking"); Hud.CloseModal();
            Hud.OpenBag(); Check(Farmer.InputBlocked && Hud.ModalVisible, "modal blocks input"); Hud.CloseModal();
            for (int s = 0; s < 4; s++) { Session.State.Day = 1 + s * 14; RefreshSeason(); RefreshPlots(); Check(renderer.CurrentSeason == s, "season " + s); }
            Session.State.Day = 1; Session.State.Minute = 700; RefreshSeason();
            for (int i = 0; i < 24; i++) { var p = Session.State.Plots[i]; p.Crop = CropKeys[i % 8]; p.Growth = Farm.Crops[p.Crop].Days; p.RipeDay = 1; }
            RefreshPlots();
            var reportDir = ProjectSettings.GlobalizePath("res://reports"); Directory.CreateDirectory(reportDir);
            var qualityReports = new List<object>();
            for (int q = 0; q < 3; q++)
            {
                SetQuality(q); Session.State.Weather = "พายุ"; Session.State.Minute = 1210;
                for (int n = 0; n < 60; n++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                frameTimes.Clear(); long managedBefore = GC.GetTotalMemory(false); long privateBefore = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;
                for (int n = 0; n < 600; n++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                var sample = frameTimes.Order().ToArray();
                qualityReports.Add(new { quality = q, frames = sample.Length, meanMs = sample.Average(), p95Ms = sample[(int)(sample.Length * .95)], p99Ms = sample[(int)(sample.Length * .99)], fps = 1000 / sample.Average(), privateBefore, privateAfter = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64, managedBefore, managedAfter = GC.GetTotalMemory(false) });
                Check(weather.Quality == q && effects.Quality == q, "quality " + q);
            }
            if (DisplayServer.GetName() != "headless")
            {
                Session.State.Weather = "แดดอ่อน"; Session.State.Minute = 720; Session.Move(10.5, 4);
                for (int i = 0; i < 90; i++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                await NextDraw();
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(reportDir, "farm-day.png"));
                Session.State.Minute = 1220; Session.State.Weather = "พายุ";
                for (int i = 0; i < 90; i++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                await NextDraw();
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(reportDir, "farm-storm.png"));
                Hud.OpenShop();
                await NextDraw();
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(reportDir, "shop.png")); Hud.CloseModal();
                foreach (var size in new[] { new Vector2I(960, 640), new Vector2I(1280, 800), new Vector2I(1920, 1080) })
                {
                    DisplayServer.WindowSetSize(size);
                    for (int i = 0; i < 10; i++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                    Check(GetViewport().GetVisibleRect().Size == new Vector2(800, 450), "integer viewport at " + size);
                    for (int i = 0; i < 24; i++)
                    {
                        var t = Farm.Tile(i); var world = new Vector2(t.X * 32, t.Y * 32);
                        var canvas = GetViewport().GetCanvasTransform();
                        Check((canvas.AffineInverse() * (canvas * world)).DistanceTo(world) < .001, "camera inverse at size " + size + " plot " + i);
                    }
                }
                DisplayServer.WindowSetSize(new Vector2I(1600, 900));
            }
            var frames = frameTimes.Skip(30).Order().ToArray();
            var report = new { checks, passed = checks.Count, renderer = DisplayServer.GetName(), frames = frames.Length,
                meanFrameMs = frames.Average(), p95FrameMs = frames[(int)(frames.Length * .95)], fps = 1000 / frames.Average(),
                privateBytes = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64, managedBytes = GC.GetTotalMemory(false), qualityReports,
                limitation = "Finite engine-loop sample. Headless does not measure graphics. Rendered runs include presentation but are not long-term GPU or leak certification." };
            System.IO.File.WriteAllText(Path.Combine(reportDir, "godot-smoke-" + DisplayServer.GetName() + ".json"), System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            await Session.SaveAsync(); finalCheckpoint = true;
            if (sceneActive) { GD.Print("SLICE PASS " + checks.Count); GetTree().Quit(); }
        }
        catch (OperationCanceledException) { /* Scene exited while diagnostics awaited a frame. */ }
        catch (Exception e) { if (!sceneActive) return; GD.PushError("SLICE FAIL " + e); GetTree().Quit(1); }
    }
}
