> Historical definition baseline at 8b0388b. Current 25-item foundation and inventory compatibility work: [ITEM_SYSTEM_MILESTONES.md](ITEM_SYSTEM_MILESTONES.md).

# Item and furniture definition contract

Definitions are immutable Domain metadata without Godot, WinForms, delegates, Nodes or graphics types. Runtime state is separate. `GameReadModel.ItemDefinitions` and `FurnitureDefinitions` expose these catalogs to presentation.

## ItemDefinition

| Field | Current meaning |
| --- | --- |
| ItemId | Stable ordinal ID. Produce retains `carrot` etc.; seed metadata uses `seed:carrot`; fixed furnishing items use `furniture:bed` / `furniture:chest` |
| Name / Description | Thai display metadata |
| Category | Produce, Seed, Furniture |
| Icon | Typed asset reference resolved/cached by Infrastructure `AssetCatalog.ItemIcon`, not a Domain texture |
| MaxStack | Metadata reflecting current bounds: produce `int.MaxValue`, seeds 1,000,000, fixed furniture 1. It does not impose new stack limits on saves |
| BasePrice | Produce `Farm.Crops.Price`, seed `Farm.Crops.Cost`. Daily selling still uses `Farm.SalePrice`, not BasePrice. Fixed furniture has 0 and is neither offered nor sold |
| Sellable | Only existing produce; seeds/fixed furniture are not added to the selling command |
| QualityRules | Existing plot quality affects harvest yield; there is no quality-bearing inventory stack or quality-price multiplier |
| UsableBehavior | Typed command-routing metadata: None, PlantCrop, FurnitureInteraction. Existing Application commands remain the only execution path |
| CropId | Original crop key for produce/seed command routing |

Eight produce, eight seeds and two existing interactive furnishings are registered. `ItemCatalog` derives prices/names from `native/World.cs`; no second crop rules or recipes are introduced. Definitions are process-lifetime metadata, not an editor for mutable runtime `Farm.Crops`.

Inventory/storage read models now take display names from ItemCatalog and daily price from Farm. Existing item cards resolve their icons through definition references, returning the same ripe crop textures. Seed definitions reuse the existing crop icon convention. Furniture icons sample the actual farmhouse background; no new art or placeholder is introduced. Catalog initialization validates all 18 icon references.

## FurnitureDefinition

| Field | Current meaning |
| --- | --- |
| FurnitureId | Stable fixed instance identity: `moonroot.bed`, `moonroot.chest` |
| ItemId | Foreign key to the furnishing ItemDefinition |
| Scene | Existing farmhouse scene path + generated `Actors/BedCluster` / `Actors/StorageChest` node path; these are not standalone placeable scenes |
| Size / Footprint | Size in logical pixels of the existing blocking cluster; footprint is the registered BuildingObstacle in world-tile coordinates. Includes bed rug/stand, not only the painted bed body |
| PlacementRules | FixedSceneOnly; moving/buying/placing is unsupported |
| InteractionType | Sleep or Storage; original commands/access checks apply |
| StorageCapacity | Chest maximum per crop equals `InteriorState.MaximumStorageStack`; no separate distinct-slot limit. Bed has no storage |
| UpgradeRequirements | Empty immutable list; no upgrade is offered or inferred from this |
| PersistenceRules | Bed is static scene content; chest contents use existing building storage serialization |
| BuildingId / InteractionPoint | Existing registered farmhouse and bed/chest coordinates |

Only furniture with an implemented interactive role is registered. Painted NPC room furnishings are not advertised as placeable or usable. Generated node references are checked after entering the real farmhouse. Metadata does not replace collision, ownership, distance or storage validation.

## Save/network compatibility and next module

No save version, serializer, protocol, native source, crop command, price formula or furniture coordinates change. Existing bags keep original crop IDs; `Village.Seeds` still stores crop keys rather than `seed:*`. Definition IDs are not serialized into existing dictionaries. New IDs are therefore not silently inserted into legacy inventory/save data.

Future placement needs a distinct FurnitureInstance (instance ID, definition ID, room, position, orientation, state), standalone authored scenes, validated placement commands and a versioned save migration. Upgrade recipes and quality-bearing ItemStacks similarly need explicit new state/commands. This commit supplies definitions integrated into existing read/UI paths, not those gameplay features.

## Executed verification

Core suite passes **361** assertions (326 existing + 35 definition checks), including original save/storage/seed/rules and loopback network fixtures. Godot builds with zero compiler warnings/errors. Rendered building checks pass **51** (49 existing + two node-reference checks); rendered Premium UI checks pass **180**. Tests use isolated profiles. The first new test fixture used an invalid session string; corrected to the existing GUID format before rerunning successfully.

Changed sources: `Core/Domain/ItemDefinitions.cs`, `FurnitureDefinitions.cs`, `Core/Application/GameReadModel.cs`, `Infrastructure/Assets/AssetCatalog.cs`, `Presentation/UI/FarmHud.cs`, `Diagnostics/FarmScene.BuildingChecks.cs`, `Tests/DefinitionChecks.cs`, `Tests/Program.cs`, generated UID metadata and reports. No appearance redesign or furniture placement was performed.
