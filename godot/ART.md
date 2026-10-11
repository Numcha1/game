# Original art pipeline and current acceptance status

Current presentation work is on `feature/premium-polish`. See [polish implementation](docs/PREMIUM_POLISH.md), [audit](docs/PREMIUM_AUDIT.md), [measurements](docs/PREMIUM_MEASUREMENTS.md) and real renderer screenshots in `reports/`.

## Runtime assets

Existing original environment/character assets remain: `Assets/props.png`, `characters.png`, `terrain.png`, `pond.png`, `shadow.png`, `glow.png`, `stake.png`, regions JSON and `FarmTiles.tres`. PNG textures use lossless imports, no mipmaps and nearest filtering. Scene and camera coordinates are pixel-snapped; UI fonts use the higher-resolution canvas.

New original growth/tool assets: `Assets/crop-growth.png`, `tool-icons.png`, `tool-poses.png`, `polish-regions.json`. Eight crop species each have four distinct anchored stages; seven recognizable tool icons and four cached held-tool poses replace generic hotbar/gesture visuals. `FarmWorldPresenter` uses the new species-specific stages, not the unused legacy `sprout`/`young-*` regions. Those old regions remain for compatibility only.

## Creation and reproducibility

The imagegen skill used the built-in `image_gen` tool, transparent-background mode, without CLI/API keys or reference-game pixels. Original environment/character sources and exact prompts remain in `ArtSource/` and `ArtSource/prompts.json`. New source sheets: `ArtSource/crop-growth-source.png`, `tool-icons-source.png`; exact full prompts and tool mode are in `ArtSource/premium-prompts.json`.

`Tools/ArtBake` crops/packs the older sheet using curated extraction boundaries (generated objects did not align perfectly to an equal grid). `Tools/PolishBake` validates and packs the new equal-cell source sheets into fixed runtime dimensions and pre-bakes tool rotations. Both use System.Drawing only at build time; neither runs in the game loop. `ArtSource/.gdignore` keeps full source images out of Godot import/export.

Palette: olive/sage foliage, honey highlights, cream UI, copper/russet accents, umber outlines and upper-left illumination. Shared shadows, rain layers, pond rings/splashes, seasonal accents and cached harvest/coin feedback provide depth without full-screen blur. Low/Medium/High and reduced motion bound these effects.

## Fonts and audio

Mitr Medium headings and Sarabun Regular/SemiBold body/control fonts are bundled. No system-installed Tahoma dependency remains. Upstream unmodified OFL licenses, source URLs and SHA256 hashes live in `Assets/Fonts/`. The diagnostic verifies Thai vowel/tone-mark glyph availability; Thai-reader shaping acceptance remains separate.

Five short original synthesized WAV cues in `Assets/Audio/` are reproducible with `Tools/synthesize_audio.py`; no third-party recorded samples. Eight reusable audio voices use the Effects bus and saved volume. These are action cues, not a complete ambient/music soundtrack.

## Remaining artist acceptance

Existing walking frames still need artist inspection and any necessary in-between/arm redraw; held-tool overlays and anticipation/recovery are implemented, not newly hand-drawn full-body action sheets. Terrain/path/fence/pond edges and older fractionally scaled props need a consistent pixel-density/shoreline pass. The native Control theme is cohesive and reusable, but an ornate nine-slice UI atlas is not claimed. Sound mixing and native Thai reading require player review. No unused legacy atlas region is represented as a finished new growth asset; no commercial-quality signoff is implied by automated hashes/layout checks.
