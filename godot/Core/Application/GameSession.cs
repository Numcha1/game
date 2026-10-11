using SeasonsTogether.Core.Domain;
using SuanPleng;

namespace SeasonsTogether.Core.Application;

[Flags]
public enum GameChange { None = 0, Position = 1, Player = 2, Plots = 4, Inventory = 8, Economy = 16, Time = 32, World = 64 }
public readonly record struct GameCommand(string Action, int Plot = -1, string Seed = "carrot", Guid Id = default);
public readonly record struct CommandResult(string Message, bool Changed, ActionEffect? Effect = null);

/// <summary>Main-thread coordinator. Delegates every gameplay mutation to the original Farm.</summary>
public sealed partial class GameSession : IAsyncDisposable
{
    internal Farm State { get; }
    internal Farmer Player { get; }
    public GameReadModel Read { get; }
    public string SavePath => saves.SavePath;
    public event Action<GameChange>? StateChanged;
    public event Action<string>? PersistenceFailed;
    readonly ISaveService saves;
    readonly IGameLog log;
    readonly int ownerThread = Environment.CurrentManagedThreadId;
    readonly Dictionary<Guid, CommandResult> recentCommands = new();
    double saveElapsed;
    bool stopped;
    public int ObserverCount => StateChanged?.GetInvocationList().Length ?? 0;
    public GameSession(ISaveService persistence, IGameLog? logger = null)
    {
        saves = persistence; log = logger ?? NullGameLog.Instance;
        State = saves.Load(); FarmInvariants.Validate(State);
        Buildings = saves.LoadBuildings(); Buildings.Validate(State);
        Player = State.Join(saves.LoadPlayerSession(), "ชาวสวน"); State.Owner = Player.Id;
        Read = new GameReadModel(State, Player, Buildings); log.Write("info", "session.started");
    }
    void CheckThread()
    {
        ObjectDisposedException.ThrowIf(stopped, this);
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Game state must be coordinated on its owning thread");
    }
    public void Tick(double seconds)
    {
        CheckThread();
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Player.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int day = State.Day, minute = (int)State.Minute;
        State.Tick(seconds);
        if (State.Day != day) StateChanged?.Invoke(GameChange.World | GameChange.Plots | GameChange.Inventory | GameChange.Player | GameChange.Time);
        else if ((int)State.Minute != minute) StateChanged?.Invoke(GameChange.Time);
        saveElapsed += seconds;
        if (saveElapsed >= 15) { saveElapsed = 0; saves.RequestSave(State,Buildings); }
        while (saves.TryReadFailure(out var error)) PersistenceFailed?.Invoke(error);
    }
    public CommandResult Execute(GameCommand command)
    {
        CheckThread();
        if (command.Id != Guid.Empty && recentCommands.TryGetValue(command.Id, out var existing)) return existing with { Changed = false, Effect = null };
        if (!ValidAction(command.Action)) return new("เลือกเครื่องมือก่อน", false);
        if (InsideBuilding && command.Action is not ("pause" or "wake")) return new("ออกไปฟาร์มก่อนใช้เครื่องมือ", false);
        bool plotAction = command.Action is "plant" or "water" or "harvest" or "treat" or "fertilize" or "stake" or "clear";
        if (plotAction && (command.Plot < 0 || command.Plot >= State.Plots.Count)) return new("เลือกแปลงผักก่อน", false);
        if (command.Action == "plant" && (command.Seed == null || !GameCatalog.Crops.ContainsKey(command.Seed))) return new("เลือกเมล็ดก่อน", false);
        int energy = Player.Energy, coins = State.Coins, day = State.Day;
        bool sleeping = Player.Sleeping, paused = State.Paused, greenhouse = State.Greenhouse;
        string oldCrop = command.Plot >= 0 && command.Plot < State.Plots.Count ? State.Plots[command.Plot].Crop : "";
        string message = command.Action == "rescue" ? State.Rescue(Player) : State.Action(Player, command.Action, command.Plot, command.Seed);
        ActionEffect? effect = null;
        if (Player.Energy < energy || State.Coins != coins)
        {
            State.Effect(Player, command.Action, command.Plot, command.Action == "plant" ? command.Seed : oldCrop, State.Coins - coins);
            effect = State.Effects[^1];
        }
        bool changed = effect != null || sleeping != Player.Sleeping || paused != State.Paused || greenhouse != State.Greenhouse || day != State.Day;
        var result = new CommandResult(message, changed, effect);
        if (changed)
        {
            saves.RequestSave(State,Buildings);
            StateChanged?.Invoke(GameChange.Player | GameChange.Plots | GameChange.Inventory | GameChange.Economy | GameChange.Time | GameChange.World);
            log.Write("info", "command." + command.Action);
        }
        if (command.Id != Guid.Empty)
        {
            recentCommands[command.Id] = result;
            if (recentCommands.Count > 512) recentCommands.Remove(recentCommands.Keys.First());
        }
        return result;
    }
    public static bool ValidAction(string? action) => action is "plant" or "water" or "harvest" or "treat" or "fertilize" or "stake" or "clear" or "sleep" or "wake" or "sell" or "pause" or "greenhouse" or "rescue";
    public bool Move(double x, double y)
    {
        CheckThread();
        if (State.Paused || Player.Sleeping) return false;
        if (Buildings.Locations.TryGetValue(Player.Id,out var indoor)) {
            if(BuildingRegistry.Find(indoor.BuildingId) is not { } building || !building.Walkable(new(x,y)))return false;
            if(indoor.X==x && indoor.Y==y)return true;
            indoor.X=x;indoor.Y=y;StateChanged?.Invoke(GameChange.Position);return true;
        }
        if (!Farm.Walkable(x,y)) return false;
        if (Player.X == x && Player.Y == y) return true;
        Player.X = x; Player.Y = y; StateChanged?.Invoke(GameChange.Position); return true;
    }
    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        CheckThread(); saves.RequestSave(State,Buildings); return saves.FlushAsync(cancellationToken);
    }
    public Task FlushAsync(CancellationToken cancellationToken = default) => saves.FlushAsync(cancellationToken);
    public async ValueTask DisposeAsync()
    {
        if (stopped) return; stopped = true; StateChanged = null; PersistenceFailed = null;
        await saves.DisposeAsync().ConfigureAwait(false); log.Write("info", "session.closed");
    }
}
