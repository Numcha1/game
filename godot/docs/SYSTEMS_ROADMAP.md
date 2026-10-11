# Premium systems roadmap and feature status

Working branch: `feature/premium-game-systems`. Original base `godot/vertical-slice`: `ecf7e3276197821d093b163113468383366aabd7`. Verified modular core/premium work (`af3ee6c`) was merged using checkpoint `3f3ffd1` before additional systems changes. History, Native/dist and real player profiles were preserved.

Status vocabulary: **NOT STARTED** = no working implementation; **IN PROGRESS** = partial implementation or missing required acceptance; **IMPLEMENTED** = callable code; **TESTED** = named executable checks passed within their stated scope; **RELEASE READY** = actual playable package passed release acceptance. Nothing in this roadmap is currently RELEASE READY.

| Milestone | Status | Existing work / next acceptance |
| --- | --- | --- |
| 1 Architecture and save reliability | TESTED | Four conceptual layers, read-only views, shared original rules, bounded atomic save worker, strict save/undo contract, tracked scene persistence and local protocol compatibility. See `SYSTEMS_MILESTONE1.md`. |
| 2 Existing farming loop | IN PROGRESS | Original actions and eight-crop regression matrix are TESTED; rendered loop, feedback, onboarding and confirmations tested. Extended player acceptance and balance documentation remain. |
| 3 Godot co-op integration | IN PROGRESS | Application NetworkService, HTTP adapter, timeout/reconnect/UUID retry and 2/4-process local tests. Godot room routing, remote actors and online session binding still required. |
| 4 Advanced farming/economy | NOT STARTED | Approve meaningful mechanics and tested balance first; define explicit new-state migration/protocol compatibility before persistence fields change. |
| 5 First exploration region | NOT STARTED | Handcrafted region plus one full gather/inventory/production/save/co-op loop required. |
| 6 Connected exploration world | NOT STARTED | Depends on region and co-op acceptance; stable object IDs and persistence required. |
| 7 Full experience polish | IN PROGRESS | Existing premium typography/art/UI/audio/effects integrated; artist, Thai-reader and sound-mix acceptance open. |
| 8 Full regression and Windows release | IN PROGRESS | Local builds/regressions verified; extended gameplay/Internet/clean-machine/export gates open. No production release is published. |

| Feature | Status | Evidence / boundary |
| --- | --- | --- |
| Layer ownership / single farming authority | TESTED | ArchitectureChecks; Domain/Application free of Godot/WinForms/graphics; `native/World.cs` remains authoritative. |
| Original complete Version 1 save/load | TESTED | Native reader round trips, older optional effects, UTF-8/16/32 BOMs and paired identity fixtures. |
| Unknown/ambiguous/incomplete save protection | TESTED | 28 additional contract checks; live/import/undo failures preserve original bytes. |
| Background atomic save, backup and import recovery | TESTED | Coalescing, disk failure, retries, profile lease, paired undo, recovery UI; no power-loss certification. |
| Pending manual/settings save on scene teardown | TESTED | 50 immediate teardowns with queued quality choices/manual saves; lifecycle now 350 checks. |
| Plant/water/grow/harvest/rotation/disease/weather | TESTED | Original matrix and rendered smoke; no new farming values. |
| Seasons/time/harvest windows/fertilizer/stakes | TESTED | Original rules and regression fixtures. |
| Crop quality and original yield | TESTED | Existing quality/yield retained; per-item quality tiers/pricing are NOT STARTED. |
| Soil types/nutrient inventory/sprinklers/AOE tools | NOT STARTED | No invented balance values or unused manager scaffolding. |
| XP/tool upgrades/farm expansion/automation | NOT STARTED | Requires approved progression definitions, balance and migration. |
| Crop inventory/stable identifiers/descriptions | TESTED | Original eight crop IDs and counts, application read models, shared cards and daily refresh. |
| Capacity/equipment/stack rules/storage/transfers | NOT STARTED | No new restrictions imposed on old bags. |
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

User QA preference: test several instances locally first; do not certify Internet multiplayer from loopback results. Next implementation priority is to finish existing-loop acceptance and bind one authoritative room session into Godot before advanced persistent mechanics or region expansion.
