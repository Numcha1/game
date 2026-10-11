# GDD 1.1 implementation baseline

Design source: [unaltered user GDD](GDD_V1_1.md). Working branch: `feature/premium-game-systems`. This specification supersedes the earlier systems roadmap's co-op-first ordering; existing reliability and protocol work is retained.

## Locked boundaries

Single-player completion precedes Godot co-op integration. No combat, hostile AI, weapons, death penalties or mandatory survival meters. Romance/marriage require separate scope approval. Thai is primary; English localization is planned. The eight original crop IDs, 24 initial plots, four 14-day seasons and `native/World.cs` remain the rules baseline. Native, Version 1 saves, protocol and current artwork are preserved.

## Delivery order

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

The current farm is a playable foundation, not a completed authored Moonroot region. The 7 regions, 12 NPCs, 24 crops and other counts in GDD section 14 are **planned targets**, never current feature counts. No placeholder scenes/items/NPCs were introduced to inflate coverage. No milestone is RELEASE READY.

## This implementation: daily sale preview

`Core/Application/SaleQuote.cs` and `GameReadModel.QuoteSale()` expose detached, stable-ID-sorted inventory counts with the original Farm daily unit prices and a widened display total. The quote owns no price, growth or selling rules. `FarmHud.RequestSale()` presents Thai per-item prices/quantities and total before confirmation; both E and the bag route through it. Closing cancels without changing resources. If the day, inventory or unit prices change before confirmation, a fresh estimate requires another confirmation. Repeated UI signals submit at most once for that dialog.

Authority: the UI estimate is advisory. The existing GameSession sell command validates and executes against Farm, including distance, available produce and current price; no resources are reserved by a quote. Future remote presentation must request/confirm against authoritative state, not trust client estimates. No network fields, gameplay balance or persistent quote/tutorial state were added. Quote arithmetic does not change the legacy integer transaction implementation; expanded economy bounds belong to a separately tested rules change.

Changes: application read model and quote; HUD and input routing; ApplicationChecks and rendered Premium checks; GDD/source-status docs and test evidence. Save v1/schema and Native files unchanged.

## Acceptance boundaries

Performance target follows GDD: cap 60, mean frame <=17 ms, P95 <=18 ms on i5-12400F/RTX 3060. Existing 24-crop storm fixture is useful evidence but does not cover future dense expansions, NPCs or region streaming. Numerical measurements and executable results are recorded after validation below.

User QA preference remains multiple local instances first. Existing 2/4-process tests cover application/HTTP protocol, not four rendered Godot players or cross-home Internet. Human onboarding acceptance, five new-player sessions, three >=2-hour sessions, accessibility review, and a standalone EXE/clean-machine test remain open. A launcher requiring Godot/SDK is not a standalone export.

## Executed verification (2026-10-11)

`verify.ps1 -Rendered -ExtendedLifecycle`: Godot and Native builds passed with zero warnings/errors; Core 238, Native 35, startup 60, recovery UI 10, rendered gameplay 101, rendered Premium 180, extended lifecycle 350. Separate headless Premium run: 52 passed. Zero failed checks. Core includes unchanged local 2/4 OS-process protocol concurrency/retry checks. Five new Godot sale checks cover displayed estimate/no mutation, cancellation, day change, repeat confirmation and emptied inventory. Screenshot: `reports/sale-preview.png`, inspected for readable Thai and uncropped controls. Tests use isolated profiles; no existing player save is overwritten.

[Measured before/after](GDD_MEASUREMENTS.md) documents the finite fixture and limitations. Compatibility: Native source/dist unchanged; original eight-crop rules, save Version 1 and network wire protocol unchanged. Remaining issues are the open milestone/release gates listed above; automated success does not complete human onboarding acceptance or the entire GDD.
