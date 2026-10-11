# GDD 1.1 sale-preview measurements

Actual i5-12400F / RTX 3060, Godot 4.7.2 .NET Compatibility, 1600x900 window, cap60. Same 24-ripe-plot storm/evening fixture, 60 warmup and 600 sample frames per quality. Before/after reports: `gdd-v11-before.json` / `gdd-v11-after.json`.

| Quality | FPS before / after | Mean ms before / after | P95 ms before / after | Private MiB before / after |
| --- | --- | --- | --- | --- |
| Low | 60.048 / 60.062 | 16.653 / 16.650 | 16.726 / 16.774 | 212.1 / 209.6 |
| Medium | 60.002 / 60.002 | 16.666 / 16.666 | 16.688 / 16.706 | 216.5 / 214.9 |
| High | 60.002 / 60.002 | 16.666 / 16.666 | 16.689 / 16.710 | 207.4 / 204.6 |

Each quality window satisfies the current GDD mean <=17 ms / P95 <=18 ms gate in this fixture. This does not establish a speedup, future dense-world/NPC performance or long-session leak freedom. This UI feature does not change the normal rendering pipeline. General smoke includes resize/capture overhead and is not the controlled quality benchmark.
