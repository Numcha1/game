# Furniture validation record

Validated 2026-10-12 on i5-12400F / RTX 3060, Windows, Godot 4.7.2 .NET, OpenGL Compatibility / NVIDIA 610.62.

## Automated gates

Core before changes: 383 passed. Final Godot and Native builds: zero warnings/errors. Final current gates:

| Gate | Passed |
|---|---:|
| core | 470 |
| native | 35 |
| startup | 60 |
| recovery | 10 |
| smoke | 101 |
| premium | 180 |
| buildings | 51 |
| npc | 34 |
| indoor | 46 |
| inventory | 21 |
| furniture_box | 14 |
| furniture_six | 60 |
| composition | 190 |
| lifecycle | 70 |

No failed final gates. See [machine-readable full summary](../reports/furniture-regression-summary.json), [470 Core assertions](../reports/furniture-core-tests.json), [first box loop](../reports/furniture-box-loop-Windows.json), [six types in real Godot](../reports/furniture-six-types-Windows.json). The full verifier refreshes the existing regression reports/screenshots. Historical revisions remain available in Git; this new furniture summary records the complete current run. The preliminary furniture-performance.json delta-time sample is retained, but final comparisons use the four monotonic-clock reports below.

First box loop ran before the six-type scene gate: real shop buy, bag select, green preview, mouse placement, collision, F storage UI, deposit/organize, nonempty pickup rejection, atomic save, withdraw and pickup. The six-type gate adds all other purchases/placements, actual inventory sprites and safe display retrieval, authored table rotation, chair sit/F stand, chest transfers, shelf wood display, crafting, invalid exit preview, cancellation, real restarted FarmScene restore and teardown.

## Measured performance

Each row measures 600 frames after 20 settling frames. Frame intervals/FPS use monotonic `Time.GetTicksUsec`, rather than a target-FPS label. The empty farmhouse uses High; the furnished scene contains six actual objects and two actual displayed items. Both keep the same closed Help UI contents for comparable node/memory measurements. Assets are already warm in both: this isolates placed-scene overhead, not pre-refactor cold-start performance. These are sequential local runs, not statistical repeat-run significance claims.

| Workload | FPS | Frame P95 ms | Nodes | Resources | Static engine memory MiB start → end |
|---|---:|---:|---:|---:|---:|
| empty-farmhouse | 60.01 | 16.759 | 466 | 62 | 41.66 → 41.66 |
| six-types-low | 60.02 | 16.699 | 486 | 62 | 41.94 → 41.84 |
| six-types-medium | 60.00 | 16.698 | 486 | 62 | 41.84 → 41.84 |
| six-types-high | 60.00 | 16.701 | 486 | 62 | 41.84 → 41.84 |

All four workloads keep a stable node count throughout their 600-frame interval. Raw reports also contain median intervals and managed memory observations. Godot's raw process monitor is recorded separately; it is not an isolated furniture CPU-time benchmark. No 64-piece stress, long production simulation or Internet load is certified. Ten full scene teardowns and 100 menu cycles return to baseline nodes with zero orphan nodes; [raw lifecycle samples](../reports/furniture-regression-summary.json) remain a finite soak, not a lifetime memory guarantee. The pre-existing one-instance ObjectDB warning remains at diagnostic exit.

Measurements: [empty High farmhouse](../reports/furniture-performance-empty-farmhouse.json), [Low](../reports/furniture-performance-six-types-low.json), [Medium](../reports/furniture-performance-six-types-medium.json), [High](../reports/furniture-performance-six-types-high.json).

## Actual rendered captures

- [Six playable pieces and inventory items in the farmhouse](../reports/furniture-farmhouse-six-types.png)
- [Real box storage interface](../reports/furniture-box-storage.png)
- [Real workbench recipe interface](../reports/furniture-workbench.png)

These are actual viewport captures from the gameplay scene, not reference images or mockups. Runtime sprites come from the original generated source and prepared atlas in `Assets/Furniture`; provenance is recorded in `SOURCE.json`. Original painted fixed furnishings and the existing room background are retained.

## Reproducing

```powershell
dotnet run --project godot/Tests/SeasonsTogether.Tests.csproj -- godot/reports/furniture-core-tests.json
pwsh -File godot/verify.ps1 -Rendered
```

The verifier resolves the installed Godot Mono executable or accepts `-GodotPath`. First `--npc-checks --furniture-checks --furniture-box-only`, then `--npc-checks --furniture-checks` runs against ignored GUID profiles. To reprepare artwork deliberately, build then run Godot with `--headless --path godot res://Tools/FurnitureBake.tscn`, and import through `godot/build.ps1`. That bake is an offline tool and never runs in gameplay.

## Changed source groups

- Domain: `Core/Domain/PlacedFurniture.cs`, `ItemDefinitions.cs`, `Buildings.cs`.
- Application: `Core/Application/GameSession.Furniture.cs`, `GameSession.cs`, `GameSession.Buildings.cs`, `GameReadModel.cs`, `Services.cs`.
- Persistence: `Core/Infrastructure/Persistence/BuildingSaveDocument.cs`, `SaveService.cs`.
- Assets: original `Assets/Furniture` source/atlas/provenance/import metadata; `Infrastructure/Assets/AssetCatalog.cs`, `FurnitureAssetBake.cs`, `FurnitureBakeRunner.cs`, `Tools/FurnitureBake.tscn`.
- Presentation: `Presentation/Buildings/FurnitureRoomView.cs`, `InteriorRoom.cs`; `World/FarmScene.Furniture.cs`, `FarmScene.Buildings.cs`; `Input/FarmInputRouter.cs`; `Player/FarmerController.cs`; `UI/FarmHud.Furniture.cs`, `FarmHud.Inventory.cs`, `FarmHud.Npcs.cs`.
- Verification: `Tests/FurnitureChecks.cs`, `DefinitionChecks.cs`, `ItemTransactionChecks.cs`, `Program.cs`; `Diagnostics/FarmScene.FurnitureChecks.cs`, `FarmScene.NpcChecks.cs`; `verify.ps1`.
- Documentation/evidence: current furniture architecture/validation documents, README/status links, new furniture reports and actual screenshots. New C# `.uid` files are Godot metadata, not new gameplay systems.

See [architecture, migration, player guide and remaining limits](FURNITURE_SYSTEM.md). Legacy farming/native/network source files remain unchanged.
