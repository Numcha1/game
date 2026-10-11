# Milestone 1: architecture and persistence reliability

## Baseline and checkpoints

Original base: `ecf7e3276197821d093b163113468383366aabd7` (`godot/vertical-slice`). New branch was already clean at that exact base when inspected. Original build succeeded with zero warnings/errors; its 35 checks passed in `reports/systems-baseline-core.json`. No history rewrite was performed.

Integrated tested architecture/presentation using merge `3f3ffd1`. This preserves the existing premium artwork, fonts and UI. Integrated baseline passed Core 194, Native 35, startup 45, recovery 10, rendered gameplay 101, premium 175 and lifecycle 300. The actual rendered baseline performance is retained in `reports/systems-m1-before.json`. The original Native/dist sources have no changes from the base.

Rollback checkpoints: base `ecf7e32`, integration `3f3ffd1`, strict save contract `c433903`, persistence lifecycle `7941f6a`. Checkpoints can be checked out in an isolated review checkout; do not reset a working branch or overwrite any player save to review them.

## Prioritized audit against actual sources

| Severity | Source and finding | Result / remaining issue |
| --- | --- | --- |
| Critical | `Core/Infrastructure/Persistence/SaveService.cs`, `RecoverInterruptedImport`: a Version 1 undo object missing Farm/Identity could become null defaults; Restore interpreted null as absence and could delete profile files. | Required undo members validated before either file changes. Explicit null is still valid for a genuine originally absent file. Malformed/unknown/duplicate undo data is refused and retained. Four fault fixtures verify all three original files byte-for-byte. |
| High | Save deserialization silently skipped future fields and initialized omitted core fields; a later save could lose state. | New `SaveJsonContract.cs` rejects unmapped fields, duplicate decoded property names and missing Version/Day/Minute/Coins/Plots/Players before model use. Unknown future state is preserved by refusal, not converted to a fresh farm. |
| High | `Presentation/World/FarmScene.cs`: async-void manual/graphics persistence was not included in scene shutdown; pending settings could finish after a new scene loaded. | Extracted `FarmScene.Persistence.cs`, tracked manual save and ordered graphics writes; exit/import waits for pending persistence before session/log disposal. |
| High | `ReleaseServices` disposed the session only after pending tasks succeeded; a fault could skip the session/profile lease cleanup. | Session disposal now runs in finally even after a pending-task failure; structured error codes retained. |
| High | Original `Core/LocalFarmSession.cs` at the base exposed mutable Farm/Player and synchronous saves on every command/tick. | Integrated prior verified GameSession/read models, application service contracts and bounded background atomic save worker. No new duplicate farming rules. |
| High | Godot playable scene has no room/session UI, remote actors or online authority binding. | Existing adapter/service preserved and tested. Local 2/4-process checks added; playable Godot co-op remains IN PROGRESS, not completed by protocol tests. |
| Medium | Snapshot serialization/validation remains on the owning simulation thread; pathological near-limit states can cost frame time. | Immutable snapshot capture retained; disk replacement stays on the worker. Profile representative saves before changing the capture strategy. No speculative rewrite or speedup claim. |
| Medium | Bounded UUID deduplication and state snapshots are not a persistent session replay ledger. | Existing protocol preserved; stale state/reconnect/concurrency acceptance must be completed when room binding is implemented. No seamless host migration claim. |
| Medium | Full clean-machine export, long gameplay soak and independent-machine network testing are absent. | Explicit roadmap gates; user selected local multiple-instance testing first. |
| Low | Legacy path names in the master plan refer to Scripts/LocalFarmSession; current integrated paths differ. | Actual implementation is under Core/{Domain,Application,Infrastructure}, Infrastructure/Assets, Presentation and Diagnostics. Documentation links the real files. |

## Implemented changes and responsibility boundaries

- `Core/Infrastructure/Persistence/SaveJsonContract.cs`: persistence-only schema interpretation, recursive duplicate-name rejection, required-member checks, unknown-field refusal and existing BOM acceptance. No Domain rules are added.
- `SaveService.cs`: uses the strict reader for the farm and import undo record. Existing size limits, atomic replacement, backup, profile lease, queue coalescing and paired rollback remain intact.
- `Presentation/World/FarmScene.Persistence.cs`: scene-owned manual save, ordered graphics persistence, shutdown, import and service disposal. `FarmScene.cs` keeps composition, simulation/presentation coordination and commands.
- `Diagnostics/FarmScene.Lifecycle.cs`: queues 0→1→2 graphics changes and manual save immediately before each teardown; verifies final preference, farm state and no leftover temporary files.
- `Diagnostics/FarmScene.Startup.cs`: three additional unsupported/missing/undo fault cases, real error-dialog visibility and preservation/lease-release checks.
- `Tests/SaveContractChecks.cs`: 28 meaningful schema/BOM/startup/import/undo checks on GUID profiles.
- `Tests/LocalRoomProcessChecks.cs`, `Tests/Program.cs`: 2 and 4 OS-process room scenarios through the unchanged HTTP protocol. Ephemeral test credentials use stdin, not CLI arguments/files/logs. One host process also runs a client; the other peers are separate .NET processes. Concurrent harvest generates one yield and pays energy once; UUID retries preserve inventory. This tests transport/application compatibility, not Godot remote rendering.
- `verify.ps1`: startup minimum 60, lifecycle 70/350; existing fresh-report and process-status validation preserved. CI includes the new development branch.

The dependency direction remains Presentation → Application/read models/contracts → Domain. Infrastructure implements persistence/network/log/configuration contracts. Domain uses original World.cs with no Godot/WinForms/graphics reference. Dependency diagram and responsibility matrix remain in `ARCHITECTURE.md`; no per-class interface proliferation or new package was introduced.

Framework reference: .NET supports rejecting unmapped JSON fields through [JsonUnmappedMemberHandling](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members). This is applied at the save boundary only; Native source and network wire parsing are unchanged.

## Test evidence

Regression was first run against the old reader and failed at `missing critical save member refused: Version`, proving the defaulting defect. After the fix, Core passed 222 including 28 added save-contract checks. Adding 12 local multi-process assertions brought the final Core result to **234** (`reports/systems-m1-core.json`).

Actual Godot and Native builds succeeded with zero warnings/errors. Rendered wrapper passed startup **60**, recovery **10**, gameplay **101**, premium **175**, and extended lifecycle **350** across 50 scene teardowns/500 menu cycles. Headless startup also passed **60**. Native release self-tests passed **35**. Startup fault injection deliberately logs engine errors; those expected negative cases are not failing assertions. The extracted persistence file initially missed System.Environment's alias and failed compilation; it was corrected and rebuilt before its commit.

Reproduce from repository root:

```powershell
dotnet run --project godot/Tests -- godot/reports/systems-m1-core.json
.\godot\verify.ps1 -Rendered -ExtendedLifecycle
.\godot\verify.ps1
```

The first command includes real 2/4-process loopback scenarios and cleans up test child processes. Reports/profiles are isolated; no real save is used. Remaining multiplayer cases include Godot room/actor integration, real disconnect-during-save/network interruption UI, late joining and cross-house testing. No assertion that all Milestone 3 acceptance criteria passed is made.

## Save and migration policy

Complete original Version 1 saves and pre-animation Version 1 saves remain readable. The original Farm serializer always emits the six required members; incomplete handwritten JSON is now intentionally refused instead of defaulted. Older optional Effects/EffectSequence remain supported. UTF-8/16/32 BOM formats remain accepted.

Unknown future fields are refused while preserving exact source bytes; no new Version 2 schema or migration was introduced. Backup recovery remains an explicit UI choice; no unsupported player data is silently replaced. New persistent farming/inventory/exploration fields require a separately designed version migration: immutable source backup plus paired identity, bounded read, explicit known-version transform, validation, atomic installation, rollback and round-trip/native/protocol compatibility tests. Future migrations must not guess how to translate unknown fields.

## Performance scope and budgets

Target hardware remains i5-12400F / RTX 3060. The controlled fixture is 24 ripe crops, storm/evening lights, 1600×900 requested window, 60 warmup + 600 recorded frames per quality, cap 60 FPS. `systems-m1-before.json` and the final `godot-smoke-Windows.json` use the same upgraded rendering pipeline; numerical comparison is in `SYSTEMS_MEASUREMENTS.md`. No graphics workload or gameplay rules were changed by this milestone.

Engineering budgets for future work: 16.67 ms average pacing at the 60 cap, controlled P95 ≤17 ms; renderer CPU mean ≤4 ms and GPU mean ≤8 ms on this hardware; private working set ≤384 MiB in the current fixture; no increasing scene-node baseline or orphan nodes in the finite teardown workload; no disk writes/resource loads in the per-frame presentation path. These are workload budgets, not promises about every machine or all lifetime gameplay. TimeProcess is engine-frame work, not CPU-only timing; forced-GC lifecycle samples do not measure normal gameplay pacing.

## Completion boundary

Milestone 1's named automated build/architecture/persistence criteria are TESTED. Full premium RPG/release readiness remains open. Original gameplay, Native/dist, Save v1, art and protocol are retained. Milestone 2/3 acceptance and advanced mechanics/exploration/export remain listed in `SYSTEMS_ROADMAP.md`, with no new gameplay values invented during the refactor.
