using System.Text;
using System.Text.Json;
using SeasonsTogether.Core.Infrastructure.Persistence;
using SuanPleng;

internal static class RecoveryChecks
{
    public static async Task<List<string>> Run(string reports)
    {
        string root = Path.Combine(reports, "recovery-profile-" + Guid.NewGuid().ToString("N"));
        var checks = new List<string>();
        void Check(bool pass, string label) { if (!pass) throw new Exception(label); checks.Add(label); }
        async Task<int> ProbeLock(string directory)
        {
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add(typeof(RecoveryChecks).Assembly.Location); start.ArgumentList.Add("--lock-probe"); start.ArgumentList.Add(directory);
            using var child = System.Diagnostics.Process.Start(start)!;
            try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
            catch { if (!child.HasExited) child.Kill(true); throw; }
            return child.ExitCode;
        }
        string locked = Path.Combine(root, "shared-profile");
        await using (var save = new SaveService(locked))
        {
            save.RequestSave(new Farm { Coins = 777 }); await save.FlushAsync();
            Check(await ProbeLock(locked) == 2 && save.Load().Coins == 777, "independent process cannot open or overwrite a live profile");
            bool recoveryRefused = false;
            try { await SaveService.RestoreBackupAsync(locked); } catch (IOException) { recoveryRefused = true; }
            Check(recoveryRefused && save.Load().Coins == 777, "backup recovery cannot race a live profile writer");
        }
        Check(await ProbeLock(locked) == 0, "profile lease releases after shutdown for an independent process");
        foreach (Encoding encoding in new Encoding[] { new UTF8Encoding(true), Encoding.Unicode, Encoding.BigEndianUnicode, Encoding.UTF32 })
        {
            string source = Path.Combine(root, "encoded-" + encoding.WebName); Directory.CreateDirectory(source);
            string session = Guid.NewGuid().ToString("N"); var farm = new Farm { Coins = 883 };
            farm.Join(session, "เจ้าของไทย").Bag["carrot"] = 3;
            byte[] json = encoding.GetPreamble().Concat(encoding.GetBytes(JsonSerializer.Serialize(farm))).ToArray();
            byte[] identity = encoding.GetPreamble().Concat(encoding.GetBytes(session)).ToArray();
            File.WriteAllBytes(Path.Combine(source, "farm.json"), json); File.WriteAllBytes(Path.Combine(source, "player.txt"), identity);
            await using (var save = new SaveService(source)) {
                Check(save.Load().Coins == Farm.Load(save.SavePath).Coins && save.LoadPlayerSession() == session,
                    "legacy BOM save and identity match original reader: " + encoding.WebName);
                Check(json.SequenceEqual(File.ReadAllBytes(save.SavePath)) && identity.SequenceEqual(File.ReadAllBytes(Path.Combine(source, "player.txt"))),
                    "startup leaves encoded legacy files byte-for-byte unchanged: " + encoding.WebName);
            }
            await using (var save = new SaveService(Path.Combine(root, "encoded-target-" + encoding.WebName))) {
                await save.ImportLegacyAsync(source);
                Check(save.Load().Find(session)?.Bag["carrot"] == 3 && save.LoadPlayerSession() == session
                    && json.SequenceEqual(File.ReadAllBytes(save.SavePath)), "paired import preserves encoded v1 ownership and inventory: " + encoding.WebName);
            }
        }
        byte[] oldFarm = JsonSerializer.SerializeToUtf8Bytes(new Farm { Coins = 333 });
        byte[] newFarm = JsonSerializer.SerializeToUtf8Bytes(new Farm { Coins = 888 });
        byte[] oldIdentity = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N"));
        byte[] newIdentity = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N"));
        // Reconstruct each on-disk interruption boundary, then start the real recovery service.
        for (int boundary = 0; boundary < 4; boundary++)
        {
            string directory = Path.Combine(root, "boundary-" + boundary); Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "farm.json"), identity = Path.Combine(directory, "player.txt");
            File.WriteAllBytes(path, boundary is 1 or 2 ? newFarm : oldFarm);
            File.WriteAllBytes(identity, boundary >= 2 ? newIdentity : oldIdentity);
            File.WriteAllBytes(path + ".import-pending", JsonSerializer.SerializeToUtf8Bytes(new { Version = 1, Farm = oldFarm, Identity = oldIdentity }));
            await using (var save = new SaveService(directory))
                Check(File.ReadAllBytes(path).SequenceEqual(oldFarm) && File.ReadAllBytes(identity).SequenceEqual(oldIdentity)
                    && !File.Exists(path + ".import-pending"), "interrupted paired import restores both files at boundary " + boundary);
            await using (var restarted = new SaveService(directory))
                Check(restarted.Load().Coins == 333, "recovery remains stable on second startup " + boundary);
        }
        string fresh = Path.Combine(root, "previously-empty"); Directory.CreateDirectory(fresh);
        string freshPath = Path.Combine(fresh, "farm.json"); File.WriteAllBytes(freshPath, newFarm);
        File.WriteAllBytes(Path.Combine(fresh, "player.txt"), newIdentity);
        File.WriteAllText(freshPath + ".import-pending", "{\"Version\":1,\"Farm\":null,\"Identity\":null}");
        await using (var save = new SaveService(fresh))
            Check(!File.Exists(freshPath) && !File.Exists(Path.Combine(fresh, "player.txt")), "interrupted import into empty profile restores absence of both files");
        foreach (string journal in new[] { "{broken", "{\"Version\":2}", "{\"Version\":1,\"Farm\":\"e30=\",\"Identity\":\"aW52YWxpZA==\"}" })
        {
            string directory = Path.Combine(root, "bad-" + checks.Count); Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "farm.json"); File.WriteAllBytes(path, newFarm);
            File.WriteAllText(path + ".import-pending", journal);
            bool refused = false; try { await using var save = new SaveService(directory); }
            catch (Exception e) when (e is IOException or InvalidDataException or JsonException) { refused = true; }
            Check(refused && File.ReadAllBytes(path).SequenceEqual(newFarm) && File.Exists(path + ".import-pending"), "invalid recovery journal refuses all file mutation " + checks.Count);
        }
        string damaged = Path.Combine(root, "damaged-identity"); Directory.CreateDirectory(damaged);
        string identityPath = Path.Combine(damaged, "player.txt"); File.WriteAllText(identityPath, "corrupt");
        await using (var save = new SaveService(damaged))
        {
            bool refused = false; try { save.LoadPlayerSession(); } catch (InvalidDataException) { refused = true; }
            Check(refused && File.ReadAllText(identityPath) == "corrupt", "damaged identity is never silently overwritten");
        }
        string missing = Path.Combine(root, "missing-identity"); Directory.CreateDirectory(missing);
        var owned = new Farm(); owned.Join(Encoding.UTF8.GetString(oldIdentity), "เจ้าของ");
        File.WriteAllBytes(Path.Combine(missing, "farm.json"), JsonSerializer.SerializeToUtf8Bytes(owned));
        await using (var save = new SaveService(missing))
        {
            bool refused = false; try { save.LoadPlayerSession(); } catch (InvalidDataException) { refused = true; }
            Check(refused && !File.Exists(Path.Combine(missing, "player.txt")), "missing identity for an inhabited farm cannot create a replacement owner");
        }
        string recovery = Path.Combine(root, "backup"); Directory.CreateDirectory(recovery);
        File.WriteAllBytes(Path.Combine(missing, "player.txt"), newIdentity);
        await using (var save = new SaveService(missing)) {
            bool refused = false; try { save.LoadPlayerSession(); } catch (InvalidDataException) { refused = true; }
            Check(refused && File.ReadAllBytes(Path.Combine(missing, "player.txt")).SequenceEqual(newIdentity), "unrelated valid identity cannot change the owner of an existing farm");
        }
        string recoveryPath = Path.Combine(recovery, "farm.json"); File.WriteAllBytes(recoveryPath + ".bak", oldFarm);
        File.WriteAllText(recoveryPath, "{damaged");
        await using (var save = new SaveService(recovery)) Check(save.CanRestoreBackup, "validated backup offered for explicit recovery");
        await SaveService.RestoreBackupAsync(recovery);
        Check(File.ReadAllBytes(recoveryPath).SequenceEqual(oldFarm)
            && File.ReadAllText(Directory.GetFiles(recovery, "*.corrupt-*").Single()) == "{damaged"
            && File.ReadAllBytes(recoveryPath + ".bak").SequenceEqual(oldFarm), "explicit backup recovery preserves damaged original and backup byte-for-byte");
        File.WriteAllText(recoveryPath + ".bak", "{broken"); byte[] current = File.ReadAllBytes(recoveryPath);
        bool backupRefused = false; try { await SaveService.RestoreBackupAsync(recovery); } catch (JsonException) { backupRefused = true; }
        Check(backupRefused && current.SequenceEqual(File.ReadAllBytes(recoveryPath)), "invalid backup cannot replace the live save");
        File.WriteAllBytes(recoveryPath + ".bak", oldFarm); File.WriteAllText(Path.Combine(recovery, "player.txt"), "corrupt");
        await using (var save = new SaveService(recovery))
            Check(!save.CanRestoreBackup, "backup recovery is not offered when it cannot fix damaged identity");
        string target = Path.Combine(root, "import-refused");
        await using (var save = new SaveService(target))
        {
            save.RequestSave(new Farm { Coins = 222 }); await save.FlushAsync();
            bool refused = false; try { await save.ImportLegacyAsync(missing); } catch (InvalidDataException) { refused = true; }
            Check(refused && save.Load().Coins == 222, "legacy farm without matching identity is rejected before replacement");
        }
        return checks;
    }
}
