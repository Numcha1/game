# Executed world visual validation

Hardware: user-selected i5-12400F / RTX 3060; Godot 4.7.2 .NET, OpenGL Compatibility, NVIDIA 610.62. Tests executed in Windows with rendered engine instances and isolated diagnostic save profiles.

## Reproduction and actual results

Run `./godot/verify.ps1 -Rendered` from the repository. For the gallery alone: Godot `--path godot -- --npc-checks --composition-checks`. Baseline uses `--composition-before` against original `adb7d6c` presentation. The verifier checks report freshness, successful process exit and minimum assertions, rather than accepting an old report.

| Suite | Passed |
| --- | ---: |
| Core regression, including save/network peer fixtures | 326 |
| Native build/self-test | 35 |
| Startup | 60 |
| Recovery UI | 10 |
| Rendered farming, collision, plots, four seasons, quality/input | 101 |
| Premium UI/rendering | 180 |
| Buildings, bed/chest/light/reload/transitions | 49 |
| NPC movement/schedule/shop/workshop/home/save | 34 |
| Indoor HUD/camera/input/layout | 46 |
| Controlled seven-scene composition | 190 |
| Lifecycle: 10 scene teardowns, 100 bag/shop cycles | 70 |

Full wrapper completed successfully, builds had zero compiler warnings/errors. The final Rowan foreground correction was then rebuilt and the affected full seven-scene composition suite rerun. No failing gameplay assertion remains in these scopes. The earlier format/UV rendering failures and incorrect chair outline were fixed after inspecting actual images; assertions alone were insufficient.

## Controlled before/after gallery

Each scene is captured at requested window sizes 1280x720, 1600x900 and 1920x1080. Both fixtures use spring/day 1, minute 600, fair weather and matching player world coordinates and `Camera2D.GlobalPosition`. The after suite compares these recorded values directly against [before-all.json](../reports/composition/before-all.json); results and frame timings are in [after-all.json](../reports/composition/after-all.json).

The camera profile deliberately changes from 1.25 to 1.0 indoors. Effective camera clamping/framing therefore changes even though the matched camera-node position is retained. PNGs are actual engine viewport readbacks, not desktop/window chrome screenshots or image-generator mockups. Integer stretching may produce a 1600x900 viewport image at a requested 1920x1080 window; `shotWidth`/`shotHeight` record the actual output. Window resize, visibility, stationary camera, HUD separation, unit scale, opaque floors and side coverage are independently asserted. Do not relabel viewport PNGs as native-sized desktop captures.

| Actual region | Before, 1600 window | After, 1600 window |
| --- | --- | --- |
| Seed shop | [before](../reports/composition/before-village.seed-shop-1600x900.png) | [after](../reports/composition/after-village.seed-shop-1600x900.png) |
| Farmhouse | [before](../reports/composition/before-moonroot.farmhouse-1600x900.png) | [after](../reports/composition/after-moonroot.farmhouse-1600x900.png) |
| Workshop | [before](../reports/composition/before-village.workshop-1600x900.png) | [after](../reports/composition/after-village.workshop-1600x900.png) |
| Mira home | [before](../reports/composition/before-village.mira-home-1600x900.png) | [after](../reports/composition/after-village.mira-home-1600x900.png) |
| Rowan home | [before](../reports/composition/before-village.rowan-home-1600x900.png) | [after](../reports/composition/after-village.rowan-home-1600x900.png) |
| Farm | [before](../reports/composition/before-farm-1600x900.png) | [after](../reports/composition/after-farm-1600x900.png) |
| Village | [before](../reports/composition/before-moonroot.village-1600x900.png) | [after](../reports/composition/after-moonroot.village-1600x900.png) |

All 21 pairs are in `reports/composition`; replace the size suffix for other requests. [Behind chair](../reports/composition/chair-behind.png) / [in front](../reports/composition/chair-front.png) are separate depth probes using real valid movement positions. These prove a rendered approach, not exact per-pixel masking or a new seating mechanic. All seven 1600-window scenes were visually inspected; smaller/larger layout checks are automated, not human artistic signoff at every size.

## Performance and resource comparison

Farm benchmark is the unchanged gameplay diagnostic, with 600 sampled frames per quality. [Archived baseline](../reports/composition/before-performance.json) came from the prior commit; [current run](../reports/composition/after-performance.json) uses the modified presentation. Both used the target hardware, but background load/focus was not controlled sufficiently to infer a causal speedup from the poor baseline. Values below are recorded samples, not a promised percentage improvement.

| Quality | Before FPS / P95 ms | After FPS / P95 ms | After mean draw calls | After private MB (end) |
| --- | --- | --- | ---: | ---: |
| Low | 34.99 / 67.00 | 60.01 / 16.77 | 87.29 | 236.89 |
| Medium | 41.05 / 66.29 | 60.00 / 16.71 | 127.43 | 241.82 |
| High | 28.46 / 67.26 | 60.00 / 16.70 | 178.97 | 232.23 |

The controlled gallery additionally measures 180 frames each in seed shop and farm; current values are in `after-all.json` and must pass mean <=17ms and P95 <=18ms. That fair-weather workload differs from the gameplay benchmark; no matching baseline gallery timing was recorded, so no gallery speedup is claimed.

Lifecycle node count is stable at 437 (previous run 448), resources 49, orphans 0 across ten teardown rounds. This is a finite observation, not a proof of no memory leak. Native engine cleanup still reports one ObjectDB instance at exit in NPC/composition runs. This pre-existing warning remains unresolved and is distinct from compiler warnings. Save fixtures avoid original user data; Core/native code and protocol are untouched. Loopback tests are not cross-house Internet certification. No new Windows export is claimed.

See [remaining art acceptance](WORLD_COMPOSITION.md) for bed/chair/door/floor/terrain limitations.
