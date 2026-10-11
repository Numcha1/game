using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Application;
using SuanPleng;

static class DefinitionChecks
{
    public static List<string> Run()
    {
        var checks = new List<string>();
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add(message); }
        Check(ItemCatalog.All.Count == 32 && PlaceableFurnitureCatalog.All.Count==6, "definitions retain legacy items and add six furniture types and wood");
        foreach (var (key, crop) in Farm.Crops)
        {
            var produce = ItemCatalog.Find(key)!;
            var seed = ItemCatalog.Find(ItemCatalog.SeedId(key))!;
            Check(produce.Name == crop.Name && produce.BasePrice == crop.Price && produce.Sellable && produce.MaxStack == int.MaxValue,
                "produce metadata preserves original name/base price/bag bound " + key);
            Check(seed.CropId == key && seed.BasePrice == crop.Cost && seed.MaxStack == 1000000 && !seed.Sellable && seed.UsableBehavior == ItemUsableBehavior.PlantCrop,
                "seed metadata preserves original buy/plant routing " + key);
            Check(produce.QualityRules == ItemQualityRules.LegacyPlotQualityAffectsYield && seed.QualityRules == ItemQualityRules.None,
                "quality metadata does not introduce stack quality prices " + key);
        }
        Check(ItemCatalog.Find(null) == null && ItemCatalog.Find("Carrot") == null && ItemCatalog.Find("unknown") == null,
            "unknown/null/case-altered item IDs do not silently alias save IDs");
        Check(ItemCatalog.All.Values.All(i => i.ItemId.Length > 0 && i.MaxStack > 0 && i.BasePrice >= 0 && i.Icon.Key.Length > 0), "definition metadata is valid");
        Check(FurnitureCatalog.All.Select(f => f.FurnitureId).Distinct().Count() == 2, "furniture stable IDs are unique");
        foreach (var definition in FurnitureCatalog.All)
        {
            Check(ItemCatalog.Find(definition.ItemId)?.Category == ItemCategory.Furniture && definition.Scene.Path == BuildingRegistry.Farmhouse.Scene && definition.Scene.NodePath.StartsWith("Actors/"),
                "furniture item/scene reference resolves " + definition.FurnitureId);
            Check(BuildingRegistry.Farmhouse.Obstacles.Contains(definition.Footprint) && definition.PlacementRules == FurniturePlacementRules.FixedSceneOnly && definition.UpgradeRequirements.Count == 0,
                "furniture retains registered collision and does not invent placement/upgrades " + definition.FurnitureId);
        }
        var chest = FurnitureCatalog.Find("moonroot.chest")!;
        Check(chest.InteractionPoint == BuildingRegistry.Farmhouse.Chest && chest.StorageCapacity?.MaximumPerItem == InteriorState.MaximumStorageStack && chest.StorageCapacity.MaximumDistinctItems == null && chest.PersistenceRules == FurniturePersistenceRules.ExistingBuildingStorage,
            "chest capacity and persistence match existing building storage");
        Check(FurnitureCatalog.Find("moonroot.bed")!.InteractionPoint == BuildingRegistry.Farmhouse.Bed && FurnitureCatalog.ForBuilding("village.mira-home").Count == 0,
            "fixed interactive furniture does not mislabel painted NPC room furniture");
        var farm = new Farm(); var player = farm.Join(Guid.NewGuid().ToString("N"), "definitions");
        player.Bag["carrot"] = 3;
        var read = new GameReadModel(farm, player);
        Check(read.Inventory()[0].Name == ItemCatalog.Find("carrot")!.Name && read.Inventory()[0].UnitPrice == farm.SalePrice("carrot"), "read model uses item metadata and authoritative daily price");
        return checks;
    }
}
