using SeasonsTogether.Core.Infrastructure.Configuration;

public static class DisplayChecks
{
    public static async Task<IEnumerable<string>> Run(string directory)
    {
        var checks = new List<string>();
        void Check(bool pass, string label) { if (!pass) throw new InvalidOperationException(label); checks.Add(label); }
        string profile = Path.Combine(directory, "display-profile-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(profile);
        var settings = new DisplaySettings(profile);
        Check(settings.Load() == new DisplayPreferences(), "display preferences default without touching farm save");
        await settings.SaveAsync(new(2, 2, true));
        Check(new DisplaySettings(profile).Load() == new DisplayPreferences(2, 2, true), "display accessibility preferences survive reload");
        await settings.SaveAsync(new(-4, 15));
        Check(settings.Load() == new DisplayPreferences(0, 2), "display preference bounds validated");
        File.WriteAllText(Path.Combine(profile, "display.json"), "{broken");
        Check(settings.Load() == new DisplayPreferences(), "corrupted display preference safely defaults");
        await Task.WhenAll(Enumerable.Range(0, 20).Select(i => settings.SaveAsync(new(i % 3, i % 3, true))));
        Check(settings.Load() == new DisplayPreferences(1, 1, true) && !Directory.EnumerateFiles(profile, "*.tmp").Any(), "rapid display saves finish in order without temporary files");
        Check(!File.Exists(Path.Combine(profile, "farm.json")), "display preferences never create or migrate Farm v1");
        return checks;
    }
}
