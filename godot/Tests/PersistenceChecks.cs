using System.Text.Json;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using SuanPleng;

internal static class PersistenceChecks
{
    public static async Task<List<string>> Run(string root)
    {
        root = Path.Combine(root, "persistence-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var checks = new List<string>();
        void Check(bool value, string label) { if (!value) throw new Exception(label); checks.Add(label); }
        string directory = Path.Combine(root, "persistence-" + Guid.NewGuid().ToString("N"));
        await using (var save = new SaveService(directory))
        {
            var farm = save.Load();
            for (int i = 0; i < 200; i++) { farm.Coins = 160 + i; save.RequestSave(farm); }
            await save.FlushAsync();
            Check(Farm.Load(save.SavePath).Coins == 359, "coalesced saves persist newest immutable snapshot");
            Check(!Directory.EnumerateFiles(directory, "*.tmp").Any(), "atomic writer leaves no temporary files");
            farm.Coins = 400; save.RequestSave(farm); await save.FlushAsync();
            Check(Farm.Load(save.SavePath + ".bak").Coins == 359, "atomic replacement keeps previous version backup");
            string identity = save.LoadPlayerSession();
            Check(identity == save.LoadPlayerSession(), "valid identity reused without changing session");
            byte[] original = File.ReadAllBytes(save.SavePath);
            farm.Effects = null!;
            bool refused = false; try { save.RequestSave(farm); } catch (InvalidDataException) { refused = true; }
            Check(refused && original.SequenceEqual(File.ReadAllBytes(save.SavePath)), "invalid null Effects cannot overwrite a valid save");
        }
        foreach (var corruption in new[] { "{broken", "{\"Version\":2}", "{\"Effects\":null}", "{\"Plots\":null}" })
        {
            bool refused = false;
            try { SaveService.Deserialize(System.Text.Encoding.UTF8.GetBytes(corruption)); }
            catch (Exception e) when (e is JsonException or InvalidDataException) { refused = true; }
            Check(refused, "corrupt/unsupported schema refused " + checks.Count);
        }
        var malformed = new Farm(); malformed.Plots[0].Moisture = 101;
        bool invalidRange = false; try { FarmInvariants.Validate(malformed); } catch (InvalidDataException) { invalidRange = true; }
        Check(invalidRange, "structural ranges checked before mutation or serialization");
        string faultDirectory = Path.Combine(root, "fault-" + Guid.NewGuid().ToString("N"));
        await using (var save = new SaveService(faultDirectory))
        {
            Directory.CreateDirectory(save.SavePath); save.RequestSave(new Farm());
            bool failure = false; try { await save.FlushAsync(); } catch (IOException) { failure = true; }
            Check(failure && save.TryReadFailure(out _), "disk failure is observable through flush and error queue");
            Directory.Delete(save.SavePath); save.RequestSave(new Farm()); await save.FlushAsync();
            Check(Farm.Load(save.SavePath).Version == 1, "save worker recovers after disk failure");
        }
        string source = Path.Combine(root, "import-source-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(source);
        var imported = new Farm { Coins = 888 }; imported.Save(Path.Combine(source, "farm.json"));
        string sourceSession = Guid.NewGuid().ToString("N"); File.WriteAllText(Path.Combine(source, "player.txt"), sourceSession);
        byte[] sourceBytes = File.ReadAllBytes(Path.Combine(source, "farm.json"));
        string target = Path.Combine(root, "import-target-" + Guid.NewGuid().ToString("N"));
        await using (var save = new SaveService(target))
        {
            save.RequestSave(new Farm { Coins = 222 }); await save.FlushAsync(); string previousIdentity = save.LoadPlayerSession();
            await save.ImportLegacyAsync(source);
            Check(Farm.Load(save.SavePath).Coins == 888 && save.LoadPlayerSession() == sourceSession, "paired save and identity import succeeds");
            string backup = Directory.EnumerateFiles(target, "farm.json.before-import-*").First(x => !x.EndsWith(".player"));
            Check(Farm.Load(backup).Coins == 222 && File.ReadAllText(backup + ".player") == previousIdentity, "import keeps original save and identity backup");
            Check(sourceBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(source, "farm.json"))), "legacy source never written by import");
        }
        string rollback = Path.Combine(root, "rollback-" + Guid.NewGuid().ToString("N"));
        await using (var save = new SaveService(rollback))
        {
            save.RequestSave(new Farm { Coins = 333 }); await save.FlushAsync();
            Directory.CreateDirectory(Path.Combine(rollback, "player.txt")); // Force second-file install failure.
            bool failed = false; try { await save.ImportLegacyAsync(source); } catch (IOException) { failed = true; }
            Check(failed && Farm.Load(save.SavePath).Coins == 333, "identity failure rolls farm replacement back");
        }
        var concurrent = new SaveService(Path.Combine(root,"dispose-import"));
        concurrent.RequestSave(new Farm()); await concurrent.FlushAsync();
        Task importing = concurrent.ImportLegacyAsync(source);
        Task disposing = concurrent.DisposeAsync().AsTask();
        await Task.WhenAll(importing,disposing).WaitAsync(TimeSpan.FromSeconds(5));
        Check(Farm.Load(concurrent.SavePath).Coins == 888, "shutdown waits for active import before disposing storage resources");
        return checks;
    }
}
