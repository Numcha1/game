using System.Collections.ObjectModel;
using SuanPleng;

namespace SeasonsTogether.Core.Domain;

public enum ItemCategory { Produce, Seed, Furniture }
public enum ItemIconSource { CropStage, FarmhouseRegion }
public enum ItemQualityRules { None, LegacyPlotQualityAffectsYield }
public enum ItemUsableBehavior { None, PlantCrop, FurnitureInteraction }
public sealed record ItemIconReference(ItemIconSource Source, string Key);

/// <summary>Immutable metadata. Prices and gameplay execution remain authoritative in Farm.</summary>
public sealed record ItemDefinition(string ItemId, string Name, string Description,
    ItemCategory Category, ItemIconReference Icon, int MaxStack, int BasePrice,
    bool Sellable, ItemQualityRules QualityRules, ItemUsableBehavior UsableBehavior,
    string? CropId = null);

public static class ItemCatalog
{
    public static IReadOnlyDictionary<string, ItemDefinition> All { get; } = Create();
    public static ItemDefinition? Find(string? id) => id != null && All.TryGetValue(id, out var item) ? item : null;
    public static string SeedId(string cropId) => "seed:" + cropId;

    static IReadOnlyDictionary<string, ItemDefinition> Create()
    {
        var items = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        foreach (var (id, crop) in Farm.Crops)
        {
            // Existing produce IDs are save/protocol keys. Seeds remain in Village.Seeds.
            items.Add(id, new(id, crop.Name, "ผลผลิตจากฟาร์ม ราคาขายเปลี่ยนตามวัน",
                ItemCategory.Produce, new(ItemIconSource.CropStage, id), int.MaxValue,
                crop.Price, true, ItemQualityRules.LegacyPlotQualityAffectsYield, ItemUsableBehavior.None, id));
            items.Add(SeedId(id), new(SeedId(id), "เมล็ด" + crop.Name, "ใช้ปลูกผ่านคำสั่งปลูกและกฎฤดูกาลเดิม",
                ItemCategory.Seed, new(ItemIconSource.CropStage, id), 1000000,
                crop.Cost, false, ItemQualityRules.None, ItemUsableBehavior.PlantCrop, id));
        }
        items.Add("furniture:bed", new("furniture:bed", "เตียง", "เตียงประจำบ้าน ใช้นอนพัก",
            ItemCategory.Furniture, new(ItemIconSource.FarmhouseRegion, "bed"), 1, 0, false,
            ItemQualityRules.None, ItemUsableBehavior.FurnitureInteraction));
        items.Add("furniture:chest", new("furniture:chest", "หีบเก็บของ", "หีบประจำบ้าน ใช้ฝากและถอนผลผลิต",
            ItemCategory.Furniture, new(ItemIconSource.FarmhouseRegion, "chest"), 1, 0, false,
            ItemQualityRules.None, ItemUsableBehavior.FurnitureInteraction));
        return new ReadOnlyDictionary<string, ItemDefinition>(items);
    }
}
