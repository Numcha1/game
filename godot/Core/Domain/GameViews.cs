using System.Collections.ObjectModel;
using SuanPleng;

namespace SeasonsTogether.Core.Domain;

public readonly record struct WorldView(int Day, double Minute, int Coins, string Weather, bool Greenhouse, bool Paused, int Season, int SeasonDay);
public readonly record struct PlayerView(double X, double Y, int Energy, bool Sleeping);
public readonly record struct PlotView(string Crop, double Growth, int RipeDay, int Moisture, int Fertility, int Quality, bool Disease, bool Stake, string LastCrop);
public readonly record struct InventoryItem(string Crop, string Name, int Quantity, int UnitPrice);
public sealed record CropDefinition(string Key, string Name, int Cost, int Days, IReadOnlyList<int> Seasons, int HarvestWindow);

/// <summary>Metadata projections of the legacy catalog. All prices/growth/validation still use Farm.</summary>
public static class GameCatalog
{
    public static readonly IReadOnlyList<string> CropKeys = Array.AsReadOnly(Farm.Crops.Keys.ToArray());
    public static readonly IReadOnlyList<string> SeasonNames = Array.AsReadOnly(Farm.SeasonNames);
    public static readonly IReadOnlyDictionary<string, CropDefinition> Crops = new ReadOnlyDictionary<string, CropDefinition>(
        Farm.Crops.ToDictionary(p => p.Key, p => new CropDefinition(p.Key, p.Value.Name, p.Value.Cost, p.Value.Days, Array.AsReadOnly(p.Value.Seasons), p.Value.Window)));
    public static (int X, int Y) PlotTile(int index) => Farm.Tile(index);
}
