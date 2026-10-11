> Status update 2026-10-12: the initial six-type placed furniture loop is now implemented and tested. [Current furniture architecture/migration](FURNITURE_SYSTEM.md) and [actual validation](FURNITURE_VALIDATION.md) supersede the pending placement/crafting statements below. Full slot inventory, wardrobes, timed production and furniture co-op remain open. The following text retains earlier M1/M2 evidence.

# Premium item systems: verified milestone boundaries

Base `8b0388b`, branch `feature/premium-game-systems`. Reference images guide readability, functional furniture grouping and clear paths; no copyrighted reference sprites are imported. Native/dist stay untouched.

## Audit and migration plan

The existing eight crop IDs and economic/growth/quality rules are authoritative in `native/World.cs`, linked into Core. Bags are aggregate crop-count dictionaries; bought seeds are separate crop counts in `BuildingState.Village.Seeds`. Farmhouse storage has per-crop capacity 1,000,000, original proximity validation and replay protection. It does not have slot-bearing stacks. Bed/chest are fixed and interactive; other painted furniture is not a placed instance. Mira sells real seeds and Rowan offers the existing greenhouse. Bag/shop/storage use real Thai Godot UI. Atomic SaveService reads v1 and writes a strict v2 envelope after buildings/village are used. Unknown fields are rejected, so a future instance/placement migration cannot be introduced casually.

Dependency: Domain definitions + pure transaction validation -> Application authority/read models -> Infrastructure assets/save/network -> Godot UI. Domain never loads textures or Nodes. IDs remain ordinal, not translated names.

Keep existing v1/v2 payloads untouched for the foundation. Expose detached ItemStackView projections with null quality/freshness/durability rather than fabricating legacy values. Future persisted inventory slots and furniture instances require an explicit versioned migration, old-item preservation/overflow policy, validated commands and rendered scene acceptance. Do not run a second spoilage simulation: the original bag decay and chest preservation remain authoritative.

## M1 — Foundation

25 meaningful definitions: eight produce, eight seeds, seven existing tool actions and two existing farmhouse furnishings. Category enum reserves the requested future categories but no fake fish/mineral/material/recipe content is added. Typed icon references resolve existing original assets. Definitions expose stable localization keys with current Thai Name/Description fallback, nullable buy/sell prices, flags, equipment-slot contract and processing tags. Flags reflect implemented capabilities: furniture is not placeable/tradeable; item quality/freshness support remains false until an actual migration. Nullable instance projection metadata is separate from definitions.

Pure ItemTransactions validates storage transfer and seed purchase using wide arithmetic before existing Application mutations. BuySeeds and TransferStorage retain access checks, command replay handling and saves; UI seed offers/prices now come from Application/catalog. Selling validates supported items and total integer range before calling original Farm rules, preventing overflow from losing a large legacy bag. Existing daily prices, growth, yield and spoilage are unchanged.

Baseline Core 361 and Godot build passed before edits. M1 Core 380 and rendered NPC/seed-service 34 passed after edits, including failures, overflow, currency/item conservation, replay and save roundtrip. Build had zero compiler warnings/errors. Isolated profiles never write original user saves. The existing single ObjectDB cleanup warning remains in the NPC run.

## M2 — Implemented compatibility slice; full milestone remains open

The backpack now shows produce and actually owned seeds in one 24-cell display, with exact-count tooltips, icons, category filters and deterministic ID/quantity sorting. Seed selection connects to the original planting hotbar without consuming an item. Produce opens the existing item/daily-price dialog. Existing seven-tool keyboard/mouse hotbar stays intact. The 24 cells are a view of original aggregate counts, not a new authoritative capacity restriction; all legacy counts including huge stacks remain intact. Empty cells do not create items. Filtering/sorting do not alter saves or sell unsupported categories.

Chest quantity deposit/withdraw and new per-crop “all” buttons use the existing validated transaction path, proximity checks, replay UUIDs and atomic building save. The buttons are rebuilt on inventory events; the UI never calls freed nodes. The real scene suite tests the backpack at requested 1280x720, 1600x900 and 1920x1080 windows, filters/sort, seed selection, quick deposit/withdraw and restoration of movement. PNGs are engine viewport readbacks, with integer stretch possibly producing a smaller image than the requested window, as documented in world visual validation.

Visual QA caught narrow Thai filter buttons wrapping vertically, then corrected explicit widths and added a height assertion. A separate early test failed because Godot sanitizes colons in Node names; presentation now uses sanitized names while authoritative ItemIds retain their colons.

This is not a persisted 24-slot inventory, drag/drop or splitting system. Those features, backpack capacity upgrades, configurable hotbar slots, equipment, unique placed container IDs and new storage types remain open. Keeping that boundary preserves v1/v2 saves without silently merging or reinterpreting original inventories. Full M2 must not be marked complete yet.

## Executed final evidence

Full rendered verifier passed: Core 383, Native 35, startup 60, recovery 10, farming 101, Premium UI 180, buildings 51, NPC 34, indoor layout 46, composition 190 and lifecycle 70. Its first inventory run passed 18 checks; after the Thai-width fix the project was rebuilt and the affected inventory scene was rerun with 21 checks, followed by Premium UI 180 again. `verify.ps1` now requires the 21-check gate. Core includes real loopback peer fixtures, not Internet multiplayer acceptance.

Current farm benchmark (600 frames/quality): Low 60.00 FPS / P95 16.78ms, Medium 60.02 / 16.76ms, High 60.00 / 16.70ms. i5-12400F / RTX3060, Godot OpenGL Compatibility. This is a finite scene workload, not a 60-FPS guarantee for future inventory/placement/production systems. Ten teardown / 100 menu cycles ended at 437 nodes, 50 resources, zero orphans; that is not lifetime leak certification. The existing one-instance ObjectDB exit warning remains in NPC runs.

Actual engine screenshots: [backpack 1280 window](../reports/items-backpack-1280x720.png), [1600 window](../reports/items-backpack-1600x900.png), [1920 window](../reports/items-backpack-1920x1080.png), [storage with quick transfers](../reports/items-storage.png). Reports: [M1 Core](../reports/items-m1-core.json), [M1 scene](../reports/items-m1-scenes.json), [M2 inventory scene](../reports/items-inventory-Windows.json), [current full Core](../reports/production-core-tests.json), [current frame measurements](../reports/godot-smoke-Windows.json).

Changed files: `Core/Domain/ItemDefinitions.cs`, `ItemTransactions.cs`, `Core/Application/GameReadModel.cs`, `GameSession.cs`, `GameSession.Npcs.cs`, `GameSession.Buildings.cs`, `Infrastructure/Assets/AssetCatalog.cs`, `Presentation/UI/FarmHud.Npcs.cs`, `FarmHud.Inventory.cs`, `FarmHud.Storage.cs`, `FarmHud.cs`, `FarmItemCard.cs`, `Diagnostics/FarmScene.NpcChecks.cs`, `FarmScene.InventoryChecks.cs`, `Tests/DefinitionChecks.cs`, `ItemTransactionChecks.cs`, `Program.cs`, `verify.ps1`, corresponding UID metadata, documentation and isolated result/screenshots. No native/dist code or original art changes.

## Subsequent milestones

Persistent stack splitting/drag/drop, paid capacity upgrades and configurable equipment require subsequent runtime state/command work; do not mark them complete from a slot grid.

M3–M6 remain pending: real placeable inventory furniture, preview/rotation/collision/pickup/instances, sitting/outfits/refrigerator/workbench/cooking, Rowan item shop, recipes/unfinished jobs and migrated quality/freshness. Current definitions are not those gameplay systems. No 360-item catalog, 80 furniture objects, Internet multiplayer or new executable export is claimed.
