# Production Core Audit (2026-10-11)

Scope: current Godot/Core/native source and existing baseline audit, followed by the focused save, command, UI and scene-lifecycle fixes below. This is not a completed release/security certification. The source inventory lists tracked code/resources and hashes in `../reports/production-source-inventory.json`. Historical full structure analysis is [AUDIT.md](AUDIT.md); current dependency direction and responsibility matrix are in [Technical Design](../TECHNICAL_DESIGN_DOCUMENT.md).

| Severity | Actual file reference | Finding / current disposition |
|---|---|---|
| Critical | Core/Infrastructure/Persistence/SaveService.cs, LoadPlayerSession | Missing/corrupt/unrelated identity could detach an inhabited farm from its owner. Fixed in Godot: validate before mutation; legacy Native constructor risk remains below. |
| Critical | Core/Infrastructure/Persistence/SaveService.cs, ImportLegacyAsync/RecoverInterruptedImport | Farm and identity replacements were individually atomic but not recoverable as a pair after interruption. Fixed with bounded undo journal, rollback and startup recovery; physical power-loss durability remains unproven. |
| High | Core/Infrastructure/Persistence/SaveService.cs:23-42, DisposeAsync | Concurrent Godot processes could write one profile. Fixed with lifetime FileShare.None lease, writer/import completion before release and independent-process regression. Native uses its separate profile. |
| High | Presentation/World/FarmScene.cs; Diagnostics/FarmScene.Startup.cs | InvalidDataException was outside the IOException catch; missing scene nodes and bad assets could fail composition. Fixed controlled startup, explicit validated-backup UI and awaited service shutdown; nine fault fixtures plus real restore/reload checks. |
| High | native/GameForm.cs:39-41 | Native replaces malformed player identity with a new GUID and writes immediately. Unchanged legacy risk; migrate to guarded persistence before sharing live profile implementations. |
| High | native/Network.cs:48-109 | Nullable/malformed request fields can fault detached handlers; server shutdown does not await accept/handler/timer completion. Unchanged; harden with protocol-compatible validation/tracked shutdown before Godot host integration. |
| High | Godot project/launcher; Core/Application/Networking | Godot online room/remote actors and standalone export are absent. Local protocol tests do not establish Internet co-op or a shipping executable. Open acceptance gates. |
| Medium | native/Network.cs:38-41,92 | Native save performs synchronous disk I/O under server lock. Retained for compatibility; profile latency and adapt to the bounded persistence seam before production hosting. |
| Medium | Presentation/UI/FarmHud.cs; Presentation/Input/FarmInputRouter.cs | Original plot state/deadline and destructive-action consequences were unclear. Fixed inspector, original forecast/journal and cancel/confirm; keyboard menu navigation and guided onboarding still pending. |
| Medium | Presentation/UI/FarmHud.cs; Diagnostics/FarmScene.Lifecycle.cs | Per-reload SystemFont recreated glyph resources. Shared cache now tested through 50 scene teardowns and 500 menu cycles; no lifetime leak certification. |
| Medium | ART.md; Presentation/Assets/AssetCatalog.cs; UI menus | Dedicated growth/tool/UI art and production internal audio are incomplete. Kept original visuals during core hardening; stage-specific original assets and review remain necessary. |
| Low | Tools/ArtBake/Program.cs | Offline atlas/bake arguments and bounds need stronger validation before expanding artist pipeline. Does not run in the game loop. |
| Low | Diagnostics/FrameTimeMonitor.cs; reports | Finite capped frame samples and forced-GC teardown diagnostics are not long-term gameplay measurements. Keep raw evidence and perform a longer soak before release. |

## Architecture and compatibility comparison

Baseline vertical slice linked World/Network but scene code mixed lifecycle, save, menus and presentation. The integrated modular foundation puts rules in the original engine-independent World, commands/events in GameSession, immutable projections in GameReadModel, save/network/config/logging behind used contracts, and Godot-specific rendering/input/UI/resources in Presentation. Domain has no Godot/WinForms/graphics dependency; tests enforce this boundary.

This iteration strengthens the actual seams instead of creating interfaces for every class: one save worker and profile lease, durable import recovery, explicit scene shutdown, original-rule command comparisons and cached assets/fonts. New plot metadata is view-only: Farm v1 schema and Request/Reply wire DTOs remain unchanged. Native files and original production art/scene/shader resources are preserved.

## Open boundaries

Local UUID/auth/reconnect tests validate the adapter seam, not Godot room integration or hostile Internet traffic. A pending Native request-hardening change must preserve protocol, command idempotency and original rules. Backups require a matching identity; unavailable/bad backups refuse recovery rather than inventing an owner. Audio/art/export/player playtests remain separate gates.
