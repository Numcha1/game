using System.Text.Json;

namespace SeasonsTogether.Core.Infrastructure.Configuration;

public sealed record DisplayPreferences(int Mode = 0, int TextScale = 0, bool ReducedMotion = false)
{
    public DisplayPreferences Normalize() => this with { Mode = Math.Clamp(Mode, 0, 2), TextScale = Math.Clamp(TextScale, 0, 2) };
}

/// <summary>Independent preferences; never stored in or migrated with Farm v1.</summary>
public sealed class DisplaySettings(string directory)
{
    readonly string path = Path.Combine(directory, "display.json");
    readonly SemaphoreSlim writes = new(1);
    public DisplayPreferences Load()
    {
        try { return (File.Exists(path) ? JsonSerializer.Deserialize<DisplayPreferences>(File.ReadAllText(path)) : null)?.Normalize() ?? new(); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public async Task SaveAsync(DisplayPreferences value)
    {
        await writes.WaitAsync().ConfigureAwait(false);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(value.Normalize())).ConfigureAwait(false);
            File.Move(temporary, path, true);
        }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } finally { writes.Release(); } }
    }
}
