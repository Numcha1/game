# Feature Matrix

Current presentation evidence: [premium report](docs/PREMIUM_POLISH.md).

Implemented means callable in the current Godot scene; it does not mean release-accepted.

| Feature | Godot implementation | Evidence / gap |
|---|---|---|
| Move/collision/camera | FarmerController + PixelCameraFollower | physics/input/house/pond + integer resize checks |
| 24 plots, eight crops | original World via GameSession | 8x4 planting comparison and all crop cycles |
| Plant/water/harvest | GameSession + tool hotbar | native rule regression + scene core loop |
| Fertilize/treat/stake/clear | same original commands | core tests; clear cancel/confirm scene check; cached dedicated held-tool poses; full-body artist review pending |
| Soil rotation/quality/disease | original World | rotation/read model and disease tests; inspect HUD |
| Season/time/weather/night | original World + WeatherLighting | four seasons, original tomorrow forecast, quality workload |
| Inventory/economy/shop | Read.Inventory + HUD + original selling | daily prices, all crop sale cycles; select seed charges on planting |
| Greenhouse/rescue | existing original commands exposed in shop | original logic retained; full shop acceptance review pending |
| Sleep/day transition | original command + AnimationPlayer | warning when carrying crops; cancel/confirm preserves original spoilage |
| Save/load/import | SaveService v1 | bounded worker/backup + undo-journal recovery + identity guards + independent-process profile lease |
| Recovery UI | startup validated-backup dialog | backend tests + nine injected scene faults + actual confirmation/reload passed |
| Settings | three graphics qualities | saved configuration; no audio controls yet |
| Tutorial/help | contextual inspect/start hints + help + journal | no persisted guided onboarding; player playtest pending |
| HD art/animation | existing original PNG atlases | incomplete dedicated growth/tools/UI; no final premium-art claim |
| Audio | YouTube HTTPS browser link | five original action SFX and Effects bus implemented; internal music/ambient absent |
| Multiplayer | tested NetworkService seam, Native room protocol | Godot room UI/host/avatars absent; Internet acceptance pending |
| NPC/animals/craft/quests/etc | absent | deferred until core+co-op stable |
| Windows standalone | Native executable retained; Godot launcher uses editor/runtime | Godot export/clean-machine acceptance pending |
