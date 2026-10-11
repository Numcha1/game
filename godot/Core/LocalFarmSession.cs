using SuanPleng;

namespace SeasonsTogether.Core;

/// <summary>Single authority for the vertical slice. No Godot or drawing dependencies.</summary>
public sealed class LocalFarmSession : IDisposable
{
    public Farm State { get; }
    public Farmer Player { get; }
    public string SavePath { get; }
    double saveElapsed;
    public LocalFarmSession(string directory, bool loadExisting = true)
    {
        Directory.CreateDirectory(directory);
        SavePath = Path.Combine(directory, "farm.json");
        State = loadExisting ? Farm.Load(SavePath) : new Farm();
        var identity = Path.Combine(directory, "player.txt");
        string secret = File.Exists(identity) ? File.ReadAllText(identity).Trim() : "";
        if (!Guid.TryParseExact(secret, "N", out _)) secret = Guid.NewGuid().ToString("N");
        File.WriteAllText(identity, secret);
        Player = State.Join(secret, "ชาวสวน");
        State.Owner = Player.Id;
    }
    public void Tick(double seconds)
    {
        Player.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        State.Tick(seconds);
        saveElapsed += seconds;
        if (saveElapsed >= 15) { Save(); saveElapsed = 0; }
    }
    public string Execute(string action, int plot = -1, string seed = "carrot")
    {
        int energy = Player.Energy, coins = State.Coins;
        string oldCrop = plot >= 0 && plot < State.Plots.Count ? State.Plots[plot].Crop : "";
        string result = action == "rescue" ? State.Rescue(Player) : State.Action(Player, action, plot, seed);
        if (Player.Energy < energy || State.Coins != coins)
            State.Effect(Player, action, plot, action == "plant" ? seed : oldCrop, State.Coins - coins);
        Save();
        return result;
    }
    public bool Move(double x, double y)
    {
        if (State.Paused || Player.Sleeping || !Farm.Walkable(x, y)) return false;
        Player.X = x; Player.Y = y; return true;
    }
    public void Save() => State.Save(SavePath);
    public void Dispose() => Save();
}
