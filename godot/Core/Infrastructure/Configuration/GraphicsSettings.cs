using System.Text.Json;

namespace SeasonsTogether.Core.Infrastructure.Configuration;

public sealed class GraphicsSettings(string directory)
{
    readonly string path = Path.Combine(directory, "graphics.json");
    public int Load()
    {
        try { return File.Exists(path) ? Math.Clamp(JsonSerializer.Deserialize<int>(File.ReadAllText(path)), 0, 2) : 2; }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return 2; }
    }
    public async Task SaveAsync(int quality, CancellationToken cancellationToken = default)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(Math.Clamp(quality, 0, 2)), cancellationToken).ConfigureAwait(false); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
