using System.Collections.ObjectModel;
using SuanPleng;

namespace SeasonsTogether.Core.Domain;

public enum ItemCategory { Produce, Seed, Furniture, Tool, Material, Mineral, Fish, AnimalProduct, Food, MagicalMaterial, Equipment, CraftingStation, QuestItem, Decoration }
public enum ItemIconSource { CropStage, FarmhouseRegion, ToolAction }
public enum ItemQualityRules { None, LegacyPlotQualityAffectsYield }
public enum ItemUsableBehavior { None, PlantCrop, FurnitureInteraction, FarmTool }
public enum EquipmentSlot { None, Headwear, Outfit, Footwear, Accessory, Charm }
public enum ItemQualityTier { Common, Good, Excellent, Exceptional }
public sealed record ItemIconReference(ItemIconSource Source, string Key);

/// <summary>Immutable metadata. Prices and gameplay execution remain authoritative in Farm.</summary>
public sealed record ItemDefinition(string ItemId, string Name, string Description,
    ItemCategory Category, ItemIconReference Icon, int MaxStack, int BasePrice,
    bool Sellable, ItemQualityRules QualityRules, ItemUsableBehavior UsableBehavior,
    string? CropId = null)
{
    public string DisplayNameKey => "item." + ItemId + ".name";
    public string DescriptionKey => "item." + ItemId + ".description";
    public int? BaseBuyPrice => Category == ItemCategory.Seed ? BasePrice : null;
    public int? BaseSellPrice => Sellable ? BasePrice : null;
    public bool Tradeable => false; // No peer inventory transfers implemented yet.
    public bool Placeable => false; // Current furnishings are fixed scene content.
    public bool Consumable => UsableBehavior == ItemUsableBehavior.PlantCrop;
    public EquipmentSlot EquipmentSlot => EquipmentSlot.None;
    public bool QualitySupport => false; // Plot quality affects yield; stack quality was never saved.
    public bool FreshnessSupport => false; // Preserve aggregate overnight spoilage until a migration exists.
    public IReadOnlyList<string> ProcessingTags => Array.Empty<string>();
    public string? ActionId { get; init; }
}

/// <summary>Detached runtime projection. Unknown legacy quality/freshness must remain null.</summary>
public sealed record ItemStackView(string ItemId, int Quantity, ItemQualityTier? Quality = null,
    double? Freshness = null, int? Durability = null);
public sealed record ItemShopOffer(string ItemId, int UnitBuyPrice, int MaximumQuantity);

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
        foreach(var (action, name) in new[] {("plant","ปลูก"),("water","รดน้ำ"),("harvest","เก็บเกี่ยว"),("treat","รักษา"),("fertilize","ใส่ปุ๋ย"),("stake","ค้ำต้น"),("clear","ถอน")})
            items.Add("tool:"+action, new("tool:"+action,name,"เครื่องมือฟาร์มเดิม ใช้ผ่านคำสั่งที่ตรวจสอบแล้ว",
                ItemCategory.Tool,new(ItemIconSource.ToolAction,action),1,0,false,ItemQualityRules.None,
                ItemUsableBehavior.FarmTool) {ActionId=action});
        return new ReadOnlyDictionary<string, ItemDefinition>(items);
    }
}
