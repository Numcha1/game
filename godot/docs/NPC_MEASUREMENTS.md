# NPC Milestones 1-2: executed validation and limits

## Build and regression

Actual Windows target: i5-12400F / RTX 3060; Godot 4.7.2 .NET, .NET SDK 8.0.423. Commands run from repository root. No new package was added.

The complete `godot/verify.ps1 -Rendered -ExtendedLifecycle` run passed Core 323, Native 35, Startup 60, Recovery UI 10, Farm 101, Premium 180, Buildings 49, NPC 34 and Extended Lifecycle 350. Both builds reported zero compiler warnings/errors. This is the full-run checkpoint, not a claim that every diagnostic was rerun after every final refinement.

Final targeted validation after save validation, metadata-only persistence, atlas actor lifecycle, idle motion, settled HUD captures and village weather refinements:

- `dotnet run --project godot/Tests -- godot/reports/npc-core-tests.json`: 326 passed; includes original farming, save and local legacy network regression tests.
- `dotnet build godot/SeasonsTogether.csproj --no-restore`: zero compiler warnings/errors.
- Actual Godot `--npc-checks`: 34 rendered checks; `--headless --npc-checks`: 33 checks. Exit code 0 in each. Headless omits the rendered frame-time assertion.
- NPC checks exercise 800 lunch-travel frames, 1,000 return-to-work frames, 1,000 return-home frames, real F input/dialogue buttons/trade/carpentry service, both homes, copied-profile scene reloads, day rollover and return to the original farm.
- A forced village rain check verifies outdoor rain remains enabled while original farm pond rings are suppressed on the dry village map. Farm pond rings retain their default behavior.
- Save fixtures are isolated GUID profiles. No existing player saves were overwritten. Genuine pre-NPC v2 farmhouse fixtures load; missing NPC state in an already-used village save is refused. Original v1 rules and native network protocol are unchanged.

Functional assertions failed: 0 in these completed runs. Resource-clean-exit acceptance remains open below. Standalone Windows export and internet multiplayer were not executed for this delivery.

## Comparable original farm benchmark

Before: `reports/building-m2-performance.json`. After: `reports/npc-after-performance.json` (snapshot of the full rendered farm diagnostic). Existing storm/evening, 24 mature plots, 600 measured frames per quality, warm scenes with 60 FPS cap. Quality 0/1/2 means Low/Medium/High.

| Quality | Before FPS | After FPS | Before P95 ms | After P95 ms | Before private MiB | After private MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 60.003 | 60.066 | 16.772 | 16.728 | 215.9 | 222.3 |
| 1 | 60.055 | 60.002 | 16.697 | 16.695 | 220.9 | 226.1 |
| 2 | 60.002 | 60.002 | 16.689 | 16.697 | 211.2 | 216.4 |

These finite capped samples show retained frame pacing; they do not establish a speedup or long-session memory stability. The original farm gained a route sprite/label (446 to 448 nodes).

## Village benchmark and lifecycle

`reports/npcs-Windows.json`: 300 rendered frames, mean 16.6653 ms, P95 16.6874 ms, 60.005 FPS. Private memory 284.9 MiB; managed memory 11.1 MiB. Scope is an evening village after residents reached homes. Walking is verified separately; this is not a dedicated worst-case walking benchmark. Cold alpha-bound atlas scanning and first-load latency were not benchmarked.

The extended farm lifecycle run passed 350 checks across 50 full scene teardowns and 500 bag/shop cycles, with 448 nodes, 49 resources and zero orphan nodes in each recorded sample. This is the original farm lifecycle, not 50 NPC village reloads. The NPC diagnostic separately performs three actual copied-save scene reloads and asserts nodes and observers return to baseline each time. Neither finite test proves absence of every memory leak.

## Open resource diagnostic

Both final NPC processes report **one ObjectDB instance leaked at exit**, despite exit code 0 and passed functional assertions. A verbose asset-only probe identifies a generic native RefCounted with reference count 0; ownership/root cause is still unidentified. Earlier investigation included actor prefab ownership, audio stream release, node teardown, deferred shutdown and managed collection. The warning remains unresolved. `reports/npc-resource-diagnostics.json` records this failed resource acceptance separately from compiler and gameplay results. Do not label the slice leak-free or production certified.

## Actual visual evidence

Rendered captures: [village](../reports/village-square.png), [Mira dialogue](../reports/mira-dialogue.png), [seed shop](../reports/mira-seed-shop.png), [Rowan workshop](../reports/rowan-workshop.png), [Mira home](../reports/mira-home.png), [Rowan home](../reports/rowan-home.png). Shop/workshop captures await 20 engine frames after commands so the existing 0.25-second HUD refresh displays actual updated coins. No mockups or placeholders were used.

Original final assets were generated using built-in imagegen; full prompts are in [asset-prompts.json](../Assets/Village/asset-prompts.json). Runtime alpha-bound atlas slices handle the generated 1774 x 887 character sheet without editing bitmap pixels. Existing farm/player art remains unchanged.

Next acceptance: resolve the exit warning, broaden long-session/village load and Thai text-scale checks, then Milestone 3 dialogue/relationships. Gifts, reward-bearing quests, house-level upgrades, ten additional playable villagers and NPC co-op replication remain later milestones.
