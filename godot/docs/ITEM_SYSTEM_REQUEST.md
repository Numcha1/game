# SEASONS TOGETHER — PREMIUM ITEM, INVENTORY & INTERACTIVE FURNITURE MASTER PROMPT

## ROLE

Act as a Lead Godot 4 .NET Developer, Senior C# Gameplay Architect, Economy Designer, Inventory Systems Engineer, Technical Artist, UI/UX Designer, and QA Lead.

Your mission is to implement a fully integrated, production-oriented item and interactive furniture system for Seasons Together.

## PROJECT

Repository: https://github.com/Numcha1/game
Target branch: feature/premium-game-systems
Engine: Godot 4 .NET / C#
Platform: Windows PC
Visual Style: Premium HD Pixel Art
Primary Language: Thai

## LOCKED GAME DESIGN

- Genre: Fantasy Farming + Life Simulation + Exploration
- World Structure: Connected Zones
- Combat: None
- Farming Complexity: Deep Simulation
- Primary Mode: Single-player
- Future Mode: 4-player Cooperative Multiplayer

## MAIN OBJECTIVE

Create a unified system connecting:

1. Item Definitions
2. Player Inventory
3. Hotbar and Equipment
4. Storage Containers
5. Furniture Items
6. Furniture Placement
7. Interactive Furniture
8. Item Shops
9. Buying and Selling
10. Crafting and Production
11. Save/Load and Migration
12. Future Multiplayer Authority

Everything must work within real gameplay.

Do not build decorative-only mockups or disconnected demonstration systems.

---

# PHASE 0 — PROJECT AUDIT

Inspect the current repository before implementing anything.

Review:
- Existing inventory model
- Existing farming actions
- Existing crop definitions
- GameSession and authoritative gameplay rules
- SaveService and existing save schema
- Building and Interior System
- NPC and shop implementations
- Farm HUD and inventory UI
- Sprite atlases and rendering pipeline
- Automated tests

Identify what already works and what needs improvement.

Preserve existing functionality.

Do not recreate systems unnecessarily.

Do not modify native/ or dist/ without a documented and necessary compatibility reason.

Create a baseline build and run existing tests before making changes.

---

# PHASE 1 — UNIFIED ITEM DEFINITIONS

Create a data-driven Item Catalog.

Every item must have a stable identifier.

Suggested properties:

- ItemId
- DisplayNameKey
- DescriptionKey
- Category
- IconId
- MaxStack
- BaseBuyPrice
- BaseSellPrice
- Sellable
- Tradeable
- Placeable
- Consumable
- EquipmentSlot
- QualitySupport
- FreshnessSupport
- ProcessingTags

Separate static ItemDefinition data from runtime ItemInstance state.

Never use localized item names as authoritative IDs.

## ITEM CATEGORIES

Support:

- Seeds
- Crops
- Tools
- Materials
- Minerals
- Fish
- Animal Products
- Food
- Magical Materials
- Equipment
- Furniture
- Crafting Stations
- Quest Items
- Decorations

## CONTENT SCOPE

Long-term target: approximately 360 unique item definitions.

Do NOT create hundreds of meaningless placeholder items.

Begin with a coherent starter catalog containing the current eight crops and their seeds, required materials, tools, and functional furniture.

Preserve original IDs and economic values unless an explicit migration and balance change is approved.

---

# PHASE 2 — PLAYER INVENTORY

Implement a reliable inventory system with:

- Inventory slots
- Stack limits
- Stack merging
- Stack splitting
- Drag-and-drop
- Quick transfer
- Sorting
- Filtering
- Tooltips
- Full inventory feedback
- Capacity upgrades
- Inventory transaction validation

Proposed backpack capacities:

Level 1: 24 slots
Level 2: 36 slots
Level 3: 48 slots
Level 4: 60 slots
Level 5: 72 slots

Do not impose incompatible restrictions on older inventories.

Preserve all legacy items during save migration.

Inventory transactions must never silently delete or duplicate items.

---

# PHASE 3 — HOTBAR AND EQUIPMENT

Implement a configurable Hotbar.

Support:
- Item selection
- Tool selection
- Keyboard shortcuts
- Mouse controls
- Active slot feedback
- Correct tool usage
- Input remapping compatibility

Equipment slots:

- Headwear
- Outfit
- Footwear
- Accessory
- Charm

The game has no combat.

Clothing is primarily cosmetic.

Special equipment may support farming, exploration, or environmental simulation.

Preserve existing farming input behavior.

---

# PHASE 4 — STORAGE SYSTEM

Implement fully functional storage containers.

Initial types:

- Wooden Chest: 24 slots
- Large Storage Cabinet: 48 slots
- Refrigerator: food storage and preservation
- Wardrobe: clothing and outfit storage
- Arcane Chest: advanced storage unlock

Storage must support:
- Deposit items
- Withdraw items
- Stack merging
- Quick transfer
- Sorting
- Unique container IDs
- Save/load
- Error recovery

Prevent:
- Duplication through repeated commands
- Invalid withdrawals
- Item loss during scene transitions
- Container removal while occupied unless a safe explicit transfer rule is defined

---

# PHASE 5 — FURNITURE ITEM SYSTEM

Furniture must exist both as inventory items and as placed world objects.

Each FurnitureDefinition must specify:

- FurnitureId
- Corresponding ItemId
- Scene or Sprite Resource
- Footprint
- Placement Category
- Allowed Surfaces
- Collision Shape
- Interaction Type
- Rotation Support
- Storage Capacity
- Upgrade Level
- Placement Rules
- Persistent State Schema

Every placed furniture instance must have a stable unique InstanceId.

Furniture should support:

1. Buying from shops
2. Crafting
3. Storing in inventory
4. Placement preview
5. Position validation
6. Rotation where supported
7. Placement confirmation
8. Interacting after placement
9. Moving furniture
10. Returning furniture to inventory

Furniture placement and removal must be atomic transactions.

Never allow item duplication by rapidly repeating placement or pickup actions.

---

# PHASE 6 — INTERACTIVE FURNITURE

Implement actual gameplay functions.

## A. Bed

- Sleep interaction
- Day transition
- Energy restoration
- Correct animations
- Prevent repeated sleep commands

## B. Chairs and Sofas

- Sit interaction
- Seated player animation
- Standing interaction
- Correct collision and visual ordering

## C. Storage Chest

- Open inventory UI
- Deposit and withdraw items
- Persistent container state

## D. Wardrobe

- Clothing inventory
- Outfit selection
- Cosmetic preview
- Save selected outfit

## E. Refrigerator

- Store food
- Apply appropriate preservation rules
- Prevent duplicate spoilage calculations

## F. Cooking Stove

- Open cooking UI
- Ingredient validation
- Produce actual food items
- Apply processing time where required

## G. Workbench

- Open crafting UI
- Validate recipe ingredients
- Consume materials atomically
- Deliver crafted output

## H. Bookshelf

- Open owned books
- Display readable Thai text
- Support unlocked recipes and knowledge

## I. Writing Desk

- Open the Journal
- Support gameplay records and notes

## J. Fireplace and Lamps

- Toggle lighting
- Persist on/off state
- Respect performance settings

## K. Arcane Research Desk

- Research magical farming upgrades
- Consume appropriate resources
- Unlock actual game capabilities

## L. Botanical Study Table

- Inspect crop properties
- Inspect soil information
- Display environmental recommendations

Do not create interactable objects that only show meaningless placeholder messages.

For furniture whose underlying gameplay feature is not ready, mark it as deferred instead of presenting it as complete.

---

# PHASE 7 — ITEM QUALITY AND FRESHNESS

Support future item-instance properties:

- Quality
- Freshness
- Durability where relevant
- Optional metadata

Proposed quality tiers:

- Common
- Good
- Excellent
- Exceptional

Only compatible item stacks may merge.

Preserve the original farming quality rules.

Do not apply both the existing overnight spoilage system and a new freshness decay system simultaneously.

Introduce new spoilage behavior only through an explicitly tested migration.

---

# PHASE 8 — SHOP INTEGRATION

Integrate furniture and items into actual NPC shops.

## Rowan's Carpenter Shop

Sell:
- Wooden Chest
- Bed
- Table
- Chair
- Shelf
- Cabinet
- Basic decorations

Offer:
- Furniture purchase
- Building upgrades
- Selected crafting services

## Mira's Seed Shop

Sell:
- Seeds
- Fertilizers
- Gardening supplies

Buy:
- Accepted crops and farming supplies

## General Store

Buy and sell supported general goods.

## SHOP REQUIREMENTS

- Authoritative item prices
- Actual inventory transfers
- Buy quantity controls
- Sell quantity controls
- Transaction confirmation
- Insufficient funds handling
- Full inventory handling
- Clear Thai UI feedback

Shop interfaces must use item definitions and actual player state.

Do not hardcode prices or item lists inside UI components.

---

# PHASE 9 — CRAFTING AND PRODUCTION

Introduce a data-driven recipe system.

Each recipe must define:

- RecipeId
- Input items
- Input quantities
- Required workstation
- Processing duration
- Output items
- Unlock conditions

Initial functional recipes:

- Wooden Chest
- Wooden Chair
- Wooden Table
- Storage Cabinet
- Basic Lamp
- Compost
- Preserved Vegetables
- Simple Meal

Crafting must interact with real player inventory.

Prevent crafting with missing ingredients.

Save and reload unfinished production jobs.

Do not fabricate completed production output after restart.

---

# PHASE 10 — VISUAL DESIGN AND PROPORTIONS

Use consistent original HD Pixel Art.

The environment should prioritize:
- Architecture
- Functional furniture
- Gameplay items
- Character readability
- Clean walkable space
- Subtle shadows and lighting

Avoid excessive noninteractive props.

Establish consistent proportions between:
- Characters
- Beds
- Chairs
- Tables
- Doors
- Shelves
- Cabinets
- Workstations

Do not arbitrarily scale individual sprites.

Inspect source asset dimensions and pixel density.

Preserve crisp integer-scaled rendering.

Ensure furniture anchors, collisions and Y-sorting match the visual artwork.

Keep inventory icons visually consistent.

---

# PHASE 11 — PREMIUM USER INTERFACE

Create or refine:

- Inventory Window
- Hotbar
- Equipment Panel
- Storage Panel
- Furniture Shop
- Crafting Window
- Furniture Placement Preview
- Item Tooltips
- Quantity Selection
- Confirmation Dialogs

Requirements:

- Thai-readable typography
- Consistent panel proportions
- Responsive UI positioning
- Clear item icons
- No overlapping windows
- Keyboard and mouse interaction
- Proper close/cancel behavior
- Accessibility and text scaling

Avoid oversized panels that dominate the game scene unnecessarily.

---

# PHASE 12 — PERSISTENCE AND MULTIPLAYER SAFETY

Preserve:
- Existing farming state
- Player ownership
- Item counts
- Original crop rules
- Save Version 1 compatibility
- Building and interior persistence

Implement explicit migrations where necessary.

Prepare server-authoritative commands for:

- MoveItem
- SplitStack
- EquipItem
- PlaceFurniture
- PickupFurniture
- DepositItem
- WithdrawItem
- CraftItem
- BuyItem
- SellItem

Support command validation and replay protection in the architecture.

Full multiplayer implementation is NOT part of this milestone.

---

# PHASE 13 — AUTOMATED AND REAL GAMEPLAY TESTING

Test:

- Inventory transfers
- Stack splitting and merging
- Storage deposits and withdrawals
- Buying furniture
- Placing furniture
- Rotating furniture
- Moving furniture
- Picking up furniture
- Using a bed
- Sitting in a chair
- Opening storage
- Crafting at a workbench
- Cooking with ingredients
- Selling items
- Save/load after furniture changes
- Failed transactions
- Duplicate commands
- Full inventory
- Missing assets
- Scene transitions
- UI scaling
- Thai labels
- Frame time and resource usage

Use isolated test profiles.

Do not modify real player saves during testing.

---

# IMPLEMENTATION MILESTONES

## Milestone 1 — Item Foundation
- Audit existing code
- Unified Item Catalog
- Item transaction validation
- Existing inventory compatibility
- Automated tests

## Milestone 2 — Inventory & Storage
- Backpack
- Hotbar
- Working chest
- Quick transfer
- Save/load

## Milestone 3 — Furniture Placement
- Furniture definitions
- Placement preview
- Rotation
- Collision validation
- Pickup and relocation
- Persistence

## Milestone 4 — Interactive Furniture
- Bed
- Chair
- Wardrobe
- Lamp
- Workbench
- Cooking stove

## Milestone 5 — Shops & Economy
- Rowan furniture shop
- Mira item integration
- General selling shop
- Safe buy/sell transactions

## Milestone 6 — Crafting, Quality & Polish
- Recipes
- Production
- Quality metadata
- UI polish
- Visual regression tests

---

# DEFINITION OF DONE

A milestone is complete only when:

1. The functionality works in actual Godot gameplay.
2. The relevant UI is usable.
3. Items and currency remain correct.
4. Save/load works.
5. Error cases are handled.
6. Existing systems are not broken.
7. Automated tests pass.
8. Required original assets are present.
9. A real scene test demonstrates the feature.
10. The code is documented and committed.

Do not claim a milestone is finished merely because classes and UI screens exist.

---

# START IMPLEMENTATION

1. Verify the actual current branch and working tree.
2. Audit existing item, inventory, storage, shop, and building systems.
3. Create a concise dependency and migration plan.
4. Implement Milestone 1 completely.
5. Run regression tests.
6. Continue to Milestone 2 only if Milestone 1 passes.
7. Commit verified work incrementally.
8. Report the current state, tests, screenshots, and next steps.

Do not stop after writing documentation.

Do not implement all 360 items or 80 furniture objects at once.

Prioritize complete, enjoyable, working gameplay over large amounts of superficial content.