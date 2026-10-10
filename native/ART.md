# Seasons Together — original pixel artwork

The production artwork is created specifically for this project in `FarmArt.cs`. Attached farming game screenshots informed the visual direction only; their pixels, characters, buildings and proprietary assets are not included in the game.

The art direction uses a restrained palette of sage greens, terracotta roofs, honey-coloured wood, cream paper, desaturated blue water and warm harvest colours. Outlines, highlights, material textures and contact shadows use the same visual language throughout.

The renderer generates a 2048×1024 premultiplied-alpha sprite atlas at startup. It contains four seasonal tree palettes with variations, cottage and shop, soil moisture variants, eight crops at four growth stages, four character palettes with four directions and eight walk frames, all seven tools, HUD/weather icons, props, soft shadows and window glow. Four seasonal ground layers contain deterministic grass textures, flowers, stones, paths, fences and the pond.

These are the actual production sprites, not placeholder images. They are regenerated from source when building/running the game, with no third-party art downloads or unavailable asset dependencies. `--visual-test` exports the atlas and screenshots for inspection.

Cloudflared remains the only separately redistributed third-party binary and retains its Apache-2.0 license. Windows supplies the Tahoma font used for Thai UI text; no font files are redistributed.
