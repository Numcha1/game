# Building slice verification and measurements

Windows i5-12400F / RTX 3060, Godot 4.7.2 .NET Compatibility, 1600x900 requested window, cap 60. Full `verify.ps1 -Rendered -ExtendedLifecycle` passed; final diagnostics-only position/frame sampler refinement was then rebuilt and both rendered/headless building scenes rerun. Final tests-only autosave/re-entry checks were rerun separately (Core 279).

| Check | Final passed | Scope |
| --- | ---: | --- |
| Core | 279 | Legacy simulation/save/network + building/storage/migration/fault regressions |
| Native | 35 | Original Native executable self-tests |
| Startup / recovery UI | 60 / 10 | Existing malformed/startup/recovery fixtures |
| Rendered farm / Premium UI | 101 / 180 | Original camera/input/crop/art/Thai/resolution checks |
| Building scene Windows / headless | 49 / 48 | Door, missing resource, collision, bed, storage UI, saved-room restart, cancelled teardown |
| Extended lifecycle | 350 | 50 full scene teardown cycles, 500 bag/shop cycles, pending save/settings flush |

Godot and Native builds passed with zero warnings/errors. No final failed checks. All player profiles are GUID fixtures. The first Headless restart assertion compared against another still-running session instead of its saved position; it was corrected to capture the expected snapshot position and to free the auxiliary scene before reporting failure. Both renderers passed the corrected check. This does not certify human playtesting or Internet sessions.

## Outdoor before / after

Before source: `reports/gdd-v11-after.json` (pre-building baseline). After: `reports/building-m2-performance.json`. Same 24-ripe-plot storm/evening fixture, 60 warmup + 600 frames per quality. Historical Milestone 1 measurements remain in `building-m1-performance.json`. No speedup is claimed.

| Quality | FPS before / after | Mean ms before / after | P95 ms before / after | Private MiB before / after |
| --- | --- | --- | --- | --- |
| Low | 60.062 / 60.003 | 16.650 / 16.666 | 16.774 / 16.772 | 209.6 / 215.9 |
| Medium | 60.002 / 60.055 | 16.666 / 16.651 | 16.706 / 16.697 | 214.9 / 220.9 |
| High | 60.002 / 60.002 | 16.666 / 16.666 | 16.710 / 16.689 | 204.6 / 211.2 |

## Actual interior workload

Night, outdoor storm suppressed under roof, quality 2, settled door/dawn transitions, 300 consecutive ProcessFrame intervals: mean 16.556 ms, P95 16.686 ms, calculated FPS 60.403. Private 267.4 MiB; managed 8.0 MiB. The engine cap is 60; short signal timing can measure slightly above/below it. Source `reports/buildings-Windows.json`.

Both indoor and each outdoor quality sample satisfy GDD mean <=17 ms/P95 <=18 ms in this measured workload. Headless reports omit graphics performance. No claim is made for future expanded farms, NPCs or streaming zones.

Extended lifecycle node baseline stayed 446, all samples zero orphan nodes; first/last forced-GC private samples 235.4/256.0 MiB. This lifecycle primarily covers farm/menu teardown; building diagnostics separately exercise five entry/exit cycles, a live saved-room reload and a cancelled restore. Finite tests cannot establish lifetime leak freedom. Forced-GC runs are not frame-pacing benchmarks.

Genuine screenshots: `farmhouse-interior.png`, `farmhouse-storage.png`, `farmhouse-night.png`. The night capture waits for dawn/door fades to settle. Human Thai/readability/audio reviews, long-session playtests and standalone Windows export remain open.

## Memory trend and scope

Both rendered and headless extended lifecycle passed 350 checks, 50 scene teardowns and 500 menu cycles, constant 446 nodes/49 resources and zero orphans. Private memory in MiB (round 0 / 10 / 20 / 49):

| Mode | Round 0 | Round 10 | Round 20 | Round 49 |
| --- | ---: | ---: | ---: | ---: |
| Rendered | 235.45 | 256.51 | 259.01 | 255.99 |
| Headless | 89.92 | 89.48 | 79.67 | 78.25 |

Rendered private memory rose initially and then plateaued; headless private memory declined from 89.9 to 78.2 MiB in this run. Stable nodes/resources and late plateaus do not identify every allocation or establish lifetime leak freedom. Engine caches/allocator retention remain a possible explanation, not a proven cause. Longer interior-specific soak and allocation profiling remain open.

## Final harness resource correction

The extra headless farm smoke passed its 26 checks but emitted one RefCounted ObjectDB exit warning. Synthetic InputEvent objects in that harness now dispose deterministically after delivery. Rebuilt with zero warnings/errors; the verbose headless rerun passed 26 checks with no ObjectDB exit warning, and Premium headless passed 52 checks. The same input harness was rerun under Windows rendering. This fixes a detected test allocation; it does not establish lifetime leak freedom.
