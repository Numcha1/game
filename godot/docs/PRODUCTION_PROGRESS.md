# Production Core Progress (2026-10-11)

Branch `feature/production-core` was created from `godot/vertical-slice` and merged the tested modular foundation. This is a tested core-hardening iteration, not a claim that the complete production RPG is finished.

## Real verification

Ran `.\godot\verify.ps1 -Rendered -ExtendedLifecycle` in the actual `C:/xampp/htdocs/game` checkout on the user-selected i5-12400F / RTX3060 machine. Godot/.NET and Native Release builds: zero warnings/errors; engine import passed.

| Check | Result | Raw evidence |
|---|---|---|
| Core/save/local legacy protocol | 185 passed | production-core-tests.json |
| Native self-test | 35 passed | production-native-tests.json |
| Startup: nine isolated error fixtures | 45 passed | startup-Windows.json |
| Backup confirmation + real scene reload | 10 passed | recovery-ui-Windows.json |
| Rendered input/gameplay/season/resize | 101 passed | godot-smoke-Windows.json |
| 50 scene teardowns / 500 bag-shop cycles | 300 passed | lifecycle-extended-Windows.json |

Reports are in `../reports/`. Expected startup fault errors are deliberately injected; the checked process exit codes and fresh JSON reports passed. No real user save was used. Remote CI status is separate from these local results.

## Performance comparison

Same renderer/target/window/workload as baseline `80ae6159`: Compatibility/OpenGL, 1600x900, logical800x450, cap60, storm/24 ripe crops/night lights, warm60/sample600 frames per quality. Values are measured wall-clock frame times in milliseconds.

| Quality | Before mean/P95/P99 | After mean/P95/P99 | After FPS | After private MiB |
|---|---|---|---:|---:|
| Low | 16.666 / 16.788 / 16.854 | 16.666 / 16.784 / 16.851 | 60.003 | 190.1 |
| Medium | 16.666 / 16.787 / 16.847 | 16.666 / 16.782 / 16.846 | 60.002 | 191.3 |
| High | 16.666 / 16.778 / 16.847 | 16.643 / 16.767 / 16.821 | 60.086 | 185.0 |

Baseline raw: architecture-after.json (previous modular foundation); current: godot-smoke-Windows.json. Capped measurements support this finite workload near 60 FPS, not a percentage speedup or every-frame guarantee. Resize/capture transitions are outside the per-quality samples and can produce outliers. Lower-end hardware was not the accepted target.

Lifecycle samples retain 374 nodes, 22 resources and zero orphan nodes at corresponding checkpoints. Event observers return from two to zero on teardown. Shared texture/animation/font identity is checked. The earlier ten-scene pre-font-cache report is retained as production-lifecycle-before-font-cache.json; different run lengths/driver caching prevent causal memory-reduction claims. Forced GC and finite testing do not certify lifetime leak freedom.

## Phase/module report

| Module | Files changed | Problem fixed | Tests / compatibility / remaining |
|---|---|---|---|
| Audit/design | five design/gate/roadmap documents, this audit, source inventory | current risks and acceptance separated from implementation | real source refs; release audit still open |
| Save | Core/Infrastructure/Persistence/SaveService.cs; Tests/RecoveryChecks.cs | identity fork, interrupted paired import, concurrent writers, corrupted-save recovery and BOM compatibility | Core185/startup45/recovery10; Farm v1 preserved; physical power loss untested |
| Gameplay projection | Core/Domain/GameViews.cs; Application/GameReadModel.cs; Tests/GameplayMatrixChecks.cs | exposed original harvest window/previous crop/forecast/journal without duplicating formulas | eight crops x four seasons and full crop-to-sale cycles; original World unchanged |
| Presentation | FarmHud, FarmInputRouter, FarmWorldPresenter, FarmerController, FarmScene, Diagnostics partials | plot clarity/confirmations, startup guards, disposed-node safety and font reuse | rendered101/lifecycle300; original art/pixel settings preserved; onboarding/accessibility pending |
| Verification | verify.ps1, Core CI workflow, README/TESTING, JSON reports and captures | reproducible isolated checks and evidence | local wrapper passed; workflow only covers Core; full export/engine CI pending |
| Network/audio/art/content/release | no new production integration in this iteration | retained Native and adapter seams | Internet Godot co-op, production audio/art completion, expansion and standalone acceptance pending |

No tests failed in the final local run. Earlier stale-inspector and invalid-data catch failures were fixed and rerun. Complete module history and baseline architectural phases are retained in PHASES.md.

## Preservation hashes

- `native/World.cs`: `f21875fe49b274ae598a466968e4b64c593675c365838323459d292f17154444`
- `native/Network.cs`: `e3617a1038b262c31773e44bd03ea3a463117f95d7183a628143879253c96d8b`
- `SuanPleng.exe`: `4cc9a21581e51ac0e7961ed11edb3441a55be8b02157fcf0ea60bed890c6c24c`

Original scenes, shaders, art and gameplay/protocol sources have no diff from the integrated foundation. Tests never overwrite Native/player save data. New view fields are not serialized into the original save/network DTOs.

## Documents and next gates

- [Game Design](../GAME_DESIGN_DOCUMENT.md)
- [Technical Design / dependency diagram / responsibility matrix](../TECHNICAL_DESIGN_DOCUMENT.md)
- [Roadmap](../DEVELOPMENT_ROADMAP.md)
- [Quality Gates](../QUALITY_GATES.md)
- [Feature Matrix](../FEATURE_MATRIX.md)

Next required work: onboarding/keyboard UI; protocol-compatible server lifecycle/request hardening; Godot room/remote-avatar integration and two-home QA; dedicated original art and licensed audio; structured playtests/balance; matching export templates, standalone Windows and clean-machine verification. Content expansion stays deferred until core/co-op gates pass.
