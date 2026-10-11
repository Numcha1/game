using Godot;
using SuanPleng;
using System.Text.Json;

namespace SeasonsTogether;
public partial class FarmScene
{
    internal string? DiagnosticDataDirectory { get; set; }
    internal bool DiagnosticAssetFailure { get; set; }
    static bool startupHarnessRunning;
    static string? recoveryProfile, recoveryIdentity;
    static readonly List<string> recoveryChecks = new();
    static byte[] recoveryBackup = Array.Empty<byte>();
    string PrepareRecoveryDiagnostic()
    {
        if (recoveryProfile != null) return recoveryProfile;
        recoveryProfile = Path.Combine(ProjectSettings.GlobalizePath("res://reports"), "smoke-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(recoveryProfile); recoveryIdentity = Guid.NewGuid().ToString("N");
        var farm = new Farm { Coins = 333, Day = 7 };
        var owner = farm.Join(recoveryIdentity, "เจ้าของเดิม"); owner.Bag["carrot"] = 3;
        recoveryBackup = JsonSerializer.SerializeToUtf8Bytes(farm);
        System.IO.File.WriteAllBytes(Path.Combine(recoveryProfile, "farm.json.bak"), recoveryBackup);
        System.IO.File.WriteAllText(Path.Combine(recoveryProfile, "farm.json"), "{broken");
        System.IO.File.WriteAllText(Path.Combine(recoveryProfile, "player.txt"), recoveryIdentity);
        return recoveryProfile;
    }
    static void RecoveryCheck(bool pass, string label)
    {
        if (!pass) throw new InvalidOperationException(label); recoveryChecks.Add(label);
    }
    async Task RunRecoveryDialogCheck(ConfirmationDialog dialog)
    {
        try {
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            RecoveryCheck(!ReadyToPlay && dialog.Visible, "damaged startup offers visible validated-backup confirmation");
            RecoveryCheck(Session == null, "damaged startup creates no playable session");
            dialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed); // Exercise actual UI callback, async save recovery and scene reload.
            await Task.Delay(TimeSpan.FromSeconds(8), sceneLifetime.Token);
            if (sceneActive) throw new TimeoutException("Recovery did not reload the scene");
        }
        catch (Exception e) { if (sceneActive) { GD.PushError("RECOVERY FAIL " + e); GetTree().Quit(1); } }
    }
    async Task RunRecoveredSceneCheck()
    {
        try {
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            RecoveryCheck(ReadyToPlay && !Farmer.InputBlocked, "recovered scene is playable after real reload");
            RecoveryCheck(Session.Read.World.Day == 7 && Session.Read.World.Coins == 333, "recovery preserves backup day and shared coins");
            RecoveryCheck(Session.Player.Id == Farm.Identity(recoveryIdentity!) && Session.State.Owner == Session.Player.Id, "recovery retains original player and ownership");
            RecoveryCheck(Session.Read.Inventory().Single().Quantity == 3, "recovery retains original inventory");
            RecoveryCheck(System.IO.File.ReadAllText(Directory.GetFiles(DataDirectory, "*.corrupt-*").Single()) == "{broken", "UI recovery keeps the damaged file unchanged");
            RecoveryCheck(recoveryBackup.SequenceEqual(System.IO.File.ReadAllBytes(Path.Combine(DataDirectory, "farm.json.bak"))), "UI recovery keeps source backup unchanged");
            RecoveryCheck(Session.ObserverCount == 2, "reloaded scene installs exactly its two application observers");
            RecoveryCheck(AssetCatalog.Shared == Art, "reloaded scene uses shared imported assets");
            await Session.SaveAsync(); finalCheckpoint = true;
            string path = Path.Combine(ProjectSettings.GlobalizePath("res://reports"), "recovery-ui-" + DisplayServer.GetName() + ".json");
            await System.IO.File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { passed = recoveryChecks.Count, checks = recoveryChecks,
                scope = "Actual recovery dialog callback, atomic backup restore and full scene reload. Isolated profile only." }, new JsonSerializerOptions { WriteIndented = true }));
            if (sceneActive) { GD.Print("RECOVERY PASS " + recoveryChecks.Count); GetTree().Quit(); }
        }
        catch (Exception e) { if (sceneActive) { GD.PushError("RECOVERY FAIL " + e); GetTree().Quit(1); } }
    }
    async Task RunStartupChecks()
    {
        var checks = new List<string>();
        void Check(bool pass, string label) { if (!pass) throw new InvalidOperationException(label); checks.Add(label); }
        try
        {
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            var template = GD.Load<PackedScene>("res://Scenes/Farm.tscn");
            foreach (string fault in new[] { "schema", "json", "identity", "missing-identity", "journal", "backup", "assets", "scene", "camera" })
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
                if (fault == "scene") scene.GetNode("Ground").Free();
                if (fault == "camera") scene.GetNode("Actors/Farmer/Camera").Free();
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
                scope = "Nine startup faults on isolated profiles, including removed ground/camera nodes. Resource case injects composition failure; not a missing-PNG package test." }, new JsonSerializerOptions { WriteIndented = true }));
            if (sceneActive) { GD.Print("STARTUP PASS " + checks.Count); GetTree().Quit(); }
        }
        catch (Exception e) { if (sceneActive) { GD.PushError("STARTUP FAIL " + e); GetTree().Quit(1); } }
    }
}
