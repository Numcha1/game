# SEASONS TOGETHER — GAME-WIDE VISUAL SCALE, ENVIRONMENT DEPTH & COMPOSITION OVERHAUL

Repository: https://github.com/Numcha1/game
Target branch: feature/premium-game-systems
Engine: Godot 4 .NET / C#
Visual direction: Premium HD Pixel Art, Cozy Fantasy Farming Simulation

## ROLE

Act as a Senior Technical Artist, Pixel Art Director, Godot Rendering Engineer, Environment Artist, Level Designer, Camera Programmer and UI/UX Engineer.

## MAIN OBJECTIVE

Improve the visual consistency, object proportions, environmental depth, spatial composition and readability of ALL existing playable game scenes.

The final result should feel like one professionally art-directed game.

Do not focus only on the indoor shop.

Pay particular attention to:
- Character-to-object proportions
- Furniture dimensions
- Building and door sizes
- Indoor room proportions
- Outdoor environmental scale
- Floor and terrain appearance
- Environmental layering
- Lighting and shadow consistency
- Camera composition
- Pixel density
- HUD balance

This task is about cohesive visual quality, not simply increasing the size of every sprite.

## PHASE 1 — AUDIT THE CURRENT BRANCH

Inspect the actual implementation before changing anything.

At minimum inspect:

- godot/project.godot
- godot/Scenes/
- godot/Presentation/
- godot/Presentation/World/FarmWorldPresenter.cs
- godot/Presentation/Camera/PixelCameraFollower.cs
- godot/Presentation/World/FarmScene.Buildings.cs
- godot/Presentation/UI/FarmHud.cs
- godot/Scenes/Interiors/Farmhouse.tscn
- godot/Assets/
- Existing village and building scene implementations
- Asset region metadata and texture import settings

Do not assume that previously planned features or scenes have been implemented.

Build an inventory of existing playable scenes and their major visual assets.

Capture baseline screenshots from actual rendered scenes where possible.

Identify:
1. Incorrect sprite proportions
2. Inconsistent pixel density
3. Oversized or undersized objects
4. Excessive empty space
5. Overcrowded scenes
6. Poor foreground/background separation
7. Incorrect visual depth
8. Weak lighting and shadows
9. Unbalanced camera framing
10. Inconsistent UI scale

## PHASE 2 — CREATE A CONSISTENT WORLD SCALE STANDARD

Establish a reusable visual scale specification.

Use the existing 32-pixel world tile size as a starting reference, not an absolute constraint on sprite dimensions.

Measure the actual character sprite assets first.

Define proportional ranges for:
- Player characters
- NPCs
- Doors
- Beds
- Chairs
- Tables
- Cabinets
- Shop counters
- Shelves
- Windows
- Farm tools
- Storage chests
- Trees
- Rocks
- Houses
- Workshops
- Decorative props

Use human-scale relationships where appropriate.

For example:
- Doors must visually accommodate the character.
- Chairs must match the seated character.
- Counters must be usable from the character's position.
- Beds must accommodate the character's body.
- Paths must support the game's actual collision and movement requirements.

Do not blindly apply a fixed multiplier to all objects.

Avoid arbitrary fractional sprite scaling when it creates inconsistent pixel sizes.

Prefer preparing correctly sized source art or appropriately baked runtime sprites where necessary.

Do not alter collision or interaction coordinates without updating and testing dependent systems.

## PHASE 3 — ENVIRONMENTAL DEPTH

Make each scene visually layered and spatially convincing.

Use appropriate combinations of:

- Ground base
- Ground texture variations
- Paths and surface transitions
- Contact shadows
- Ground-level decorations
- Midground objects
- Character and furniture Y-sorting
- Foreground occlusion
- Window lighting
- Ambient shading
- Subtle atmospheric effects

Ensure characters pass correctly in front of and behind relevant objects.

Avoid incorrect depth ordering, floating furniture and shadows that do not match object anchors.

Maintain crisp pixel art.

Do not use excessive bloom, blur, gradients or particle effects to disguise weak composition.

## PHASE 4 — FLOOR AND TERRAIN COMPOSITION

Improve floor and terrain presentation across all scenes.

For indoor scenes:
- Correct floor plank proportions
- Consistent floor tile scale
- Natural variations in wood tones
- Clean wall-to-floor boundaries
- Appropriate furniture contact shadows
- Readable walkable space
- Room composition with purposeful negative space

For outdoor scenes:
- Natural grass variations
- Soil and path transitions
- Consistent terrain texture scale
- Proper shoreline and water edges
- Logical placement of vegetation
- Readable collision boundaries
- Environmental storytelling

Avoid repetitive, visibly tiled textures and obvious abrupt seams.

Do not randomly scatter decorations.

All objects should contribute to readability, worldbuilding or atmosphere.

## PHASE 5 — INDOOR COMPOSITION

Review every implemented interior.

Evaluate:
- Room dimensions
- Furniture arrangement
- Character visibility
- Walking paths
- Counter positions
- Door positions
- Interior focal points
- Lighting
- Empty floor distribution

For Mira's Seed Shop, specifically:
- Verify counter and shelf scale relative to Mira and the player.
- Improve visual grouping of merchandise displays.
- Reduce excessive visually empty space if confirmed.
- Preserve functional access to the counter and exit.
- Improve lighting from windows.
- Ensure the floor texture has a consistent perspective and pixel density.

For the farmhouse:
- Ensure bed, storage, doors, windows and furniture have consistent proportions.
- Improve room composition without sacrificing future decoration space.

Do not simply fill empty areas with meaningless objects.

## PHASE 6 — OUTDOOR COMPOSITION

Review the current farm and village scenes.

Improve:
- Building scale
- Tree scale
- Walkway widths
- Environmental spacing
- Object alignment
- Foreground/background layering
- Contact shadows
- Terrain texture variation
- Water and shoreline transitions
- Clear navigation landmarks

The farm must remain readable during active farming.

Preserve access to all existing plots and interactable objects.

## PHASE 7 — CAMERA AND RENDERING

Audit the current 800x450 logical viewport, integer scaling, pixel snapping and camera implementation.

Do not change the project's rendering resolution without measured justification.

Define appropriate indoor and outdoor framing profiles.

Requirements:
- No sudden perceived character-size jumps between zones
- No camera jitter
- No visible area outside the scene boundary
- No blurry sprites
- No seam artifacts introduced by scaling
- No inconsistent pixel sizes
- No broken spawn positions
- Smooth camera motion compatible with pixel-perfect rendering

Evaluate zoom using actual rendered screenshots.

Do not assume a larger zoom value automatically produces a better scene.

## PHASE 8 — UI VISUAL HIERARCHY

Keep the interface cohesive with the game environment.

Improve:
- HUD proportions
- Font hierarchy
- Button consistency
- Safe margins
- Contextual prompts
- Tooltip placement
- Inventory presentation
- Thai text readability

Avoid oversized HUD elements obscuring attractive scenery.

Support existing UI scale and text scale settings.

## PHASE 9 — ORIGINAL ART ASSETS

Preserve the original fantasy pixel-art direction.

Where assets have unsuitable proportions:
- Rework or re-bake the original asset.
- Preserve pixel density.
- Correct sprite anchoring.
- Verify transparency.
- Update atlas regions when necessary.
- Retain source asset provenance.

Do not stretch low-resolution assets to compensate for missing artwork.

Do not substitute copyrighted sprites from reference games.

If appropriate original artwork cannot be produced, document the remaining art task rather than falsely claiming it is complete.

## PHASE 10 — PERFORMANCE

Preserve performance targets for the existing Windows test hardware.

Avoid:
- Unnecessary full-screen shader passes
- Expensive per-frame texture regeneration
- Excessive draw calls
- Unbounded particle systems
- Unnecessary scene rebuilding
- Frequent runtime sprite resampling

Use existing quality presets and reduced-motion support.

## PHASE 11 — TESTING AND QUALITY GATES

Test actual playable scenes.

Minimum resolutions:
- 1280x720
- 1600x900
- 1920x1080

Capture before/after images from identical:
- Scene
- Camera position
- Window size
- Time of day
- Weather state

Verify:
- Character/object proportions
- Furniture placement
- Floor and wall alignment
- Camera framing
- Y-sorting
- Lighting
- Pixel sharpness
- UI overlap
- Collision
- Interactions
- Scene transitions
- Save/load compatibility
- Stable frame times

Clearly distinguish actual engine screenshots from mockups.

## IMPLEMENTATION ORDER

Milestone 1:
Visual audit, baseline screenshots and scale specification.

Milestone 2:
Mira's Seed Shop visual proportions, floor composition, object sizes, camera and UI.

Milestone 3:
Farmhouse and existing interiors.

Milestone 4:
Farm and village exterior environments.

Milestone 5:
Reusable environment presentation rules, remaining existing scenes, visual regression tests and documentation.

Do not attempt to redesign all scenes at once.

Complete and test one representative interior and one exterior before propagating changes.

## NON-NEGOTIABLE CONSTRAINTS

- Preserve the game's current mechanics.
- Preserve all existing saves.
- Preserve native game compatibility.
- Preserve NPC and door interactions.
- Preserve inventory and shop behavior.
- Keep the project buildable.
- Do not switch engines.
- Do not introduce paid dependencies.
- Do not rebuild the entire architecture.
- Do not claim artistic acceptance solely from automated tests.

## EXPECTED DELIVERABLES

1. Existing-scene visual audit
2. Documented world-scale specification
3. Corrected sprite and object proportions
4. Improved interior and exterior composition
5. Improved environment depth and lighting
6. Before/after comparison screenshots
7. Rendering and gameplay regression results
8. List of changed files
9. Remaining art limitations
10. Incremental commits

## START NOW

Inspect the current branch and its actual implemented scenes.

Measure existing asset dimensions and visual inconsistencies.

Begin with the Seed Shop as the first representative interior, then improve the farmhouse and farm exterior.

Make real code and asset changes, not only documentation.

Stop at the agreed milestone boundaries for verification before continuing.