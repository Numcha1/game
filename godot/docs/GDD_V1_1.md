# SEASONS TOGETHER
## Game Design Document — Version 1.1

**Project Status:** Pre-production / Design Specification  
**Engine:** Godot 4 .NET / C#  
**Genre:** Fantasy Farming, Life Simulation & Exploration  
**Visual Style:** Premium HD Pixel Art  
**Platform:** Windows PC  
**Modes:** Single-player first, cooperative multiplayer up to 4 players later  
**Language:** Thai primary, English localization planned

---

# 1. GAME IDENTITY

## 1.1 High Concept

Seasons Together is a fantasy farming and life simulation RPG set in Aetherfall Valley, a magical region whose natural seasons are gradually falling out of balance.

Players manage a living farm, study soils and crops, develop sustainable production, build relationships, explore mysterious regions, and restore the valley.

The world reacts visibly to player progression. Successful restoration changes environments, unlocks routes, introduces new resources, and improves village life.

## 1.2 Locked Design Pillars

**Pillar A — Deep Agricultural Simulation**

Farming involves interacting systems such as moisture, nutrients, soil health, disease, seasonality, production costs, and environmental effects.

**Pillar B — Peaceful Fantasy Exploration**

Exploration emphasizes discovery, collection, environmental puzzles, magical ecology, and restoration. No combat mechanics are included.

**Pillar C — Living Community**

NPCs have daily routines, personal histories, relationships, occupations, and roles in the valley's development.

**Pillar D — Connected World**

The world consists of multiple authored, connected zones with persistent world states.

**Pillar E — Meaningful Progression**

Upgrades, discoveries, relationships, and restoration projects create tangible changes in gameplay and the world.

## 1.3 Player Experience Goals

The player should experience:

- Freedom to choose daily activities.
- Satisfaction from building a productive farm.
- Curiosity when discovering new locations.
- Meaningful interactions with townspeople.
- Long-term goals without excessive punishment.
- Clear feedback from every important action.
- A peaceful atmosphere with strategic depth.

**No combat, hostile enemy AI, weapon damage, death penalties, or mandatory survival meters.**

---

# 2. WORLD AND REGION DESIGN

## 2.1 World Structure

The game uses connected handcrafted zones.

Each region has:

- A unique Region ID.
- A dedicated Godot scene.
- Entrance and exit markers.
- Walkable and blocked areas.
- Interactable objects.
- Persistent resource states.
- Seasonal appearance variants.
- Local environmental audio.
- A regional discovery record.
- Explicit world-state synchronization boundaries.

## 2.2 Region 01 — Moonroot Farm

**Role:** Player's home and central progression area.

### Main Features
- Farmhouse
- Crop fields
- Water source
- Storage
- Animal buildings
- Production facilities
- Greenhouse
- Decorative placement
- Shipping area
- Farm expansion zones

### Progression

Start with the existing 24 plots.

Proposed farm expansion milestones:
- Starter: 24 plots
- Expansion I: up to 48 plots
- Expansion II: up to 96 plots

Expansions should use placeable plot areas rather than requiring the entire farm to become a rigid rectangular grid.

**Acceptance:** The player can expand, rearrange, save, and reload the farm without losing objects or creating overlapping structures.

## 2.3 Region 02 — Lunavale Village

**Role:** Community, commerce, quests, relationships, and restoration.

### Locations
1. Seed Shop
2. General Store
3. Blacksmith
4. Carpenter Workshop
5. Herbalist
6. Bakery
7. Community Hall
8. Library
9. Town Square
10. Residential Area

### World Changes
- Buildings can be repaired.
- Businesses gain new inventory.
- Community events become available.
- NPC routines change after restoration projects.
- Visual decorations evolve with the seasons.

## 2.4 Region 03 — Whisperwood Forest

**Role:** Gathering, exploration, magical ecology.

### Resources
- Wood
- Mushrooms
- Wild berries
- Medicinal herbs
- Wild seeds
- Rare flowers
- Magical plant materials

### Mechanics
- Regenerating resource nodes
- Seasonal resource tables
- Hidden clearings
- Tree and plant restoration
- Environmental puzzles
- Rare discoveries

## 2.5 Region 04 — Crystal Highlands

**Role:** Mining and tool progression without combat.

### Resources
- Stone
- Copper
- Iron
- Silver
- Magical crystals
- Rare minerals

### Mechanics
- Mining nodes
- Resource quality
- Tool requirements
- Crystal resonance puzzles
- Collapsed passages
- Regional unlocking

Mining difficulty comes from energy, tools, environmental hazards, and puzzle requirements rather than enemies.

## 2.6 Region 05 — Moonlake

**Role:** Fishing, water resources, and relaxation.

### Features
- Fishing locations
- Seasonal fish
- Weather-dependent catches
- Wetland gathering
- Boat dock
- Water restoration activities

Fishing should include an accessible skill-based minigame with adjustable assistance.

## 2.7 Region 06 — Seasonal Grove

**Role:** Magical ecology and seasonal discovery.

Its environmental configuration changes with each season.

Examples:
- Spring: blooming magical flowers
- Summer: luminous insects and water spirits
- Autumn: rare mushrooms and drifting leaves
- Winter: frost crystals and dormant plant spirits

The area is unlocked through restoration progression.

## 2.8 Region 07 — Ancient Ruins

**Role:** Main-story culmination and environmental puzzles.

### Features
- Ancient seasonal machinery
- Light and water puzzles
- Magical mechanisms
- Lore discoveries
- Restoration milestones

The ruins contain no combat encounters.

---

# 3. PLAYER CHARACTER SYSTEM

## 3.1 Character Creation

Players can customize:
- Name
- Skin tone
- Hair style
- Hair color
- Eye appearance
- Clothing
- Accessories

Character customization does not affect gameplay attributes.

## 3.2 Character Attributes

Core attributes:
- Energy
- Farming skill
- Gathering skill
- Mining skill
- Ranching skill
- Crafting skill

Secondary progression:
- Friendship
- Exploration discoveries
- Collection progress
- Restoration contribution

Avoid unnecessary RPG statistics that do not affect meaningful decisions.

## 3.3 Movement and Interaction

Supported inputs:
- WASD / arrow movement
- Mouse interactions
- Tool hotbar
- Contextual actions
- Inventory
- Journal
- Map
- Pause/settings

Movement should feel responsive and support consistent collision and interaction distances.

## 3.4 Tool Upgrade Tiers

Proposed tiers:
1. Basic
2. Copper
3. Iron
4. Gold
5. Masterwork

Upgrades may improve:
- Energy cost
- Action area
- Tool capability
- Resource efficiency

No combat statistics.

---

# 4. ADVANCED FARMING SIMULATION

## 4.1 Core Crop System

Preserve the current eight-crop system as the authoritative baseline.

Expand the content target to **24 crop species** for version 1.0.

Do not overwrite original crop IDs or silently change legacy balancing.

## 4.2 Crop Data Definition

Each crop definition should support:

- Crop ID
- Display name
- Category
- Allowed seasons
- Seed cost
- Growth duration
- Growth stages
- Water demand
- Nutrient demand
- Preferred soil conditions
- Disease susceptibility
- Temperature tolerance
- Base yield
- Quality modifiers
- Base selling value
- Processing outputs
- Regrowth capability, if applicable

Data definitions must be separate from runtime crop state.

## 4.3 Soil Simulation

Every farm plot should track:

- Moisture
- Fertility
- Soil health
- Nutrient balance
- Soil type
- Crop rotation history
- Disease risk

Proposed nutrient model:
- Nitrogen (N)
- Phosphorus (P)
- Potassium (K)

These systems must produce understandable player-facing decisions.

The player needs access to a Soil Inspector displaying current values, explanations, and recommended corrective actions.

## 4.4 Soil Types

Initial proposed types:
1. Balanced Soil
2. Sandy Soil
3. Clay Soil
4. Rich Loam
5. Magical Soil

Each type has different moisture retention, nutrient characteristics, and suitable crops.

## 4.5 Crop Growth

Growth depends on:
- Correct season
- Soil moisture
- Soil fertility
- Nutrient availability
- Environmental conditions
- Crop health

Growth should remain deterministic when given the same authoritative state and random seed.

## 4.6 Crop Quality

Proposed tiers:
- Common
- Good
- Excellent
- Exceptional

Quality affects sale price, crafting outputs, and selected quest requirements.

Avoid making low-quality produce worthless.

## 4.7 Irrigation

Supported upgrades:
- Watering Can
- Basic Sprinkler
- Improved Sprinkler
- Advanced Irrigation
- Magical Watering Device

Water sources, coverage, operational costs, and upgrade requirements must be understandable.

## 4.8 Farming Automation

Automation may reduce repetitive tasks but should not eliminate meaningful player decision-making.

Candidates:
- Automatic watering
- Crop storage transfer
- Processing queues
- Animal feeding systems

Automation requires appropriate progression and resource costs.

---

# 5. MAGIC AND ECOLOGY

## 5.1 Design Principle

Magic supports farming, exploration, and environmental restoration.

It must not become a combat system.

## 5.2 Magic Disciplines

**Verdant Magic:** Plant growth and soil restoration.

**Tide Magic:** Water flow, irrigation, and wetland restoration.

**Crystal Magic:** Mining, energy storage, and environmental mechanisms.

**Seasonal Magic:** Interaction with season-specific puzzles and ancient structures.

## 5.3 Resource and Balance

Magical abilities consume limited resources, require infrastructure, or have cooldowns.

Magic must not make normal farming systems irrelevant.

Example:

A soil restoration ritual may recover degraded soil over several days, while normal fertilizer provides a smaller immediate benefit.

## 5.4 Magical Creatures

Magical creatures are friendly or neutral.

Possible examples:
- Forest spirits
- Water sprites
- Seasonal wisps
- Crystal creatures

Their roles include environmental feedback, exploration discoveries, and restoration activities.

They are not enemies.

---

# 6. ANIMAL AND RANCHING SYSTEM

## 6.1 Initial Animal Types

1. Chicken
2. Cow
3. Sheep
4. Duck

Fantasy animal variants may be introduced after basic husbandry is balanced.

## 6.2 Animal Attributes

- Hunger
- Happiness
- Health
- Affection
- Product quality
- Daily routine

## 6.3 Production

Animals produce:
- Eggs
- Milk
- Wool
- Duck eggs

Product quality depends on care and conditions.

## 6.4 Buildings

- Chicken Coop
- Barn
- Feed Storage
- Animal Care Station

Animal care should be meaningful without creating excessive daily maintenance.

---

# 7. INVENTORY, CRAFTING, AND PRODUCTION

## 7.1 Inventory System

Required features:
- Stable item IDs
- Item stacks
- Item categories
- Item descriptions
- Inventory capacity
- Storage chests
- Item transfer
- Sorting and filtering
- Quantity selection
- Transaction validation

## 7.2 Item Categories

1. Seeds
2. Crops
3. Animal Products
4. Materials
5. Minerals
6. Fish
7. Consumables
8. Equipment
9. Crafted Products
10. Quest Items
11. Decorations

## 7.3 Crafting

Each recipe specifies:
- Recipe ID
- Required ingredients
- Required station
- Processing time
- Output
- Unlock requirements

## 7.4 Production Stations

Proposed initial station categories:
- Furnace
- Workbench
- Preserves Maker
- Cooking Station
- Seed Processor
- Loom
- Crystal Workbench
- Compost Station

Production should have input storage, progress display, output collection, and persistent state.

---

# 8. ECONOMY DESIGN

## 8.1 Currency

Use one primary currency initially.

Avoid unnecessary premium currencies.

## 8.2 Economic Activities

Players earn money through:
- Farming
- Animal products
- Fishing
- Gathering
- Crafting
- Processing
- Delivery contracts
- Community requests

## 8.3 Pricing

Prices are affected by:
- Base item value
- Quality
- Seasonal demand
- Selected market conditions
- Processing value

Maintain understandable pricing.

The player should be able to inspect expected revenue before confirming major transactions.

## 8.4 Economy Balancing Goals

The economy should support:
- Viable early-game activities
- Multiple profitable strategies
- Meaningful upgrades
- Avoidance of dominant infinite-profit loops
- Fair progression for players who prefer different activities

Use automated simulations and playtests to set the final numbers.

---

# 9. NPC AND RELATIONSHIP DESIGN

## 9.1 NPC System

NPCs require:
- Persistent ID
- Name
- Occupation
- Personality
- Home location
- Daily schedule
- Weekly variations
- Weather reactions
- Dialogue data
- Relationship state
- Quest availability
- Special events

## 9.2 NPC Roster — Version 1.0 Target

Target **12 named NPCs**:

| NPC | Role | Gameplay Function |
|---|---|---|
| Mira | Seed Merchant | Seeds and crop requests |
| Rowan | Carpenter | Buildings and farm expansion |
| Theo | Blacksmith | Tool upgrades |
| Luna | Herbalist | Soil research and magical plants |
| Finn | Fisher | Fishing and lake activities |
| Hazel | Forest Guardian | Forestry and restoration |
| Clara | Baker | Cooking and delivery contracts |
| Elias | Village Steward | Main restoration progression |
| Iris | Librarian | Lore and discoveries |
| Bram | Miner | Minerals and crystal puzzles |
| Selene | Seasonal Researcher | Seasonal Grove access |
| Nia | Animal Keeper | Ranching and animal care |

All names are provisional original-design placeholders.

## 9.3 Relationship Levels

Use a 0–10 heart scale.

Relationships improve through:
- Conversations
- Gifts
- Quests
- Shared events
- Story milestones

Higher levels unlock personal stories, recipes, activities, and cosmetic rewards.

Romance and marriage are outside the initial version 1.0 commitment unless separately approved.

---

# 10. QUEST AND MAIN STORY SYSTEM

## 10.1 Story Structure

### Act I — A New Beginning

The player arrives, repairs the initial farm, meets villagers, and learns about the seasonal imbalance.

### Act II — Roots of the Valley

The player restores the forest, develops farming techniques, and helps repair village infrastructure.

### Act III — The Hidden Currents

The lake, crystal regions, and seasonal mechanisms reveal how the valley functions.

### Act IV — The Seasons Reunited

The player restores the ancient seasonal network and completes the main valley restoration.

The game continues after the main story.

## 10.2 Quest Types

- Main Story Quests
- NPC Relationship Quests
- Daily Requests
- Farming Contracts
- Exploration Quests
- Restoration Projects
- Seasonal Events

## 10.3 Quest Design Requirements

Quests must support:
- Conditions
- Progress tracking
- Objectives
- Rewards
- Persistent completion state
- Localization
- Clear failure handling
- Non-repeatable reward protection

Avoid mandatory quests that permanently block players because an item was discarded.

---

# 11. TIME, SEASONS, AND WEATHER

## 11.1 Calendar

Retain:
- Four seasons
- Fourteen days per season

The existing time rules remain the baseline until a tested change is approved.

## 11.2 Weather Types

Target:
1. Clear
2. Cloudy
3. Rain
4. Storm
5. Snow
6. Fog
7. Heatwave
8. Magical Weather

Weather influences farming, gathering, fishing, NPC activities, and environmental visuals.

## 11.3 Day Structure

Players can farm, gather, explore, socialize, craft, or decorate.

There must be no mandatory activity schedule.

## 11.4 Difficulty Approach

The game is simulation-heavy, but information must remain accessible.

Players should face meaningful consequences for poor farming decisions while retaining realistic recovery options.

No permanent softlocks from crop failure or insufficient money.

---

# 12. USER INTERFACE AND ACCESSIBILITY

## 12.1 Screens

Required:
- Main Menu
- Character Creation
- Farm HUD
- Inventory
- Map
- Journal
- Quest Log
- Crop Inspector
- Soil Inspector
- Shop
- Crafting
- Skill Screen
- NPC Relationship Menu
- Calendar
- Settings
- Save Management

## 12.2 Visual Guidelines

- Crisp pixel-art world
- High-resolution readable Thai fonts
- Consistent iconography
- Stable UI scaling
- Clear feedback
- Controlled use of effects
- Consistent animation timing

## 12.3 Accessibility

- Adjustable UI scale
- Adjustable text scale
- Reduced motion
- Configurable audio levels
- Keyboard remapping
- Fishing assistance
- Color-independent interaction indicators

---

# 13. MULTIPLAYER DESIGN

Version 1.0 primarily targets a complete single-player experience.

Multiplayer is an architectural requirement and a separately tested release feature.

Target:
- Up to four players
- Shared farm and world progression
- Individual inventories
- Shared restoration projects
- Authoritative gameplay commands
- Reliable reconnection
- Save consistency

All new gameplay systems must define their authority and replication rules before implementation.

No requirement for combat, PvP, or competitive progression.

---

# 14. VERSION 1.0 CONTENT TARGETS

| Content Category | Target |
|---|---:|
| Major World Regions | 7 |
| Named NPCs | 12 |
| Crop Species | 24 total |
| Soil Types | 5 |
| Animal Species | 4 |
| Fishing Species | 16 |
| Gathering Resources | 24 |
| Mineral / Crystal Types | 10 |
| Crafting Recipes | 40 |
| Production Station Types | 8 |
| Tool Upgrade Tiers | 5 |
| Skill Disciplines | 5 |
| Main Story Quests | 20 |
| Side / Relationship Quests | 36 |
| Daily Request Templates | 30 |
| Seasonal Festivals | 4 |
| Restoration Projects | 12 |
| Major Environmental Puzzle Designs | 8 |

These are planned content targets, not existing implementation counts.

Content quality is more important than reaching a numerical quota.

Repeated reskins or filler quests do not count as meaningful unique content.

---

# 15. MILESTONES AND DELIVERY

## Milestone A — Core Farming Complete

- Existing farming rules stable
- Inventory and shop usable
- Save/load reliable
- Player onboarding complete
- First-time gameplay acceptance passed

## Milestone B — Advanced Farming

- Soil simulation expanded
- Crop qualities and new crops
- Tool upgrades
- Irrigation
- Farm expansion
- Balanced progression

## Milestone C — Village and Relationships

- Fully navigable village
- Shops and schedules
- NPC interactions
- Relationship progression
- Initial story quests

## Milestone D — Exploration Foundation

- Forest region
- Gathering
- Zone transitions
- Resource persistence
- Map discovery
- First restoration project

## Milestone E — Extended Simulation

- Animal care
- Production and crafting
- Fishing
- Mining
- Magical ecology

## Milestone F — Story and World Completion

- Remaining planned zones
- Main story
- Seasonal events
- Restoration milestones
- Endgame loop

## Milestone G — Multiplayer Completion

- Four-player integration
- Shared world validation
- Reconnection
- Internet playtests
- Network safety

## Milestone H — Release Candidate

- Artwork and audio finalized
- Performance acceptance
- Save migration tests
- Gameplay balance
- Windows export
- Clean machine QA

Every milestone must produce a playable, testable build.

---

# 16. ACCEPTANCE CRITERIA

## 16.1 Core Gameplay

- A new player can complete planting, crop growth, harvesting, selling, and sleeping without developer intervention.
- All farming actions produce correct feedback and state changes.
- The player cannot duplicate money or items through interrupted actions.
- Progress is preserved after save/load.
- Existing save files remain supported or are safely migrated.

## 16.2 Farming Simulation

- Weather, moisture, fertility, and crop rotation affect growth consistently.
- Soil values are visible and explainable to players.
- Crop growth tests are deterministic where required.
- The game contains multiple viable planting strategies.
- Farming progression avoids unwinnable economic states.

## 16.3 Economy

- Buying and selling preserve resource conservation.
- Insufficient money blocks purchases.
- Transactions cannot execute twice accidentally.
- Processing outputs match recipe definitions.
- Automated balance simulations reveal no repeatable infinite-profit exploit.

## 16.4 World

- Every region has functional entrances and exits.
- Persistent resource nodes restore correctly after loading.
- Discovery and restoration changes persist.
- Players cannot become trapped in inaccessible locations.
- Every released region contains a meaningful complete gameplay loop.

## 16.5 NPCs and Quests

- NPC schedules work across day transitions.
- Relationship progress persists.
- Quest rewards cannot be claimed repeatedly unless designed as repeatable.
- Main-story progression cannot become permanently blocked by ordinary player actions.

## 16.6 Visual and Audio Quality

- No known critical missing textures.
- Thai text displays correctly.
- UI is functional at target resolutions.
- Important actions have visual and audio feedback.
- Art direction is consistent across released regions.

## 16.7 Performance

Define supported hardware configurations and measured workloads.

Initial target:
- 60 FPS cap
- Mean frame time at or below 17 ms
- P95 frame time at or below 18 ms

Validate under representative gameplay, including dense farming areas, active weather effects, NPCs, and scene transitions.

Performance results must come from actual measurements.

## 16.8 Reliability

- Automated tests pass.
- Save recovery functions correctly.
- No known release-blocking crash.
- Extended gameplay tests complete.
- Exported Windows build runs without the Godot Editor.

## 16.9 Multiplayer

For any multiplayer release:
- Four independent players can join.
- Shared world changes remain consistent.
- Concurrent transactions do not duplicate resources.
- Disconnect/reconnect works.
- Host shutdown and interrupted saving are handled explicitly.
- Internet testing uses separate real machines or networks.

## 16.10 Playtesting

Before release, conduct:
- First-time user tests
- Experienced farming game player tests
- Extended simulation sessions
- Accessibility and Thai-language reviews

Recommended initial acceptance study:
- At least 5 new players for onboarding evaluation
- At least 3 extended gameplay sessions of 2 hours or more
- Documented critical bug triage and correction
- A final release-candidate test pass

These are minimum proposed gates, not a guarantee of commercial quality.

---

# 17. DESIGN AND TECHNICAL SAFETY

The current project contains a working native simulation and a developing Godot implementation.

Rules:

1. Do not discard existing player saves.
2. Do not duplicate authoritative farming calculations.
3. Keep simulation independent of Godot scene rendering.
4. Use stable IDs for crops, items, NPCs, and persistent world objects.
5. Version persistent data changes explicitly.
6. Design new mechanics for future authoritative multiplayer support.
7. Keep development builds playable.
8. Implement one complete system at a time.
9. Do not use copyrighted assets from reference games.
10. Verify features before declaring them complete.

---

# 18. DEFINITION OF DONE

A gameplay system is complete only when:

- Game rules are implemented.
- Player input works.
- UI communicates the state.
- Required artwork and animations exist.
- Required sound feedback exists.
- Save/load preserves the state.
- Errors are handled.
- Automated tests cover key rules.
- The feature works within the complete gameplay loop.
- Multiplayer authority is specified, even if online integration is deferred.
- Human playtesting confirms that the feature is understandable and enjoyable.

**Final design objective:** A rich, peaceful fantasy farming simulation where every system contributes to the player's relationship with the farm, the community, and the living world.
