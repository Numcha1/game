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
    public IReadOnlyDictionary<string, ItemDefinition> ItemDefinitions => ItemCatalog.All;
    public IReadOnlyList<FurnitureDefinition> FurnitureDefinitions => FurnitureCatalog.ForBuilding(BuildingId);
    public IReadOnlyList<ItemShopOffer> ShopOffers(string npcId) => npcId=="mira"
        ? Array.AsReadOnly(ItemCatalog.All.Values.Where(i=>i.Category==ItemCategory.Seed).Select(i=>new ItemShopOffer(i.ItemId,i.BaseBuyPrice!.Value,99)).ToArray())
        : Array.Empty<ItemShopOffer>();
    public IReadOnlyList<ItemStackView> ItemStacks() => Array.AsReadOnly(
        player.Bag.Where(p=>p.Value>0).Select(p=>new ItemStackView(p.Key,p.Value))
        .Concat(buildings.Village.Seeds.Select(p=>new ItemStackView(ItemCatalog.SeedId(p.Key),p.Value))).ToArray());
    // Presentation slots only. Legacy aggregate inventory is not re-capped or split on load.
    public IReadOnlyList<ItemStackView?> BackpackSlots(ItemCategory? category=null,bool sortByQuantity=false)
    {
        var items=ItemStacks().Where(i=>category==null || ItemCatalog.All[i.ItemId].Category==category);
        var ordered=sortByQuantity ? items.OrderByDescending(i=>i.Quantity).ThenBy(i=>i.ItemId,StringComparer.Ordinal)
            : items.OrderBy(i=>i.ItemId,StringComparer.Ordinal);
        var slots=ordered.Cast<ItemStackView?>().ToList();
        while(slots.Count<24)slots.Add(null);
        return slots.AsReadOnly();
    }
    public bool InteriorLightsOn => buildings.Interiors.GetValueOrDefault(BuildingId)?.LightsOn ?? true;
    public IReadOnlyList<InventoryItem> Storage() => buildings.Interiors.TryGetValue(BuildingId,out var interior)
        ? Array.AsReadOnly(interior.Storage.OrderBy(pair=>pair.Key,StringComparer.Ordinal).Select(pair=>new InventoryItem(pair.Key,ItemCatalog.All[pair.Key].Name,pair.Value,farm.SalePrice(pair.Key))).ToArray())
        : Array.Empty<InventoryItem>();
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
        foreach (var pair in player.Bag) items[index++] = new(pair.Key, ItemCatalog.All[pair.Key].Name, pair.Value, farm.SalePrice(pair.Key));
        return Array.AsReadOnly(items);
    }
}
