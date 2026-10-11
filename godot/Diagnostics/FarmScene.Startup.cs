using Godot;
using SuanPleng;
using System.Text.Json;

namespace SeasonsTogether;
public partial class FarmScene
{
    internal string? DiagnosticDataDirectory { get; set; }
    internal bool DiagnosticAssetFailure { get; set; }
    static bool startupHarnessRunning;
    async Task RunStartupChecks()
    {
        var checks = new List<string>();
        void Check(bool pass, string label) { if (!pass) throw new InvalidOperationException(label); checks.Add(label); }
        try
        {
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            var template = GD.Load<PackedScene>("res://Scenes/Farm.tscn");
            foreach (string fault in new[] { "schema", "json", "identity", "missing-identity", "journal", "backup", "assets" })
            {
                string directory = Path.Combine(DataDirectory, "startup-" + fault); Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "farm.json");
                var original = new Farm(); string identity = Guid.NewGuid().ToString("N");
                if (fault is "identity" or "missing-identity") original.Join(identity, "เจ้าของเดิม");
                byte[] originalBytes = JsonSerializer.SerializeToUtf8Bytes(original);
                System.IO.File.WriteAllBytes(path, originalBytes);
                if (fault == "schema") System.IO.File.WriteAllText(path, "{\"Version\":2}");
                if (fault is "json" or "backup") System.IO.File.WriteAllText(path, "{broken");
                if (fault == "identity") System.IO.File.WriteAllText(Path.Combine(directory, "player.txt"), "corrupt");
                if (fault == "journal") System.IO.File.WriteAllText(path + ".import-pending", "{\"Version\":2}");
                if (fault == "backup") System.IO.File.WriteAllBytes(path + ".bak", originalBytes);
                byte[] before = System.IO.File.ReadAllBytes(path);
                var scene = template.Instantiate<FarmScene>();
                scene.DiagnosticDataDirectory = directory; scene.DiagnosticAssetFailure = fault == "assets";
                GetTree().Root.AddChild(scene);
                await NextFrame(SceneTree.SignalName.ProcessFrame);
                Check(!scene.ReadyToPlay, "startup fault disables gameplay: " + fault);
                var dialogs = scene.FindChildren("*", "AcceptDialog", true, false).OfType<AcceptDialog>().ToArray();
                Check(dialogs.Any(d => d.Visible), "startup fault shows an error dialog: " + fault);
                Check(dialogs.Any(d => d is ConfirmationDialog) == (fault == "backup"), "recovery offered only for validated backup: " + fault);
                scene._UnhandledInput(new InputEventKey { PhysicalKeycode = Key.Space, Pressed = true });
                GetTree().Root.RemoveChild(scene); var released = scene.shutdown; scene.QueueFree();
                await released.WaitAsync(TimeSpan.FromSeconds(5));
                await NextFrame(SceneTree.SignalName.ProcessFrame);
                Check(before.SequenceEqual(System.IO.File.ReadAllBytes(path)), "failed startup and teardown preserve farm bytes: " + fault);
                using var probe = new FileStream(path + ".lock", FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.None);
                Check(!GodotObject.IsInstanceValid(scene), "failed startup frees scene and profile lease: " + fault);
            }
            await Session.SaveAsync(); finalCheckpoint = true;
            string report = Path.Combine(ProjectSettings.GlobalizePath("res://reports"), "startup-" + DisplayServer.GetName() + ".json");
            await System.IO.File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { passed = checks.Count, checks,
                scope = "Seven injected startup faults on isolated profiles. Resource case tests composition failure; not a missing-PNG package test." }, new JsonSerializerOptions { WriteIndented = true }));
            if (sceneActive) { GD.Print("STARTUP PASS " + checks.Count); GetTree().Quit(); }
        }
        catch (Exception e) { if (sceneActive) { GD.PushError("STARTUP FAIL " + e); GetTree().Quit(1); } }
    }
}
