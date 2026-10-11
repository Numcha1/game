# Premium systems roadmap and feature status

Working branch: `feature/premium-game-systems`. Original base `godot/vertical-slice`: `ecf7e3276197821d093b163113468383366aabd7`. Verified modular core/premium work (`af3ee6c`) was merged using checkpoint `3f3ffd1` before additional systems changes. History, Native/dist and real player profiles were preserved.

Status vocabulary: **NOT STARTED** = no working implementation; **IN PROGRESS** = partial implementation or missing required acceptance; **IMPLEMENTED** = callable code; **TESTED** = named executable checks passed within their stated scope; **RELEASE READY** = actual playable package passed release acceptance. Nothing in this roadmap is currently RELEASE READY.

Current design authority: [GDD 1.1](GDD_V1_1.md) and [implementation status](GDD_IMPLEMENTATION_STATUS.md). The A-H order below supersedes the former numeric/co-op-first plan. Completed systems Milestone 1 remains reliability evidence, not a completed GDD milestone.

| Milestone | Actual status | Required next acceptance |
| --- | --- | --- |
| A Core farming | IN PROGRESS | Original farming/inventory/shop/save tests pass; sale preview implemented. Contextual onboarding, first-time human acceptance and extended balance sessions remain. |
| B Advanced farming | NOT STARTED | Soil/NPK/health/quality tiers, new crops, upgrade/irrigation/expansion progression; define explicit persistence migration and authority first. |
| C Village/relationships | NOT STARTED | Authored village, schedules, conversations, persistent relationships and initial quests. |
| D Exploration | NOT STARTED | Complete forest gathering, transitions, resource persistence, discovery and restoration loop. |
| E Extended simulation | NOT STARTED | Animals, crafting/production, fishing, peaceful mining and magic. |
| F Story/world | NOT STARTED | Remaining regions, story, festivals/restoration and sustainable endgame. |
| G Multiplayer | IN PROGRESS (infrastructure only) | Existing authoritative protocol/2-4 OS-process tests; Godot room/actors, shared progression, reconnect, Internet acceptance remain. |
| H Release candidate | IN PROGRESS (diagnostics only) | Final art/audio, performance, migration, standalone Windows export and clean-machine/human QA. |

| Feature | Status | Evidence / boundary |
| --- | --- | --- |
| Layer ownership / single farming authority | TESTED | ArchitectureChecks; Domain/Application free of Godot/WinForms/graphics; `native/World.cs` remains authoritative. |
| Original complete Version 1 save/load | TESTED | v1 compatibility retained. Godot building state uses a v2 envelope with original nested Farm v1 and preserved pre-migration file; see BUILDINGS.md. |
| Unknown/ambiguous/incomplete save protection | TESTED | 28 additional contract checks; live/import/undo failures preserve original bytes. |
| Background atomic save, backup and import recovery | TESTED | Coalescing, disk failure, retries, profile lease, paired undo, recovery UI; no power-loss certification. |
| Pending manual/settings save on scene teardown | TESTED | 50 immediate teardowns with queued quality choices/manual saves; lifecycle now 350 checks. |
| Plant/water/grow/harvest/rotation/disease/weather | TESTED | Original matrix and rendered smoke; no new farming values. |
| Seasons/time/harvest windows/fertilizer/stakes | TESTED | Original rules and regression fixtures. |
| Crop quality and original yield | TESTED | Existing quality/yield retained; per-item quality tiers/pricing are NOT STARTED. |
| Soil types/nutrient inventory/sprinklers/AOE tools | NOT STARTED | No invented balance values or unused manager scaffolding. |
| XP/tool upgrades/farm expansion/automation | NOT STARTED | Requires approved progression definitions, balance and migration. |
| Crop inventory/stable identifiers/descriptions | TESTED | Original eight crop IDs and counts, application read models, shared cards and daily refresh. |
| Capacity/equipment/stack rules/storage/transfers | IN PROGRESS | Tested starter crop-chest quantity transfers, v2 atomic bag/chest persistence. General equipment/capacity/storage furniture remain open; see BUILDINGS.md. |
| Original selling/daily prices/greenhouse/rescue | TESTED | Original pricing is centralized in Farm; no UI-owned price formula. |
| Crafting/processing/supply-demand/new upgrades | NOT STARTED | Requires a full production loop and co-op transaction acceptance. |
| Existing legacy room wire protocol | TESTED | Loopback HTTP, original Native server, 2/4 independent OS processes, simultaneous harvest and duplicate UUID retry. |
| Application network timeout/reconnect seam | TESTED | Existing timeout/lost-ack/cancellation regressions; does not imply playable Godot online rooms. |
| Godot create/join/invite/remote actor/session UI | NOT STARTED | Current playable Godot scene remains single-player. |
| Internet test across homes | NOT STARTED | User selected local multiple-instance testing until independent machines are available. |
| Village/forest/mine/gathering/world quests | NOT STARTED | Current farm scene preserved; no release-candidate placeholder region added. |
| Thai typography/UI scaling/reduced motion | TESTED | 175 rendered premium checks, four sizes × three text sizes × three UI sizes. Native-reader/DPI acceptance remains. |
| Windows Native game | TESTED | Existing EXE and 35 self-tests preserved. |
| Godot Windows standalone export/clean machine | NOT STARTED | Launcher depends on installed Godot .NET/SDK; export templates/runtime packaging still required. |

User QA preference: test several instances locally first; do not certify Internet multiplayer from loopback results. Next priority is Milestone A onboarding and first-time farming acceptance, followed by B-F single-player systems. Godot multiplayer integration is G; authority contracts are required when each new system is designed.

Building slice: [Milestones 1-2 audit/implementation](BUILDINGS.md), [actual tests/measurements](BUILDINGS_MEASUREMENTS.md). Farmhouse entry/bed/chest/light persistence are TESTED; placement, house upgrades, other interiors and co-op are NOT STARTED.
