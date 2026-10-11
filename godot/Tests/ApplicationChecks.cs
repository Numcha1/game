using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SuanPleng;
static class ApplicationChecks
{
    sealed class MemorySave : ISaveService
    {
        public Farm Farm = new(); public int Captures; public bool Closed;
        public string SavePath => "memory://farm";
        public Farm Load() => Farm;
        public string LoadPlayerSession() => "0123456789abcdef0123456789abcdef";
        public void RequestSave(Farm farm) => Captures++;
        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public bool TryReadFailure(out string error) { error = ""; return false; }
        public ValueTask DisposeAsync() { Closed = true; return ValueTask.CompletedTask; }
    }
    public static async Task<List<string>> Run()
    {
        var checks = new List<string>(); void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks.Add(name); }
        var persistence = new MemorySave(); var session = new GameSession(persistence);
        int changes = 0; Action<GameChange> observer = _ => changes++;
        session.StateChanged += observer; session.Move(8,6); changes = 0;
        var before = session.Read.World; var bad = session.Execute(new("plant",0,"unknown"));
        Check(!bad.Changed && changes == 0 && persistence.Captures == 0 && session.Read.World == before, "invalid seed has no mutation, save or event");
        Check(!session.Execute(new("water",24)).Changed && !session.Execute(new("hack")).Changed, "invalid plot and unknown command refused");
        var id = Guid.NewGuid(); var result = session.Execute(new("plant",0,"carrot",id));
        var planted = session.Read.World; var repeated = session.Execute(new("plant",0,"carrot",id));
        Check(result.Changed && result.Effect != null && !repeated.Changed && repeated.Effect == null && session.Read.World == planted && changes == 1 && persistence.Captures == 1, "duplicate local UUID cannot spend twice or repeat visual event");
        var detached = session.Read.Plot(0); session.Execute(new("water",0));
        Check(detached.Moisture == 50 && session.Read.Plot(0).Moisture == 95, "plot views are detached values");
        var inventory = session.Read.Inventory(); persistence.Farm.Players[0].Bag["carrot"] = 3;
        Check(inventory.Count == 0 && session.Read.Inventory()[0].Quantity == 3, "inventory views are immutable snapshots");
        Check(GameCatalog.Crops["carrot"].Days == Farm.Crops["carrot"].Days && session.Read.Inventory()[0].UnitPrice == persistence.Farm.SalePrice("carrot"), "catalog and inventory prices use original domain source");
        session.StateChanged -= observer; Check(session.ObserverCount == 0, "session observer detaches");
        session.Tick(15); Check(persistence.Captures == 3, "autosave captures through persistence boundary");
        bool wrongThread = await Task.Run(() => { try { session.Tick(1); return false; } catch (InvalidOperationException) { return true; } });
        Check(wrongThread, "background task cannot mutate main-thread game state");
        await session.DisposeAsync(); Check(persistence.Closed && session.ObserverCount == 0, "session shutdown drains persistence and clears observers");
        bool stopped = false; try { session.Move(8,6); } catch (ObjectDisposedException) { stopped = true; }
        Check(stopped, "closed session rejects late player operations");
        return checks;
    }
}
