# Premium item systems: verified milestone boundaries

Base `8b0388b`, branch `feature/premium-game-systems`. Reference images guide readability, functional furniture grouping and clear paths; no copyrighted reference sprites are imported. Native/dist stay untouched.

## Audit and migration plan

The existing eight crop IDs and economic/growth/quality rules are authoritative in `native/World.cs`, linked into Core. Bags are aggregate crop-count dictionaries; bought seeds are separate crop counts in `BuildingState.Village.Seeds`. Farmhouse storage has per-crop capacity 1,000,000, original proximity validation and replay protection. It does not have slot-bearing stacks. Bed/chest are fixed and interactive; other painted furniture is not a placed instance. Mira sells real seeds and Rowan offers the existing greenhouse. Bag/shop/storage use real Thai Godot UI. Atomic SaveService reads v1 and writes a strict v2 envelope after buildings/village are used. Unknown fields are rejected, so a future instance/placement migration cannot be introduced casually.

Dependency: Domain definitions + pure transaction validation -> Application authority/read models -> Infrastructure assets/save/network -> Godot UI. Domain never loads textures or Nodes. IDs remain ordinal, not translated names.

Keep existing v1/v2 payloads untouched for the foundation. Expose detached ItemStackView projections with null quality/freshness/durability rather than fabricating legacy values. Future persisted inventory slots and furniture instances require an explicit versioned migration, old-item preservation/overflow policy, validated commands and rendered scene acceptance. Do not run a second spoilage simulation: the original bag decay and chest preservation remain authoritative.

## M1 — Foundation

25 meaningful definitions: eight produce, eight seeds, seven existing tool actions and two existing farmhouse furnishings. Category enum reserves the requested future categories but no fake fish/mineral/material/recipe content is added. Typed icon references resolve existing original assets. Definitions expose stable localization keys with current Thai Name/Description fallback, nullable buy/sell prices, flags, equipment-slot contract and processing tags. Flags reflect implemented capabilities: furniture is not placeable/tradeable; item quality/freshness support remains false until an actual migration. Nullable instance projection metadata is separate from definitions.

Pure ItemTransactions validates storage transfer and seed purchase using wide arithmetic before existing Application mutations. BuySeeds and TransferStorage retain access checks, command replay handling and saves; UI seed offers/prices now come from Application/catalog. Selling validates supported items and total integer range before calling original Farm rules, preventing overflow from losing a large legacy bag. Existing daily prices, growth, yield and spoilage are unchanged.

Baseline Core 361 and Godot build passed before edits. M1 Core 380 passed after edits, including failures, overflow, currency/item conservation, replay and save roundtrip. Rendered NPC/seed-service suite is recorded separately. Build had zero compiler warnings/errors. Isolated profiles never write original user saves.

## Subsequent milestones

M2 adds a compatibility-first backpack presentation, category/sort controls and quick storage transfer after the M1 rendered gate. Persistent stack splitting/drag/drop, paid capacity upgrades and configurable equipment require subsequent runtime state/command work; do not mark them complete from a slot grid.

M3–M6 remain pending: real placeable inventory furniture, preview/rotation/collision/pickup/instances, sitting/outfits/refrigerator/workbench/cooking, Rowan item shop, recipes/unfinished jobs and migrated quality/freshness. Current definitions are not those gameplay systems. No 360-item catalog, 80 furniture objects, Internet multiplayer or new executable export is claimed.
