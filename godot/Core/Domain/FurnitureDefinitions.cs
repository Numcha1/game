namespace SeasonsTogether.Core.Domain;

public enum FurnitureInteractionType { Sleep, Storage }
public enum FurniturePlacementRules { FixedSceneOnly }
public enum FurniturePersistenceRules { StaticSceneOnly, ExistingBuildingStorage }
public sealed record FurnitureSceneReference(string Path, string NodePath);
public readonly record struct FurnitureSize(int WidthPixels, int HeightPixels);
public sealed record FurnitureStorageCapacity(int MaximumPerItem, int? MaximumDistinctItems);
public sealed record FurnitureUpgradeRequirement(string ItemId, int Quantity);

/// <summary>Static furniture metadata, distinct from future placed furniture instances.</summary>
public sealed record FurnitureDefinition(string FurnitureId, string ItemId,
    FurnitureSceneReference Scene, FurnitureSize Size, BuildingObstacle Footprint,
    FurniturePlacementRules PlacementRules, FurnitureInteractionType InteractionType,
    FurnitureStorageCapacity? StorageCapacity,
    IReadOnlyList<FurnitureUpgradeRequirement> UpgradeRequirements,
    FurniturePersistenceRules PersistenceRules, string BuildingId, WorldPoint InteractionPoint);

public static class FurnitureCatalog
{
    public static IReadOnlyList<FurnitureDefinition> All { get; } = Create();
    public static FurnitureDefinition? Find(string? id) => All.FirstOrDefault(f => f.FurnitureId == id);
    public static IReadOnlyList<FurnitureDefinition> ForBuilding(string id) =>
        id == BuildingRegistry.Farmhouse.Id ? All : Array.Empty<FurnitureDefinition>();

    static IReadOnlyList<FurnitureDefinition> Create()
    {
        var house = BuildingRegistry.Farmhouse;
        // Use registered obstacles/interaction points, not a second collision rule.
        var bed = house.Obstacles[0];
        var chest = house.Obstacles[1];
        var noUpgrades = Array.AsReadOnly(Array.Empty<FurnitureUpgradeRequirement>());
        return Array.AsReadOnly(new[] {
            new FurnitureDefinition("moonroot.bed", "furniture:bed",
                new(house.Scene, "Actors/BedCluster"), Size(bed), bed,
                FurniturePlacementRules.FixedSceneOnly, FurnitureInteractionType.Sleep, null,
                noUpgrades, FurniturePersistenceRules.StaticSceneOnly, house.Id, house.Bed),
            new FurnitureDefinition("moonroot.chest", "furniture:chest",
                new(house.Scene, "Actors/StorageChest"), Size(chest), chest,
                FurniturePlacementRules.FixedSceneOnly, FurnitureInteractionType.Storage,
                new(InteriorState.MaximumStorageStack, null), noUpgrades,
                FurniturePersistenceRules.ExistingBuildingStorage, house.Id, house.Chest)
        });
    }
    // Size describes the registered blocking cluster, including its rug/stand, not painted body bounds.
    static FurnitureSize Size(BuildingObstacle footprint) =>
        new((int)Math.Ceiling(footprint.Width * 32), (int)Math.Ceiling(footprint.Height * 32));
}
