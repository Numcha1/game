# World-wide composition: implementation and acceptance

Branch `feature/premium-game-systems`; baseline `adb7d6c`. This is a tested presentation milestone, with remaining art work explicitly open. [Measured scale specification](WORLD_SCALE.md), [executed validation](WORLD_VISUAL_VALIDATION.md), [original request](WORLD_COMPOSITION_REQUEST.md).

## Implemented scope

| Playable area | Final changes | Remaining visual work |
| --- | --- | --- |
| Farm | Cached logical-sized buildings/crate/trees; proportional contact shadows; 64px main paths; named verge flowers and shore rocks replacing random scatter; compact tool panel | Bespoke organic path/soil transitions and shoreline art; painted door openings |
| Village | Common actor scale, prepared NPC atlas, contact shadows, Y-sorted foliage at sides | Original spring background retained; seasonal background variants and facade proportions |
| Seed shop | Original edited room atlas, purposeful green aisle runner and grouped displays; cached room canvas, shelf silhouettes, subtle window/night lighting | Exact counter/door and plank proportions need further authored art |
| Farmhouse | Original edited background, narrower bed body within retained cluster, aisle runner; silhouette foregrounds instead of opaque rectangles; weather-aware room ambient | Bed still approximately 78x140 logical pixels; preferred range is 48–64x80–110. Floor boards still read about 20 logical pixels, exceeding intended fine pitch |
| Workshop | Edited atlas with work-area rug and grouped benches; bench silhouettes, common room lighting | Fine plank pitch and exact human-scale furniture acceptance |
| Mira home | Edited room atlas, coherent bed/rug/reading grouping; bed/chair silhouettes and lighting | Bed/armchair remain large; original collision clusters are authoritative |
| Rowan home | Edited atlas, bed/rug/fireplace grouping; bed and reading-chair silhouettes, common lighting | Chair/kitchen art and original generic obstacles are not an exact silhouette match |

No new forest/town/interior is claimed. All seven are actual playable regions. Boot and menus are overlays. Domain/native/save/network rules and building coordinates are unchanged. Existing saves are not modified by these test fixtures.

## Reusable presentation rules

Keep 32px tiles and the 800x450 logical viewport, existing integer stretching and nearest imports. Camera and actor scale are 1.0 across zones. Player visible height is 52px; NPCs are alpha-cropped and prepared once into 40x56 cells (320x224 atlas), with common foot alignment. This removes the prior indoor 1.25 camera/1.05 actor multiplication. Room limits remain distinct from outdoor limits. Room border pixels are prepared into a cached 800x512 canvas extending the original 768px room by 16px on each side, rather than exposing outside space or zooming fractionally.

`RoomPresentationAssets.Flat` prepares standalone images for UV sampling. `RoomDepth` anchors authored silhouette polygons at their lowest point inside the same Y-sorted Actors group. Floors/rugs stay behind actors; windows stay below faces. Silhouettes are coarse art outlines, not per-pixel alpha masks. They neither add obstacles nor make a chair usable as a new gameplay object.

`RoomLight` coordinates indoor time/weather tint. Existing farmhouse window panes, lamp and ambience remain; service rooms gain restrained night panes and smaller light accents. Outdoor weather/pond/wind, quality presets and reduced-motion remain. No new fullscreen shader pass or per-frame texture regeneration is added. Prepared textures and atlas views are cached across scene lifetimes.

The compact HUD retains Thai fonts, text/UI accessibility scales, modal routing and contextual prompts. Only the tool-panel spacing/caption hierarchy changes. Inventory/shop behavior is retained.

## Milestone boundaries and fixes

| Gate | Concrete result | Compatibility / open issue |
| --- | --- | --- |
| M1 audit | `ca8c318`: measured assets, seven-scene inventory, scale contract, 21 baseline images; 90 checks passed | No game rules modified |
| M2 representative shop | `b685369`: prepared NPC pixels, room cache, seed shop depth and original room art; seed 19 and NPC 34 checks passed | Visual inspection caught transparent floor from mismatched image formats and wrong UVs from AtlasTexture; fixed by RGBA8 conversion and standalone cached ImageTexture. Added explicit floor-opacity assertion |
| M3 interiors | `d8be7d3`: farmhouse and remaining room art, silhouettes and shared room light; building 49 checks passed | Furniture proportion acceptance remains open |
| M4 exteriors | `b911306`: logical props, paths, shadows, authored clusters, village edge depth; exterior 34 checks passed | Geometric tile transitions remain |
| M5 shared regression | Full build/Core/native/rendered tests, controlled 190-check gallery and finite lifecycle; final image review corrected Rowan chair outline, followed by a rebuilt 190-check rerun | Existing one-instance ObjectDB exit warning remains; no leak-free or full art acceptance claim |

## Source/art provenance and changed files

Built-in image generation edited existing project-owned farmhouse and room backgrounds. No reference-game sprites were imported. Original sources `farmhouse-studio.png`, `rooms-atlas.png`, `npc-atlas.png` remain available. Full final prompts and generation mode: [composition-prompts.json](../Assets/Village/composition-prompts.json). New shipping outputs: [farmhouse-composition.png](../Assets/Interiors/farmhouse-composition.png), [rooms-composition.png](../Assets/Village/rooms-composition.png), [npc-logical.png](../Assets/Village/npc-logical.png). NPC preparation is a development-only Godot utility (`--npc-checks --bake-presentation`), not a gameplay operation. These are implemented assets, not mockups.

| Responsibility | Source files changed/added |
| --- | --- |
| Asset preparation/catalog | `Infrastructure/Assets/AssetCatalog.cs`, `NpcAssets.cs`, `PresentationAssetBake.cs`, `RoomPresentationAssets.cs` |
| Shared size/camera | `Presentation/PresentationScale.cs`, `Presentation/Camera/PixelCameraFollower.cs` |
| Rooms/depth/light | `Presentation/Buildings/InteriorRoom.cs`, `RoomDepth.cs`, `RoomLight.cs`, `Scenes/Interiors/Farmhouse.tscn` |
| Actors/village/farm | `Presentation/Player/FarmerController.cs`, `Presentation/Village/NpcActor.cs`, `VillageRoom.cs`, `Presentation/World/FarmWorldPresenter.cs` |
| HUD | `Presentation/UI/FarmHud.cs` |
| Regression | `Diagnostics/FarmScene.CompositionChecks.cs`, `FarmScene.NpcChecks.cs`, `FarmScene.IndoorChecks.cs`, `verify.ps1` |
| Evidence | `reports/composition/`, refreshed rendered gameplay/building/NPC/indoor/lifecycle reports, associated generated `.cs.uid`/PNG import metadata |

No changes under `godot/Core`, `native` or `project.godot`. No paid packages, engine switch, new gameplay or network protocol. Human artistic acceptance, precise door/furniture proportions, exact foreground silhouette cleanup, fine floor pitch, natural terrain transitions, long-session resource diagnosis and exported Windows acceptance remain separate work. Numeric tests establish runtime behavior; they do not certify that the final art meets every preferred range.
