# Premium presentation audit

Scope: `feature/premium-polish`, based on `godot/vertical-slice` (`ecf7e327`) with the verified production-core work merged before presentation changes. Reviewed scenes, C# composition, shared assets, shaders, imports, input, Core/native rules, saves, protocol adapters, and rendered diagnostic evidence on 2026-10-11.

Severity describes the original issue or remaining release risk; status distinguishes implementation from acceptance. No critical rendering defect was observed in the finite automated workload. This is not an exhaustive commercial certification.

| Severity | Finding and source | Change / remaining gate |
| --- | --- | --- |
| High | `Presentation/UI/FarmHud.cs` depended on installed Tahoma/SystemFont; deployment could lose Thai coverage. | Bundled licensed Mitr/Sarabun in `Assets/Fonts/`; shared `FarmTheme.tres`, cached size variants, Thai coverage and missing-font checks. Complex Thai shaping still needs native-reader acceptance. |
| High | `project.godot` stretched a low-resolution viewport, also pixelating Thai UI. | Integer `canvas_items` scaling with nearest world sprites, high-resolution dynamic fonts and pixel snapping. Letterboxing is deliberate; see display table in the polish report. |
| High | Ground/detail draw order in `Scenes/Farm.tscn` obscured the pond and negative-Z contact shadows. | Ground -10, details -5, shared soft shadows under props and actors; visible day/storm screenshots. |
| High | `Infrastructure/Assets/AssetCatalog.cs` and `FarmWorldPresenter.cs` used generic seedlings / resized mature foliage for growth. | Original eight-species, four-stage growth atlas, anchored stages and validated atlas bounds. Old unused sprout/young regions remain for legacy asset compatibility, not the active plot renderer. |
| High | No title, pause menu or visible resource-loading flow. | `FarmHud` title/pause routing; `Scenes/Boot.tscn`, `FarmBoot`, and `FarmPreloader` poll actual threaded loads with timeout/error UI. Save recovery tests rerun through the new boot scene. |
| High | Godot Internet co-op and standalone export acceptance are absent. | Existing Native game and protocol preserved. Godot remains a single-player slice; no new network UI implying working co-op. Separate integration/export scope remains. |
| Medium | Hotbar repeated generic props; farming gestures lacked recognizable held tools. | Original seven tool icons, cached four-pose tool overlays, anticipation/recovery and idle breathing. Existing walking atlas retained; body/arm animation requires frame-by-frame artist review. |
| Medium | Large text could wrap short Thai header buttons vertically; fixed inspector could overlap expanded status. | Explicit header widths, wrapped body text, inspector follows status height. All four resolutions × three text sizes × three UI sizes checked. |
| Medium | Bag/shop were repeated text buttons with stale open-menu state after a day change. | Shared `Scenes/UI/ItemCard.tscn`; icons, descriptions, selected/focus states, event-driven inventory/day refresh. Original planting price charged only by the original command. |
| Medium | Rain/seasonal effects were shallow; sale lacked coin feedback. | Bounded reusable pond rings/splashes/leaves/fireflies/snow, layered rain, cached coin/harvest particles; Low/Medium/High budgets and reduced motion. |
| Medium | No action-confirmation audio. | Five original synthesized WAV cues, eight reused voices, dedicated Effects bus, independent volume setting. Listening/mix acceptance remains. |
| Medium | Diagnostics measured frame pacing but not renderer CPU/GPU costs. | Godot renderer timing enabled for quality sampling; process, managed memory and draw calls recorded. Original baseline lacks GPU/CPU costs, so no fabricated speedup comparison. |
| Medium | Decorative terrain, pond edge and fractional-size older props are not uniformly artist-approved. | Existing valid assets retained; no reference-game pixels copied. Hand-authored shoreline/pixel-density pass remains before premium artwork signoff. |
| Low | Theme styles were rebuilt per UI control; resource caching responsibilities were mixed. | Shared Theme style resources, cached typography variants, atlases, frames, sound streams and item scene. Resource loads confined to Infrastructure. |
| Low | Tutorial existed only as a modal help paragraph. | Dismissible day-one guide beside the HUD; no input blocking or rule mutation. A full contextual quest/tutorial system is outside this presentation pass. |

Priority order followed: typography and display → original growth/tools → feedback/audio/theme → item cards → title/pause → threaded loading → guide → full verification and evidence. Each major module was committed after actual build/regression checks. The production architecture audit remains in `PRODUCTION_AUDIT.md`; this report does not replace its security/save/network findings.
