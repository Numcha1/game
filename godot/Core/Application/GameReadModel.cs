using SeasonsTogether.Core.Domain;
using SuanPleng;

namespace SeasonsTogether.Core.Application;

/// <summary>No mutable legacy model is returned to consumers. Small views are value types.</summary>
public sealed class GameReadModel
{
    readonly Farm farm;
    readonly Farmer player;
    readonly BuildingState buildings;
    internal GameReadModel(Farm state, Farmer localPlayer, BuildingState? buildingState=null) { farm = state; player = localPlayer; buildings=buildingState ?? new(); }
    public WorldView World => new(farm.Day, farm.Minute, farm.Coins, farm.Weather, farm.Greenhouse, farm.Paused, farm.Season, farm.SeasonDay);
    public PlayerView Player => buildings.Locations.TryGetValue(player.Id,out var indoor) ? new(indoor.X,indoor.Y,player.Energy,player.Sleeping) : new(player.X, player.Y, player.Energy, player.Sleeping);
    public string BuildingId => buildings.Locations.GetValueOrDefault(player.Id)?.BuildingId ?? "";
    public bool InteriorLightsOn => buildings.Interiors.GetValueOrDefault(BuildingId)?.LightsOn ?? true;
    public int PlotCount => farm.Plots.Count;
    public string TomorrowWeather => farm.Forecast(farm.Day + 1);
    public SaleQuote QuoteSale() => new(farm.Day, Array.AsReadOnly(Inventory().OrderBy(item => item.Crop, StringComparer.Ordinal).ToArray()));
    public IReadOnlyList<string> Journal() => Array.AsReadOnly(farm.Journal.ToArray());
    public PlotView Plot(int index)
    {
        var p = farm.Plots[index];
        return new(p.Crop, p.Growth, p.RipeDay, p.Moisture, p.Fertility, p.Quality, p.Disease, p.Stake, p.LastCrop);
    }
    public IReadOnlyList<InventoryItem> Inventory()
    {
        var items = new InventoryItem[player.Bag.Count]; int index = 0;
        foreach (var pair in player.Bag) items[index++] = new(pair.Key, Farm.Crops[pair.Key].Name, pair.Value, farm.SalePrice(pair.Key));
        return Array.AsReadOnly(items);
    }
}
