# Quality Gates

Current systems statuses: [SYSTEMS_ROADMAP](docs/SYSTEMS_ROADMAP.md); tested persistence changes: [Milestone 1](docs/SYSTEMS_MILESTONE1.md). RELEASE READY remains unassigned.

Current presentation evidence: [premium report](docs/PREMIUM_POLISH.md).

VERIFIED means named checks actually passed. IN PROGRESS means partial evidence. PENDING means required evidence is absent. No production stage is COMPLETE based on code presence alone.

| Stage | Acceptance | Status |
|---|---|---|
| 1 Audit | Inventory, severity/file references, five current documents and measured evidence | IN PROGRESS: baseline and focused audit available; release audit open |
| 2 Core | Crop/action regressions, save compatibility, confirmations, input/resize and fresh-player onboarding | IN PROGRESS: automated checks passed; onboarding acceptance pending |
| 3 Art | Original production PNGs for every required growth/tool/UI element | IN PROGRESS: original four-stage crops and seven tool poses implemented; full-body/terrain artist acceptance pending |
| 4 Audio | Licensed music/ambient/actions, buses, mute/sliders/crossfade/cleanup | IN PROGRESS: original action cues, bus, volume and pooled voices tested; ambient/music/crossfade pending |
| 5 Co-op | Four clients, create/join, remote animation, shared farm, separate inventory, reconnect and two homes | PENDING: Native/local protocol tests do not pass this gate |
| 6 Expansion | Core and co-op accepted, then complete save/UI/test/balance modules | PENDING and intentionally deferred |
| 7 Balance | All-crop simulations and structured real-player evidence | IN PROGRESS: functional cycles, no fun/economic certification |
| 8 Engineering | Build/regression, bounded resources, error recovery and long-session soak | IN PROGRESS: 50 scene teardowns, 500 bag/shop cycles and startup/recovery checks |
| 9 Release | Standalone Windows x64, attribution/instructions, clean-machine QA and full engine CI | PENDING |
| 10 Acceptance | All gates, no known release-blocking crashes, real playtests and Internet QA | PENDING |

## Accepted performance target

User selected the current machine on 2026-10-11: Windows 10 build 19045, i5-12400F, RTX3060 driver610.62. Real Compatibility/OpenGL renderer, 1600x900 window, 800x450 logical viewport, integer/nearest scaling, 60 FPS cap. Workload: storm, 24 ripe crops and evening lights; warm 60 frames and sample 600 per Low/Medium/High. Mean target <=17ms and P95 <=18ms for this workload. Record P99/private/managed memory too. Lower-end support is outside the currently accepted target.

A capped short sample does not certify every frame or long-session performance. Headless tests do not measure GPU performance. Forced-GC lifecycle samples identify finite resource retention, not lifetime leak freedom.

## Test safety

Random GUID profiles only; never overwrite the player's save. Interrupted import tests reconstruct persisted intermediate files rather than physically cutting power. Unsupported schemas and mismatched identities fail closed. Native rules/protocol and original art remain unchanged. A failed module must be fixed and rerun before acceptance.
