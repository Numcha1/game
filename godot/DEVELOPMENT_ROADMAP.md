# Development Roadmap

Priorities follow risk and measured evidence, not an invented release deadline. QUALITY_GATES.md defines acceptance.

| Priority | Work | Current evidence / remaining gate |
|---|---|---|
| P0 | Save identity, paired import recovery, profile locking, explicit backup recovery | Implemented; isolated core, independent-process and actual UI-reload tests |
| P0 | Standalone Windows release | Pending export templates, packaging and clean-machine run without editor/SDK |
| P1 | Plot inspection, forecast/journal, destructive confirmations | Implemented; real rendered input/cancel/confirm/resize checks |
| P1 | Crop/season/core action regressions | All eight crops across four seasons; harvest-to-sale cycles and original-rule comparisons |
| P1 | Onboarding and keyboard/accessibility | Contextual help available; guided onboarding and fresh-player acceptance pending |
| P1 | Godot room host/join, remote actors, reconnect and shutdown | Pending; first harden original nullable request and server lifecycle boundaries |
| P1 | Internet co-op acceptance | Pending independent machines/homes, four clients and reconnection tests |
| P2 | Production growth/tool/UI art and weather polish | Pending dedicated original assets and four-season/action review |
| P2 | Music, ambience, action audio, buses and settings | Pending licensed production assets and lifecycle/crossfade/mute verification |
| P2 | Soak/localization/accessibility | Pending extended gameplay runs on accepted i5-12400F / RTX3060 target |
| P3 | NPCs/animals/crafting/progression/regions | Deferred until core and co-op gates pass; integrate one complete module at a time |

Next: onboarding/accessibility, server boundary hardening, Godot co-op integration after core regression, production art/audio, evidenced balancing and export/clean-machine acceptance. Local builds and Core CI are not a substitute for full engine/release CI.

This iteration uses separate tested save, presentation and documentation/verification commits. The rendered test initially exposed a stale plot inspector; plot events now refresh it immediately. Scene diagnostics exposed missing-node and invalid-data startup handling, now controlled. Long-term durability and leak certification remain outside these finite tests.
