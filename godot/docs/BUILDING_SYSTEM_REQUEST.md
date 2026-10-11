# SEASONS TOGETHER — BUILDING & INTERIOR SYSTEM MASTER PROMPT

## ROLE
Act as a Senior Godot 4 .NET Game Developer, C# Software Architect, Level Designer, Interior Designer, Pixel Artist, Gameplay Engineer, and QA Lead.

Your task is to design and implement a production-quality Building & Interior System for Seasons Together.

## PROJECT
Repository: https://github.com/Numcha1/game
Working Branch: feature/premium-game-systems
Engine: Godot 4 .NET
Language: C#
Visual Style: Premium HD Pixel Art

## LOCKED GAME DESIGN
Genre: Fantasy Farming + Life Simulation + Exploration
World: Connected Zones
Simulation Depth: Advanced
Combat: None
Primary Mode: Single-player
Future Mode: Four-player cooperative multiplayer

Follow the previously approved GDD v1.1, World Design v1.2, Farming & Economy Design v1.3, and Interior Design v1.2.1 where those documents exist.

Do not claim that a design document is already present in the repository unless you have verified it.

## MAIN OBJECTIVE

Implement a complete and reusable system allowing players to enter, explore, interact with, decorate, upgrade, and exit buildings.

Important buildings must have functional interiors, not decorative facades or noninteractive mockups.

Prioritize a polished player experience, safe persistence, and future multiplayer compatibility.

---

# PHASE 1 — EXISTING PROJECT AUDIT

Inspect:
- Existing Godot scenes
- FarmScene and scene lifecycle
- Player movement and collision
- World state management
- GameSession
- SaveService
- UI and input architecture
- Existing world navigation
- Asset pipeline
- Automated tests

Identify reusable systems before implementing new ones.

Document relevant architectural risks and missing functionality.

Preserve existing gameplay and saves.

# PHASE 2 — BUILDING ARCHITECTURE

Create a reusable building system with clearly defined responsibilities.

Suggested components:
- BuildingDefinition
- BuildingRegistry
- BuildingInstance
- DoorInteraction
- InteriorTransitionService
- InteriorState
- FurniturePlacementService

Use these names only where they improve the existing architecture.

Each building should have:
- Stable Building ID
- Building type
- Exterior region ID
- Exterior entrance position
- Interior scene reference
- Interior spawn marker
- Exterior return marker
- Entry requirements
- Access permissions
- Upgrade level
- Persistent state

Avoid creating a separate hardcoded entrance implementation for every building.

# PHASE 3 — ENTERING AND EXITING BUILDINGS

Implement a complete interaction flow:

1. Player approaches a building door.
2. An interaction prompt appears.
3. Player presses the interaction key.
4. Validate entry requirements.
5. Play the door interaction animation.
6. Perform a smooth screen transition.
7. Load the building interior.
8. Spawn the player at the correct entrance.
9. Restore the interior's persistent state.
10. Allow the player to interact normally.
11. Exit through the appropriate door.
12. Return to the corresponding exterior entrance.

Required safeguards:
- Prevent duplicate transition requests.
- Prevent invalid spawn positions.
- Prevent player movement during transitions.
- Handle missing scenes and loading failures.
- Prevent transition loops.
- Preserve world time.
- Preserve inventory and player state.
- Keep save/load consistent.
- Prevent players from becoming trapped inside buildings.

# PHASE 4 — PLAYER FARMHOUSE

Create an original, polished farmhouse interior.

Initial rooms:
- Bedroom
- Living Area
- Basic Storage

Functional objects:
- Bed
- Storage chest
- Lights
- Decorations
- Interaction points

The player must be able to:
- Enter the farmhouse.
- Walk through the interior.
- Interact with furniture.
- Sleep.
- Access storage.
- Exit and return to the farm.

Use original premium pixel-art assets.

Avoid unfinished placeholder rooms in the final accepted result.

# PHASE 5 — HOUSE UPGRADES

Support three farmhouse levels:

Level 1 — Studio Farmhouse
- Bedroom
- Basic storage

Level 2 — Cozy Cottage
- Larger living space
- Kitchen
- Additional furniture capacity

Level 3 — Fantasy Manor
- Additional rooms
- Expanded customization
- Magical study area

Upgrade requirements must be defined through data.

Ensure existing furniture is preserved when upgrading.

Use migration or stable object placement rules to prevent furniture loss.

# PHASE 6 — FURNITURE AND DECORATION

Implement:
- Furniture inventory
- Placement preview
- Grid snapping
- Collision validation
- Furniture rotation where supported
- Pickup and relocation
- Wall and floor customization
- Lighting objects
- Storage furniture
- Save/load for furniture state

Prevent:
- Furniture overlapping blocked locations
- Blocking all exits
- Item duplication
- Lost furniture after upgrades
- Invalid placements outside room boundaries

Support future expansion without modifying every interior scene.

# PHASE 7 — NPC HOMES AND SHOPS

Prepare reusable interiors for:
- NPC houses
- Seed shop
- General store
- Blacksmith
- Carpenter workshop
- Library
- Community hall
- Magical research building

Each important interior should have a functional purpose.

NPC houses should reflect individual character personalities.

Shops should allow conversations, shopping, and relevant interactions.

Support opening hours and locked-door explanations.

Do not generate all buildings simultaneously before proving the system with a small playable slice.

# PHASE 8 — INDOOR ENVIRONMENT

Create consistent indoor presentation:

- Indoor lighting
- Day/night window appearance
- Appropriate shadows
- Interior ambience
- Door sound effects
- Smooth transitions
- Pixel-perfect rendering
- Thai-readable UI prompts
- Correct Y-sorting
- Stable camera behavior

Outdoor weather must not visually pass through roofs.

Indoor ambience may react to outdoor weather.

Maintain the game's existing Premium HD Pixel Art direction.

# PHASE 9 — PERSISTENCE

All interior changes must survive:
- Manual save
- Autosave
- Game restart
- Building upgrades
- Scene reload
- Character re-entry

Store:
- Building level
- Interior object IDs
- Furniture placement
- Furniture orientation
- Storage contents
- Unlocked rooms
- Relevant persistent interactions

Preserve compatibility with existing Save Version 1.

Introduce explicit migrations if additional persistent fields are required.

Do not overwrite real player saves during testing.

# PHASE 10 — FUTURE MULTIPLAYER COMPATIBILITY

Design the architecture to support:
- Multiple players inside the same building
- Players in different regions
- Shared furniture changes
- Shared building upgrades
- Server-validated storage transactions
- Consistent interior state synchronization
- Authoritative interaction commands

Do not implement full multiplayer as part of this milestone.

Document future network commands and ownership rules.

# PHASE 11 — TESTING

Add automated and playable tests for:

- Entering and exiting buildings
- Correct spawn positions
- Repeated door interactions
- Missing interior resources
- Save/load inside buildings
- Furniture placement
- Furniture relocation
- Furniture persistence
- Building upgrades
- Interior collision
- Locked buildings
- Scene transitions
- Invalid interaction requests
- Regression compatibility

Run actual Godot scene tests and capture genuine screenshots.

Do not fabricate test results.

# IMPLEMENTATION PRIORITY

Milestone 1:
Reusable building definitions, door interactions, and scene transitions.

Milestone 2:
Playable farmhouse interior with a bed and storage.

Milestone 3:
Furniture placement and persistent interior state.

Milestone 4:
House upgrades and migration safety.

Milestone 5:
One complete shop interior and reusable NPC house framework.

Milestone 6:
Final visual polish, testing, documentation, and acceptance.

Complete one milestone before expanding into the next.

# STRICT REQUIREMENTS

- Do not recreate the entire game.
- Do not switch engines.
- Do not remove existing farming functionality.
- Do not modify native/ or dist/ unnecessarily.
- Do not break existing saves.
- Do not introduce unnecessary paid dependencies.
- Do not use copyrighted artwork without permission.
- Keep code modular and maintainable.
- Avoid unnecessary singleton managers.
- Implement real interactive gameplay, not mockups.
- Keep the Godot project buildable.
- Preserve Thai language support.

# ACCEPTANCE CRITERIA

The feature is complete only when:

1. The player can enter and exit the farmhouse.
2. Exterior and interior positions remain correct.
3. Sleeping works inside the farmhouse.
4. Storage is functional.
5. Furniture can be placed, moved, and saved.
6. House upgrades preserve existing items.
7. At least one shop interior is fully playable.
8. Building transitions do not corrupt saves.
9. Relevant automated tests pass.
10. Real Godot gameplay verification succeeds.

# START NOW

Inspect the current repository and determine what already exists.

Create a new feature branch if appropriate.

Implement Milestone 1 first, then Milestone 2.

After each milestone:
- Build the project.
- Run regression tests.
- Perform actual scene verification.
- Report changed files.
- Report incomplete features.
- Commit verified changes.

Do not stop at writing documentation.

Begin implementing the reusable building entrance and interior transition system, followed by the first playable farmhouse interior.