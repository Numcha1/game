# Seasons Together: Game Design Document

This iteration hardens the existing single-player core. It does not certify the complete production farming RPG.

## Experience and rules

A demanding cooperative farming game for Windows with top-down HD pixel art: plan crops around seasons, water and soil, manage energy and harvest windows, and sell together. Preserve the current visual direction. Content expansion starts only after core and co-op acceptance.

`../native/World.cs` is the authoritative source: 24 plots, eight crops, 14-day seasons, 06:00-21:00 days at 1.5 game minutes per real second, movement at four tiles/second and five energy per farming action. Preserve moisture, fertility, rotation, disease, support, crop quality, daily prices, shared funds and individual inventories. Overnight inventory spoilage remains 25%; greenhouse costs 300. Display metadata comes from original crop definitions, never a second rule implementation.

## Existing loop

1. Move with WASD/arrows, choose seeds using O/shop or Tab. Seed selection is free; planting charges the original price.
2. Hover a plot to inspect water, soil, previous crop, disease and harvest deadline. Choose tools 1-7 and click a nearby plot.
3. Plant, water and maintain crops; harvest within their original windows.
4. Sell with E near the shipping crate; sleep with Space near the house. Confirm clearing crops and sleeping with produce before commands execute.
5. Read original tomorrow forecast and journal through HUD/help/settings. Save/load preserves the same owner.

## Scope and acceptance

Godot currently runs single player. Native retains existing room multiplayer; a tested NetworkService seam does not yet make the Godot scene online. YouTube opens an HTTPS browser link; embedded playback and internal music/ambient/SFX are absent. Dedicated growth/tool/UI art still has gaps listed in ART.md.

Proposed fresh-player acceptance: at least three independent players, 30-minute sessions, record planting-to-sale completion, confusion, failed actions and perceived difficulty. Aim for 80% completing the core loop without developer guidance. This playtest has not happened; automated crop cycles are not evidence of fun or balance.
