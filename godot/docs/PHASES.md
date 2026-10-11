# Phase reports

Baseline `ecf7e327` → `godot/modular-architecture`. Execution order was audit → low-risk lifecycle isolation → persistence boundary → application consumers → network seam → presentation/resource ownership → diagnostics/performance. The requested phase labels below describe the result; persistence was implemented before routing live commands into it for safety.

| Phase | Files changed / problems fixed | Tests passed | Tests failed at final checkpoint | Compatibility / remaining issues |
|---|---|---|---|---|
| 1 Full audit | `docs/AUDIT.md`, `reports/architecture-before.json`; source review across Godot/Core/native with severity/file refs | Baseline build; 35 core; 91 rendered scene checks | 0 | No source or data changes; audit includes residual native risks |
| 2 Modular foundation | `Diagnostics/FarmScene.Diagnostics.cs`, scene/HUD lifetime/profile guards | Build; 35 core; 16 headless scene checks | 0 | Diagnostic profiles isolated; full signal cancellation completed in phase 7 |
| 3 Gameplay/application | `Core/Application/GameSession.cs`, `GameReadModel.cs`, `Services.cs`, `Core/Domain/GameViews.cs`, compatibility facade, presentation consumers, `Tests/ApplicationChecks.cs` | Build; 62 core; 16 headless scene checks | 0 | Original World rules; readonly UI; main-thread ownership; no multiplayer scene migration |
| 4 Network | `Core/Application/NetworkService.cs`, `Core/Infrastructure/Network/HttpRoomTransport.cs`, `Tests/NetworkChecks.cs` | Build; 72 core; 16 headless scene checks | 0 | Exact legacy protocol; four clients/auth/UUID/reconnect/ack-loss/timeout/cancel; real internet/RPC UI not migrated |
| 5 Save | `Core/Infrastructure/Persistence/SaveService.cs`, `Core/Domain/FarmInvariants.cs`, `Tests/PersistenceChecks.cs` | Build; 51 core at initial save checkpoint; import/dispose regression included in final 82 | 0 | v1, older v1, backups, bounded writer, corrupt save refusal, disk recovery, import rollback; power-loss pair atomicity remains limited |
| 6 Presentation | Moves into `Presentation/{World,UI,Player,Input,Camera,Effects}` and `Infrastructure/Assets`; only script paths in `Scenes/Farm.tscn`; `MusicLink.cs`, `GraphicsSettings.cs` | Build; 72 core before architectural checks; 16 headless; 91 rendered checks | 0 | Original sprites/shaders/theme/layout/game controls retained; diagnostics fixtures remain intentionally separate |
| 7 Quality/performance | `Diagnostics/FrameTimeMonitor.cs`, `FarmScene.Lifecycle.cs`, cancellation waits, `Tests/ArchitectureChecks.cs`, structured logger, final reports | Actual checkout build: 0 errors/warnings; 82 core; 60 lifecycle each headless/rendered; 16 headless and 91 rendered scene checks; native Release build and 35 self-tests | 0 | Scoped node/resource soak and actual FPS/memory measurements; not long-term leak certification |
| 8 Safe refactor | New branch, incremental commits, architecture/performance/phase docs, preservation manifest and changed-file list | Final checks above; native/art/protocol preservation check | 0 | No merge into base/main, no native deletion, no new packages; source architecture ready for further controlled expansion |

## Failures encountered and resolved

Fresh staging build with `--no-restore` initially failed due to absent restore output; a normal restore/build succeeded. During application routing, an old `State` null guard remained in WeatherLighting; corrected to the read model and rebuilt. New architectural guard initially matched `Session.StateChanged` as mutable `Session.State`; corrected to match property boundaries. The failed Windows test process briefly held its apphost file; it was stopped and the clean rebuild/test succeeded. Lifecycle harness exposed a genuine signal-await hang after node free; cancellation and disconnect replaced the boolean-only guard, and 60 lifecycle checks passed in both staging and actual checkout. These are resolved intermediate failures, not omitted from the final status.

## Test commands (from godot/)

```powershell
dotnet build
dotnet run --project Tests -- reports/final-core-tests.json
& $env:GODOT_MONO --headless --editor --path . --quit
& $env:GODOT_MONO --headless --path . -- --lifecycle
& $env:GODOT_MONO --headless --path . -- --smoke
& $env:GODOT_MONO --path . -- --smoke
```

Run rendered and headless workloads separately when comparing FPS. Smoke and lifecycle flags create UUID profiles in reports. Core tests use isolated profiles; native self-test uses a temporary UUID directory. None opens or overwrites the player's farm. The finite lifecycle workload creates/frees 10 scenes with 100 bag/shop cycles, drains saves/logs and checks observer/node/resource ownership.

## Remaining risks

1. Native server lifecycle/malformed authenticated-field handling and synchronous host saves remain as documented in the audit; the native implementation was preserved.
2. Godot multiplayer UI/remote players/RPC adapter and real cross-house connectivity are future migration work. Local HTTP interoperability is proven; no internet deployment claim.
3. Import rollback handles normal I/O exceptions, but two-file replacement cannot guarantee pair consistency during abrupt power loss. Paired backups remain available.
4. V1 is the only supported schema. A future migration needs real fixtures and an explicit version transform before changing the writer.
5. Core retains legacy public mutable types and synchronous methods for native compatibility; production presentation is guarded by source dependency tests rather than separate assemblies.
6. Performance samples are capped finite runs on the current machine; memory growth during sampling includes diagnostic tasks and report allocations. Longer hardware/driver soak and release export testing remain necessary.
7. Existing Godot export-template limitation remains; this refactor builds/runs the project and preserves the Windows native executable, without claiming a new standalone Godot export.
