# NPC Milestones 1-2: changed files

Baseline `53865b1`; foundation `53edd77`; playable village `b4d1d10`. Documentation/evidence are recorded in the following separate commit.

## Source, scenes and assets

| Status | File |
| --- | --- |
| A | [godot/Assets/Village/asset-prompts.json](../../godot/Assets/Village/asset-prompts.json) |
| A | [godot/Assets/Village/npc-atlas.png](../../godot/Assets/Village/npc-atlas.png) |
| A | [godot/Assets/Village/npc-atlas.png.import](../../godot/Assets/Village/npc-atlas.png.import) |
| A | [godot/Assets/Village/rooms-atlas.png](../../godot/Assets/Village/rooms-atlas.png) |
| A | [godot/Assets/Village/rooms-atlas.png.import](../../godot/Assets/Village/rooms-atlas.png.import) |
| A | [godot/Assets/Village/village.png](../../godot/Assets/Village/village.png) |
| A | [godot/Assets/Village/village.png.import](../../godot/Assets/Village/village.png.import) |
| M | [godot/Core/Application/GameSession.Buildings.cs](../../godot/Core/Application/GameSession.Buildings.cs) |
| A | [godot/Core/Application/GameSession.Npcs.cs](../../godot/Core/Application/GameSession.Npcs.cs) |
| A | [godot/Core/Application/GameSession.Npcs.cs.uid](../../godot/Core/Application/GameSession.Npcs.cs.uid) |
| M | [godot/Core/Application/GameSession.cs](../../godot/Core/Application/GameSession.cs) |
| M | [godot/Core/Application/Services.cs](../../godot/Core/Application/Services.cs) |
| M | [godot/Core/Domain/Buildings.cs](../../godot/Core/Domain/Buildings.cs) |
| A | [godot/Core/Domain/Npcs.cs](../../godot/Core/Domain/Npcs.cs) |
| A | [godot/Core/Domain/Npcs.cs.uid](../../godot/Core/Domain/Npcs.cs.uid) |
| M | [godot/Core/Infrastructure/Persistence/BuildingSaveDocument.cs](../../godot/Core/Infrastructure/Persistence/BuildingSaveDocument.cs) |
| A | [godot/Diagnostics/FarmScene.NpcChecks.cs](../../godot/Diagnostics/FarmScene.NpcChecks.cs) |
| A | [godot/Diagnostics/FarmScene.NpcChecks.cs.uid](../../godot/Diagnostics/FarmScene.NpcChecks.cs.uid) |
| A | [godot/Infrastructure/Assets/NpcAssets.cs](../../godot/Infrastructure/Assets/NpcAssets.cs) |
| A | [godot/Infrastructure/Assets/NpcAssets.cs.uid](../../godot/Infrastructure/Assets/NpcAssets.cs.uid) |
| M | [godot/Presentation/Audio/FarmAudio.cs](../../godot/Presentation/Audio/FarmAudio.cs) |
| M | [godot/Presentation/Buildings/InteriorRoom.cs](../../godot/Presentation/Buildings/InteriorRoom.cs) |
| M | [godot/Presentation/Effects/WeatherAccentPool.cs](../../godot/Presentation/Effects/WeatherAccentPool.cs) |
| A | [godot/Presentation/UI/FarmHud.Npcs.cs](../../godot/Presentation/UI/FarmHud.Npcs.cs) |
| A | [godot/Presentation/UI/FarmHud.Npcs.cs.uid](../../godot/Presentation/UI/FarmHud.Npcs.cs.uid) |
| M | [godot/Presentation/UI/FarmHud.cs](../../godot/Presentation/UI/FarmHud.cs) |
| A | [godot/Presentation/Village/NpcActor.cs](../../godot/Presentation/Village/NpcActor.cs) |
| A | [godot/Presentation/Village/NpcActor.cs.uid](../../godot/Presentation/Village/NpcActor.cs.uid) |
| A | [godot/Presentation/Village/VillageRoom.cs](../../godot/Presentation/Village/VillageRoom.cs) |
| A | [godot/Presentation/Village/VillageRoom.cs.uid](../../godot/Presentation/Village/VillageRoom.cs.uid) |
| M | [godot/Presentation/World/FarmScene.Buildings.cs](../../godot/Presentation/World/FarmScene.Buildings.cs) |
| M | [godot/Presentation/World/FarmScene.Display.cs](../../godot/Presentation/World/FarmScene.Display.cs) |
| M | [godot/Presentation/World/FarmScene.cs](../../godot/Presentation/World/FarmScene.cs) |
| A | [godot/Scenes/Village/NpcActor.tscn](../../godot/Scenes/Village/NpcActor.tscn) |
| A | [godot/Scenes/Village/ServiceRoom.tscn](../../godot/Scenes/Village/ServiceRoom.tscn) |
| A | [godot/Scenes/Village/Village.tscn](../../godot/Scenes/Village/Village.tscn) |
| A | [godot/Tests/NpcChecks.cs](../../godot/Tests/NpcChecks.cs) |
| A | [godot/Tests/NpcChecks.cs.uid](../../godot/Tests/NpcChecks.cs.uid) |
| M | [godot/Tests/Program.cs](../../godot/Tests/Program.cs) |
| M | [godot/verify.ps1](../../godot/verify.ps1) |

## Responsibilities and preserved boundaries

- Core/Domain: stable NPC definitions, schedule policy, navigation and validated persisted state.
- Core/Application: authoritative proximity-checked dialogue/trade/service commands and original-rule seed planting bridge.
- Core/Infrastructure: existing atomic v2 envelope extended without dropping replay metadata; legacy adapters reject unsupported state.
- Presentation/Scenes: reusable NPC actors, village/room maps, Thai HUD, nested door transitions and outdoor effects.
- Infrastructure/Assets: shared atlas/animation/actor caches; original final generated assets and full prompts.
- Tests/Diagnostics: Core NPC regression, rendered/headless real scene interactions, copied-profile reloads, rain and finite frame-time measurement.

No `native/` or `dist/` files changed from this baseline. Original farm/player bitmap assets are unchanged. No new package, NPC network RPC or standalone export was introduced.

## Documentation and evidence

- `docs/NPC_SYSTEM.md`: audit severity, architecture/dependencies, responsibility matrix, behavior, compatibility and remaining scope.
- `docs/NPC_MEASUREMENTS.md`: actual commands, checkpoint versus final targeted results, before/after performance and unresolved resource warning.
- `docs/NPC_CHANGED_FILES.md`: this inventory.
- `docs/NPC_SYSTEM_REQUEST.md`: archived user design request.
- `README.md`: new village entry controls and documentation links.
- `reports/npc-core-tests.json`, `npcs-Windows.json`, `npcs-headless.json`: final actual checks.
- `reports/npc-after-performance.json`: full-run farm benchmark snapshot.
- `reports/npc-resource-diagnostics.json`: resource acceptance remains open.
- Existing tracked farm/building/premium/native/lifecycle reports and captures refreshed by the complete rendered regression.
- Six actual NPC captures: village square, Mira dialogue/shop/home, Rowan workshop/home.

No real player profiles or transient logs are committed. See the measurements document for scope and failures; finite passing node tests do not certify leak freedom.
