# SEASONS TOGETHER — CHARACTER, NPC & LIVING VILLAGE MASTER PROMPT

Repository: https://github.com/Numcha1/game
Working Branch: feature/premium-game-systems
Engine: Godot 4 .NET / C#

ROLE:
Act as a Senior Game Designer, Godot Gameplay Engineer, C# Architect, Narrative Designer, AI Behavior Programmer, and QA Lead.

## GAME DESIGN DIRECTION

Genre: Fantasy Farming + Life Simulation + Exploration
World: Connected Zones
Combat: None
Farming: Deep Simulation
Visuals: Premium HD Pixel Art
Primary Language: Thai
Initial Mode: Single-player
Future Mode: Four-player Co-op

## OBJECTIVE

Build a living NPC and village system where characters have believable daily lives, relationships, occupations, homes, and meaningful interactions with the player.

The village must feel alive rather than functioning as a static collection of shops and dialogue boxes.

## PHASE 1 — AUDIT

Inspect the current repository, GDD files, region architecture, interior implementation, world time, player interaction, SaveService, and UI.

Reuse existing systems where possible.

Do not assume the Building & Interior milestones are complete. Verify their implementation and test status first.

Record dependencies and blockers before introducing new systems.

## PHASE 2 — NPC ARCHITECTURE

Design reusable data-driven components for:

- NPC definitions and stable IDs
- NPC instances and persistent states
- Daily schedules
- Pathfinding and navigation
- Dialogue
- Relationships
- Gifts
- Shops and services
- Quest triggers
- Building entry and exit
- Seasonal behavior
- Weather reactions

Keep NPC simulation independent of rendering.

Avoid embedding all NPC logic inside a single scene controller.

## PHASE 3 — FIRST TWO CHARACTERS

Implement the first two NPCs completely.

NPC 01: Mira
Role: Seed Merchant
Personality: Friendly, knowledgeable, hardworking
Gameplay: Seed purchases, farming guidance, crop requests
Locations: Seed Shop, Village Square, Home
Schedule: Opens the shop during daytime, travels home in the evening

NPC 02: Rowan
Role: Carpenter
Personality: Practical, calm, supportive
Gameplay: Building upgrades, construction projects
Locations: Carpenter Workshop, Village, Home
Schedule: Works during daytime and follows evening routines

Both NPCs must have original character sprites, dialogue, schedules, functional interactions, and persistence.

Use Thai dialogue with an extensible localization structure.

## PHASE 4 — NPC SCHEDULE SYSTEM

Support:
- Time-based activities
- Work schedules
- Home routines
- Walking between destinations
- Indoor/outdoor transitions
- Weather-based variations
- Festival overrides
- Quest overrides
- Schedule recovery after loading

NPCs must not teleport visibly between nearby waypoints without a justified scene transition.

For NPCs in unloaded regions, use lightweight schedule simulation and restore their correct state when the region loads.

## PHASE 5 — DIALOGUE

Implement:
- Dialogue boxes
- Speaker portraits
- Thai text
- Multiple dialogue choices
- Conditional dialogue
- Relationship-dependent dialogue
- Seasonal dialogue
- Quest dialogue
- Dialogue history where appropriate

Important dialogue must remain readable and not block progression because of missing text.

## PHASE 6 — RELATIONSHIP SYSTEM

Support relationship levels from 0–10 hearts.

Players gain friendship through:
- Conversations
- Gifts
- Quests
- Special events

Prevent repeated interaction exploits.

Relationships should unlock meaningful dialogue, recipes, services, and character stories.

Do not implement romance or marriage yet.

## PHASE 7 — NPC HOMES

Integrate with the planned Building & Interior System.

NPCs should:
- Enter their houses
- Leave according to their schedules
- Respect building access rules
- Appear at the correct indoor positions
- Maintain their schedules after save/load

Do not create decorative doors that cannot be interacted with.

If the reusable interior system is not ready, document the dependency and build only what can be safely integrated.

## PHASE 8 — FULL NPC ROSTER

Prepare data definitions for these 12 characters:

1. Mira — Seed Merchant
2. Rowan — Carpenter
3. Theo — Blacksmith
4. Luna — Herbalist
5. Finn — Fisher
6. Hazel — Forest Guardian
7. Clara — Baker
8. Elias — Village Steward
9. Iris — Librarian
10. Bram — Miner
11. Selene — Seasonal Researcher
12. Nia — Animal Keeper

Do not claim all 12 NPCs are completed until each has functional schedules, interactions, visuals, and required quest content.

## PHASE 9 — TESTING

Verify:
- NPC pathfinding
- Schedule transitions
- Indoor/outdoor movement
- Dialogue choices
- Shop interactions
- Relationships
- Save/load
- Day changes
- Weather variations
- Missing scene handling
- No duplicate gifts or rewards
- No softlocks
- Consistent UI feedback

## ACCEPTANCE CRITERIA

The initial milestone is complete when:

1. Mira and Rowan exist in the playable village.
2. Both visibly walk and follow daily routines.
3. Players can speak with both.
4. Mira sells seeds using authoritative economy rules.
5. Rowan offers building-related services.
6. Both enter and exit buildings correctly.
7. Both NPCs persist correctly after save/load.
8. Thai dialogue displays without clipping.
9. NPC behavior remains stable through day transitions.
10. Automated and actual Godot scene tests pass.

## IMPLEMENTATION ORDER

Milestone 1: NPC Framework
Milestone 2: Mira and Rowan
Milestone 3: Dialogue and relationships
Milestone 4: Village activity and schedules
Milestone 5: Expand remaining NPCs
Milestone 6: Quests, polish, and verification

Start by auditing the existing branch.

Implement only Milestones 1 and 2 in this run.

Preserve existing farming, save compatibility, original assets, and multiplayer authority boundaries.

After implementation, report:
- Files changed
- Gameplay functionality
- Tests performed
- Screenshots captured
- Remaining limitations
- Next recommended milestone

Commit verified changes incrementally.