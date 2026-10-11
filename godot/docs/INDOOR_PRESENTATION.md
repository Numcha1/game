# Indoor scale and visual hierarchy

Baseline: `9422cc9`, branch `feature/premium-game-systems`. Scope is presentation only. Core/native rules, save documents, textures, room geometry and interaction distances are unchanged.

## Actual scene audit

The playable seed shop uses the 768 x 512 room atlas cell, a retained FarmerController and two reusable NpcActor nodes (only the resident in this region is visible). Characters had approximately 44 logical-pixel visual height, room/furniture art was authored at the existing scale, and the camera zoom was 1.0. The original top-left panel resolved to 63 pixels high, despite its 54-pixel minimum, because font metrics, row gaps and padding grew it. Top buttons were 32 pixels high; the menu glyph had a narrower width. The room-wide fallback prompt repeated F/Esc help even away from an interaction target and occupied a high central zone. Status and help competed with actors for attention.

Before/after measurements are recorded by the actual Godot scene in `reports/indoor-layout-before.json` and `indoor-layout-after.json`. Windows were set to 1280 x 720 and 1920 x 1080. Their logical engine viewport is 800 x 450. Integer stretching produces engine captures of 800 x 450 and 1600 x 900 respectively; these are not native-resolution desktop screenshots.

## Reusable rules

`Presentation/PresentationScale.cs` defines the default logical-pixel tokens. Existing display text/UI scaling still applies.

| Rule | Before | After |
| --- | --- | --- |
| Enclosed-room camera | 1.0 | 1.25 |
| Player/NPC visual scale relative to existing art | 1.0 | 1.05 |
| Combined character screen presence | 1.0 | 1.3125 (+31.25%) |
| Resolved status panel height | 63 | 48 (-23.8%) |
| Top action height | 32 | 28 |
| Top action minimum width | 110 / 110 / 48 | 96 / 96 / 96 |
| Status/top action font | existing theme default 14 | 12; accessibility options add 2/4 |
| Prompt font | existing theme default 14 | 12 |
| Safe margin / horizontal padding / action gap | assorted | 16 / 8 / 8 |

Compact styles use 4-pixel vertical padding and 2-pixel status-row spacing to preserve Thai ascenders/marks without making the top bar tall. Larger accessibility text can grow controls; fixed clipping is not imposed. Shared menu typography/styles are preserved: only top HUD controls receive compact overrides.

The camera profile is selected by enclosed building type. The outdoor farm and outdoor village retain 1.0 zoom and original character sizing. Camera damping and nearest-filter pixel snapping remain. Limits use real room bounds; a viewport coverage fallback raises zoom if a future logical viewport would exceed those bounds. Resize does not rebuild the room or reload textures.

Only Sprite/held-tool presentation is scaled. FarmerController's root position/scale, simulation movement, hitbox/collision rules and interaction radii are preserved. Player sprite and tool offsets scale with visuals, retaining the existing foot anchor. NPC atlas offsets remain unchanged while their visual scale and name offset follow the same indoor rule. Furniture/background bitmap size is unchanged, so characters are only 5% larger relative to furniture, while camera focus supplies the rest of the screen-presence gain.

## One contextual prompt

The existing door prompt becomes the single interaction prompt: nearest NPC first, then a child doorway, room exit, bed or chest. It is empty when no target is active. Indoors it sits 16 pixels from the bottom in a 28-pixel zone; outdoors it stays above the existing hotbar. The generic room-wide F/Esc message is removed. Menu access remains clear on the equal-sized `เมนู Esc` top control.

Modal opening clears the prompt synchronously, rather than waiting for a later process frame. Fresh action feedback uses the same indoor bottom zone and suppresses the contextual prompt while visible at that target. Walking to a different target replaces old feedback with the new prompt. Opening a modal clears old feedback. Prompt selection uses the existing authoritative proximity queries; no duplicate distance/interaction rules are introduced.

## Executed verification

See `reports/indoor-layout-after.json` for the final actual assertions and `INDOOR_VALIDATION.md` for complete-run results. The focused rendered diagnostic is launched with `--npc-checks --indoor-layout-checks` and is included in `verify.ps1 -Rendered`.

It enters the real seed shop, asserts actual window resolutions, takes before/after captures, checks default and maximum Thai text/UI sizes, top-control overlap, NPC/player visual scale, stable standing camera, room coverage, pixel-snapped actor position, retained foot offset, original collision rejection, real D-key walking/animation, resident interaction, prompt hiding, no-target silence, player visibility across left/right/bottom room positions and outdoor profile restoration. Profiles are isolated; existing saves are not overwritten.

Actual captures: [before 720](../reports/indoor-before-1280x720.png), [after 720](../reports/indoor-after-1280x720.png), [before 1080](../reports/indoor-before-1920x1080.png), [after 1080](../reports/indoor-after-1920x1080.png).

## Changed-file responsibilities

- `Presentation/PresentationScale.cs`: shared presentation tokens.
- `Presentation/Camera/PixelCameraFollower.cs`: dedicated enclosed-room zoom, coverage and bounds.
- `Presentation/Player/FarmerController.cs`: visual-only character/tool scaling and offsets.
- `Presentation/Village/NpcActor.cs`: indoor resident visual scale and name placement.
- `Presentation/UI/FarmHud.cs`: compact top HUD styles/text, equal controls, feedback/modal prompt coordination.
- `Presentation/World/FarmScene.Buildings.cs`: profile selection and single contextual prompt.
- `Diagnostics/FarmScene.IndoorChecks.cs`: real scene/resolution/interaction checks and captures.
- `Diagnostics/FarmScene.NpcChecks.cs`: isolated focused diagnostic routing.
- `verify.ps1`: runs the focused check in rendered verification.
- Documentation, existing regression reports/captures and four before/after captures record evidence. New C# UID files are generated by Godot import.

## Remaining limits

The pre-existing unidentified ObjectDB exit warning remains open; this presentation task does not claim to fix or certify resource leaks. The room artwork still has a generous open centre; this change focuses it with the camera rather than rebuilding furniture or collision. NPC dialogue/shop modals retain their existing design. Long-session, ultrawide, mobile/touch, native-resolution desktop capture and Windows export acceptance are outside this verified scope. Human Thai readability/playtest remains useful despite font coverage and layout assertions.
