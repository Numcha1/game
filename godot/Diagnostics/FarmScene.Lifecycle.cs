using Godot;
namespace SeasonsTogether;
public partial class FarmScene
{
    static bool lifecycleHarnessRunning;
    async Task RunLifecycle()
    {
        var checks = new List<string>(); var samples = new List<object>();
        int rounds = arguments.Contains("--lifecycle-extended") ? 50 : 10;
        void Check(bool value,string label) { if (!value) throw new InvalidOperationException(label); checks.Add(label); }
        try
        {
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            var template = GD.Load<PackedScene>("res://Scenes/Farm.tscn");
            long baselineNodes = (long)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
            for (int round = 0; round < rounds; round++)
            {
                var scene = template.Instantiate<FarmScene>(); GetTree().Root.AddChild(scene);
                var session = scene.Session; var catalog = scene.Art;
                Check(session.ObserverCount == 2,"two scene/HUD observers in lifecycle " + round);
                Check(ReferenceEquals(catalog,Art) && ReferenceEquals(catalog.FarmerFrames(),Art.FarmerFrames()) && ReferenceEquals(scene.Hud.Theme.DefaultFont,Hud.Theme.DefaultFont),"shared assets, animation frames and Thai font cache " + round);
                for (int cycle = 0; cycle < 10; cycle++)
                {
                    scene.Hud.OpenBag(); scene.Hud.CloseModal(); scene.Hud.OpenShop(); scene.Hud.CloseModal();
                    await NextFrame(SceneTree.SignalName.ProcessFrame);
                }
                Check(session.ObserverCount == 2,"menu cycles do not duplicate observers " + round);
                await session.SaveAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Task delayed = scene.WaitForDiagnosticExit();
                GetTree().Root.RemoveChild(scene);
                Task finished = scene.shutdown;
                scene.QueueFree(); await NextFrame(SceneTree.SignalName.ProcessFrame);
                await finished.WaitAsync(TimeSpan.FromSeconds(5));
                await delayed.WaitAsync(TimeSpan.FromSeconds(5));
                Check(session.ObserverCount == 0,"freed scene detaches all application observers " + round);
                Check(!GodotObject.IsInstanceValid(scene),"scene node freed " + round);
                await NextFrame(SceneTree.SignalName.ProcessFrame);
                long nodes = (long)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
                Check(nodes == baselineNodes,"node count returns to baseline " + round);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                samples.Add(new { round,nodes,managedBytes=GC.GetTotalMemory(true),privateBytes=System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64,
                    resources=Performance.GetMonitor(Performance.Monitor.ObjectResourceCount),orphans=Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount) });
            }
            // No performance claims from a lifecycle test that deliberately forces GC.
            await Session.SaveAsync(); finalCheckpoint = true;
            string path = Path.Combine(ProjectSettings.GlobalizePath("res://reports"),(rounds > 10 ? "lifecycle-extended-" : "lifecycle-") + DisplayServer.GetName() + ".json");
            await System.IO.File.WriteAllTextAsync(path,System.Text.Json.JsonSerializer.Serialize(new { passed=checks.Count,checks,samples,
                scope=$"{rounds} full scene teardowns, {rounds * 10} bag/shop cycles. Finite soak; no lifetime leak certification." },new System.Text.Json.JsonSerializerOptions { WriteIndented=true }));
            if (sceneActive) { GD.Print("LIFECYCLE PASS " + checks.Count); GetTree().Quit(); }
        }
        catch (Exception e) { if (sceneActive) { GD.PushError("LIFECYCLE FAIL " + e); GetTree().Quit(1); } }
    }
    async Task WaitForDiagnosticExit()
    {
        try { await NextFrame(SceneTree.SignalName.ProcessFrame); await NextFrame(SceneTree.SignalName.ProcessFrame); }
        catch (OperationCanceledException) { }
    }
}
