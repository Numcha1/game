# Indoor presentation: executed validation

Windows target: i5-12400F / RTX 3060. Godot 4.7.2 .NET; .NET SDK 8.0.423. All changes are presentation/diagnostics; Core, native rules, saves and image assets are unchanged.

The complete `godot/verify.ps1 -Rendered` checkpoint passed both builds with zero compiler warnings/errors, original Core/native regressions, startup/recovery, farm, premium UI, buildings, NPCs, the 42-check focused layout diagnostic and 70 lifecycle checks. After final feedback-priority and diagnostic texture-disposal refinements, the affected building and focused indoor diagnostics were rebuilt and rerun; those final reports are listed below. It is not claimed that every unrelated diagnostic was repeated after the last small change.

| Suite | Passed | Evidence |
| --- | ---: | --- |
| Core | 326 | [report](../reports/production-core-tests.json) |
| Native | 35 | [report](../reports/production-native-tests.json) |
| Startup | 60 | [report](../reports/startup-Windows.json) |
| Recovery UI | 10 | [report](../reports/recovery-ui-Windows.json) |
| Farm | 101 | [report](../reports/godot-smoke-Windows.json) |
| Premium | 180 | [report](../reports/premium-Windows.json) |
| Buildings | 49 | [report](../reports/buildings-Windows.json) |
| NPC | 34 | [report](../reports/npcs-Windows.json) |
| Lifecycle | 70 | [report](../reports/lifecycle-Windows.json) |
| Focused indoor scene | 46 | [report](../reports/indoor-layout-after.json) |

Final targeted commands: `dotnet build godot/SeasonsTogether.csproj --no-restore`, Godot `--building-checks`, and Godot `--npc-checks --indoor-layout-checks`. Engine exit codes were 0. The verifier checks new report timestamps and required assertion counts, preventing stale evidence from passing.

The focused check asserts actual 1280 x 720 and 1920 x 1080 windows, default and maximum Thai text/UI layouts, NPC/player scale and foot anchor, standing camera stability, room coverage, real walking input/animation and collision, NPC interaction, immediate prompt hiding, fresh feedback replacing the prompt, a new target replacing stale feedback, no-target silence, actor visibility across the room, and outdoor camera/scale restoration. Engine captures were visibly inspected: the logical viewport is 800 x 450, and the PNGs are 800 x 450 for the 1280 x 720 window and 1600 x 900 for the 1920 x 1080 window under integer stretching. They are not native-resolution desktop captures.

## Failures found and corrected

The first focused run caught a prompt still visible for one process frame after modal opening. Modal opening now clears it synchronously. The first full checkpoint caught old feedback hiding a valid farmhouse door prompt. Final coordination tracks the contextual target: fresh same-target feedback stays visible, while a changed target replaces stale feedback. Dedicated final checks cover both cases. Completed final functional checks failed: 0.

## Performance and resource scope

- Original farm quality 0: 34.986 FPS, P95 67.004 ms over 600 measured frames.
- Original farm quality 1: 41.051 FPS, P95 66.290 ms over 600 measured frames.
- Original farm quality 2: 28.460 FPS, P95 67.264 ms over 600 measured frames.
- NPC village: 60.006 FPS, P95 16.856 ms over 300 rendered evening frames; this is not an indoor/worst-case benchmark.

The pre-existing **one ObjectDB instance leaked at exit** warning still appears in isolated NPC/indoor processes. Compiler warnings are zero; the native cleanup warning is a separate unresolved acceptance issue. Passing finite lifecycle/node checks do not establish leak freedom or long-session stability. No internet multiplayer or new Windows export was tested.

No player save was overwritten: diagnostics use isolated GUID profiles. Domain/native source and all existing room/character PNG assets are unchanged. Compatibility is checked by the original Core suite and real building/NPC save reload regressions.

See [presentation rules, audit and changed files](INDOOR_PRESENTATION.md) and the four linked before/after captures there. Remaining visual limits: the existing room keeps its open floor plan, modal design is preserved, ultrawide/touch and human Thai acceptance are outside this test scope.
