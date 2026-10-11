using Godot;
using SuanPleng;
namespace SeasonsTogether;
public partial class FarmScene
{
    async Task NextFrame(StringName signal)
    {
        if (!sceneActive) throw new OperationCanceledException();
        await ToSignal(GetTree(), signal);
        if (!sceneActive) throw new OperationCanceledException();
    }
    async Task NextDraw()
    {
        if (!sceneActive) throw new OperationCanceledException();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (!sceneActive) throw new OperationCanceledException();
    }
    async Task RunSmoke()
    {
        await NextFrame(SceneTree.SignalName.ProcessFrame);
        var checks = new List<string>();
        void Check(bool pass, string label) { if (!pass) throw new InvalidOperationException(label); checks.Add(label); }
        try
        {
            Check(ground.GetUsedCells().Count == 494, "494 ground tiles loaded");
            Check(crops.Length == 24 && trees.Count == 10, "24 plots and ten seasonal trees");
            double startX = Session.Player.X;
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = true });
            for (int i = 0; i < 20; i++) await NextFrame(SceneTree.SignalName.PhysicsFrame);
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = false });
            Check(Session.Player.X > startX, "WASD through Godot physics input");
            Check(Farmer.Camera.GlobalPosition.X >= startX * 32, "camera follows player");
            Session.Move(8, 6); Tool = "plant";
            _UnhandledInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = GetViewport().GetCanvasTransform() * new Vector2(256, 192) });
            Check(Session.State.Plots[0].Crop == "carrot", "mouse picking uses camera inverse and original plot zero");
            Perform("water", 0); Check(Session.State.Plots[0].Moisture == 95, "water original +45 rule");
            Session.State.Plots[0].Growth = 3; Session.State.Plots[0].RipeDay = Session.State.Day;
            Perform("harvest", 0); Check(Session.Player.Bag.GetValueOrDefault("carrot") == 3, "harvest original quality yield");
            Session.Move(20, 8); int before = Session.State.Coins; Perform("sell"); Check(Session.State.Coins > before, "sell at original shipping location");
            Hud.OpenBag(); Check(Farmer.InputBlocked && Hud.ModalVisible, "modal blocks input"); Hud.CloseModal();
            for (int s = 0; s < 4; s++) { Session.State.Day = 1 + s * 14; RefreshSeason(); RefreshPlots(); Check(season == s, "season " + s); }
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
            GD.Print("SLICE PASS " + checks.Count); GetTree().Quit();
        }
        catch (OperationCanceledException) { /* Scene exited while diagnostics awaited a frame. */ }
        catch (Exception e) { if (!sceneActive) return; GD.PushError("SLICE FAIL " + e); GetTree().Quit(1); }
    }
}
