using System.Text.Json;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SuanPleng;

internal static class GameplayMatrixChecks
{
    sealed class MemorySave : ISaveService
    {
        public Farm Farm = new();
        public const string Identity = "0123456789abcdef0123456789abcdef";
        public string SavePath => "memory://matrix";
        public Farm Load() => Farm;
        public string LoadPlayerSession() => Identity;
        public void RequestSave(Farm farm) { }
        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public bool TryReadFailure(out string error) { error = ""; return false; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
    public static async Task<List<string>> Run()
    {
        var checks = new List<string>();
        void Check(bool pass, string label) { if (!pass) throw new Exception(label); checks.Add(label); }
        foreach (var crop in Farm.Crops)
        {
            for (int season = 0; season < 4; season++)
            {
                var save = new MemorySave { Farm = new Farm { Day = season * 14 + 1, Coins = 1000 } };
                await using var session = new GameSession(save); session.Move(8, 6);
                var original = JsonSerializer.Deserialize<Farm>(JsonSerializer.Serialize(save.Farm))!;
                string expected = original.Action(original.Find(MemorySave.Identity)!, "plant", 0, crop.Key);
                var actual = session.Execute(new("plant", 0, crop.Key));
                Check(actual.Message == expected && session.Read.World.Coins == original.Coins
                    && session.Read.Player.Energy == original.Find(MemorySave.Identity)!.Energy
                    && session.Read.Plot(0).Crop == original.Plots[0].Crop,
                    "application matches original planting rule: " + crop.Key + " season " + season);
            }
            var cycleSave = new MemorySave { Farm = new Farm { Day = crop.Value.Seasons[0] * 14 + 1, Coins = 1000 } };
            await using var cycle = new GameSession(cycleSave); cycle.Move(8, 6);
            Check(cycle.Execute(new("plant", 0, crop.Key)).Changed, "full cycle starts: " + crop.Key);
            for (int day = 0; day < crop.Value.Days; day++)
            {
                cycleSave.Farm.Weather = "แดดอ่อน"; cycleSave.Farm.Plots[0].Moisture = 50;
                cycle.Move(4, 4); cycle.Execute(new("sleep"));
            }
            Check(cycle.Read.Plot(0).RipeDay == cycle.Read.World.Day
                && cycle.Read.Plot(0).Growth == crop.Value.Days
                && GameCatalog.Crops[crop.Key].HarvestWindow == crop.Value.Window,
                "growth and harvest metadata use original rule: " + crop.Key);
            cycle.Move(8, 6); Check(cycle.Execute(new("harvest", 0)).Changed, "harvest succeeds: " + crop.Key);
            int previousCoins = cycle.Read.World.Coins, expectedPrice = cycleSave.Farm.SalePrice(crop.Key) * 3;
            cycle.Move(20, 8); cycle.Execute(new("sell"));
            Check(cycle.Read.World.Coins == previousCoins + expectedPrice && cycle.Read.Inventory().Count == 0,
                "full crop cycle sells at original daily price: " + crop.Key);
        }
        var soil = new MemorySave(); await using (var session = new GameSession(soil))
        {
            session.Move(8, 6); session.Execute(new("plant", 0));
            soil.Farm.Plots[0].Disease = true;
            session.Execute(new("treat", 0));
            Check(!session.Read.Plot(0).Disease && session.Read.Player.Energy == 90 && session.Read.World.Coins == 142,
                "disease treatment spends original coins and energy");
            var journal = session.Read.Journal(); var forecast = session.Read.TomorrowWeather;
            soil.Farm.Log("new event");
            Check(journal.Count + 1 == session.Read.Journal().Count && forecast == soil.Farm.Forecast(soil.Farm.Day + 1),
                "journal is detached and forecast delegates to authoritative world");
            soil.Farm.Plots[0].LastCrop = "turnip";
            Check(session.Read.Plot(0).LastCrop == "turnip", "rotation history is available without mutable state access");
            soil.Farm.Plots[0].Fertility = 29; soil.Farm.Plots[0].Moisture = 50; soil.Farm.Weather = "แดดอ่อน";
            session.Move(4, 4); session.Execute(new("sleep"));
            Check(session.Read.Plot(0).Growth == .5, "weak soil retains half-speed growth rule");
        }
        return checks;
    }
}
