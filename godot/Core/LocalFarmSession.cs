using SuanPleng;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Infrastructure.Persistence;
namespace SeasonsTogether.Core;
/// <summary>Synchronous compatibility facade for existing regression consumers only.</summary>
public sealed class LocalFarmSession : IDisposable
{
    readonly GameSession session;
    public Farm State => session.State;
    public Farmer Player => session.Player;
    public string SavePath => session.SavePath;
    public LocalFarmSession(string directory, bool loadExisting = true)
    {
        if (!loadExisting && File.Exists(Path.Combine(directory, "farm.json"))) throw new IOException("Use a new test profile");
        session = new GameSession(new SaveService(directory));
    }
    public void Tick(double seconds) => session.Tick(seconds);
    public string Execute(string action, int plot = -1, string seed = "carrot") => session.Execute(new GameCommand(action, plot, seed)).Message;
    public bool Move(double x, double y) => session.Move(x, y);
    public void Save() => session.SaveAsync().GetAwaiter().GetResult();
    public void Dispose() { Save(); session.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
}
