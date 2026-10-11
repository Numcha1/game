# World composition audit and scale contract

Baseline `adb7d6c`, branch `feature/premium-game-systems`. Actual playable regions: farm, village, player farmhouse, Mira seed shop, Rowan workshop, Mira home and Rowan home. Boot/title, inventory/shop/storage/dialogue/settings are overlays, not additional implemented world scenes. No forest/town expansion is assumed.

## Measured assets and audit

- World tile: 32 logical pixels; viewport: 800 x 450 with existing integer window stretching and nearest filtering. No resolution change is proposed.
- Player atlas cells: 40 x 56; first down-frame alpha bounds `(5,2)..(34,53)`, visible 30 x 52 pixels. The earlier approximate 44-pixel player assumption was inaccurate.
- Original NPC sheet: 1774 x 887, 8 x 4 cells; fractional cell dimensions and runtime reduction created a different pixel density. Its new preparation target is 40 x 56 cells, alpha height 52, common foot alignment. Original source remains available.
- Village room atlas: four 768 x 512 cells. Farmhouse: 1536 x 1024 shown at half size. Both contain painted furniture, shadows and floors. Many floor boards read roughly 20 logical pixels tall; furniture and the open centre dominate the characters.
- Indoor camera 1.25 and sprite scale 1.05 caused a 31.25% perceived actor-size jump at doors. World-wide composition will use one character size and a 1.0 camera, with room-specific limits and textured border padding instead of arbitrary sprite scaling.
- Farm atlas props are individually reduced with fractional ratios, while crops/terrain already use logical source sizes. Props will be cached at intended logical dimensions, preserving feet and Y sorting.
- Farm ground/path variants and pond/wind already exist. Decoration is currently seeded randomly, and all tree shadows use one footprint despite different crowns. Replace with named, purposeful clusters and proportional foot shadows.
- Village room furniture is painted into the background, without actor-level foreground ordering. Existing farmhouse foreground rectangles can obscure empty floor inside their crops. Explicit silhouette meshes are required for limited, useful occlusion.
- UI is already compact after the prior task: resolved status 48px, three 28px buttons, contextual prompt. Keep those improvements and existing Thai fonts/accessibility.

Baseline diagnostics captured all seven areas at 1280 x 720, 1600 x 900 and 1920 x 1080, locked to minute 600, spring/day 1, fair weather, documented actor/camera positions. `reports/composition/before-all.json` passed 90 actual entry/resolution/visibility/layout/stability assertions. Engine screenshots are distinct from mockups and from native-resolution desktop captures.

## Scale ranges (logical pixels, measured at camera 1.0)

| Element | Preferred visible range | Rule |
| --- | --- | --- |
| Player / playable NPC | height 50-54, body width 26-36 | one foot anchor; 40 x 56 prepared cells |
| Door opening | width 32-40, facade height 48-60 | must fit character; painted openings need separate art acceptance |
| Bed body | width 48-64, length 80-110 | remaining blocked cluster may include rug/stand |
| Chair | width 24-36, height 32-48 | seated body must fit; no seat mechanic invented |
| Table | width 48-96, depth 32-56 | feet/contact shadows anchor to floor |
| Cabinet / shelf / counter | depth 32-56, width 48-128 per unit | preserve blocking clusters and service approach |
| Window | width 32-56, height 40-64 | facade plane; night panes and floor light agree |
| Held tool | authored 32 x 32 | preserve action animation and grip |
| Chest / shipping crate | width 32-56 | not larger than a small workstation |
| Tree | crown width 80-96, height 88-112 | trunk foot stays fixed; crown may occlude actor |
| Rock / flowers / small props | width 16-32 | deliberate borders/landmarks, never plot clutter |
| Farm building | width 128-160 | complete footprint; painted doorway is a separate check |
| Village building | retained authored facade | do not resize doorway coordinates invisibly |
| Walkable aisle | minimum 48-64 visible width | existing collision/navigation is the authority |

These are art review ranges, not assertions that every painted prop already satisfies them. Source generation can preserve layout while failing exact proportions; remaining discrepancies must be named in the final art report. Do not claim artistic acceptance from numeric tests alone.

## Layer and safety contract

Ground, paths and rugs are below actors. Contact shadows anchor to feet. Blocking furniture uses authored silhouette meshes in the Y-sorted Actors group; wall/window atmosphere is above ground and below actor faces. Existing plot/door/bed/chest/service coordinates and Domain rules remain unchanged. No decoration blocks a currently valid save position. If a later change needs a new obstacle, it needs explicit save-position and navigation testing before shipment.

Prepare/crop/resample images once, cache them and retain original provenance. Never regenerate textures in Process. Quality/reduced-motion must still gate existing effects. Verify the seed shop and one farm exterior before activating presentation changes across remaining scenes.
