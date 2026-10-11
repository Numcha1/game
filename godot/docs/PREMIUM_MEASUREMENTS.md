# Measured verification evidence

Hardware: Intel i5-12400F, NVIDIA RTX 3060, Windows 10 build 19045, NVIDIA driver 610.62. Godot 4.7.2 .NET Compatibility/OpenGL 3.3; SDK 8.0.423. Measurements below were produced by actual local processes, not estimated FPS.

## Final verification

| Suite | Passed | Failed at final verification | Scope |
| --- | --- | --- | --- |
| Core | 194 | 0 | Original crop rules, command validation, v1 saves, recovery, protocol/session/configuration and architecture regressions |
| Native | 35 | 0 | Release build and existing executable self-tests |
| Startup | 45 | 0 | Fault-injected composition/identity/schema/save/asset cases |
| Recovery UI | 10 | 0 | Real dialog callback and asynchronous backup restore / scene reload |
| Rendered smoke | 101 | 0 | Farming/selling/sleeping/input/camera/quality and viewport capture |
| Premium Windows | 175 | 0 | Thai fonts, audio, title/pause/guide/boot, pixel movement, cards, modes and 36 display/text/UI combinations |
| Premium headless | 47 | 0 | Non-GPU presentation contracts; excludes native window/pixel-capture certification |
| Extended lifecycle | 300 | 0 | 50 full scene teardowns and 500 bag/shop cycles |

Godot and Native builds passed with zero warnings/errors. `verify.ps1 -Rendered -ExtendedLifecycle` passed after each production module. The final added title-clock diagnostic was also rebuilt and rerun in headless and Windows renderers. Its initial `int`/`double` test-variable build error was corrected before delivery. Earlier Windows borderless promotion and Thai header wrap defects were reproduced, fixed and covered by regressions. There are no unresolved failures in these named suites.

## Comparable controlled quality sample

Each quality level: 60 warmup frames, 600 recorded frames, 24 ripe crops, storm, evening window lights, same 1600×900 requested window and fixed camera, 60 FPS cap. Before viewport surface was 800×450; after canvas content is 1600×900. Timings therefore describe the actual upgraded presentation workload, not a same-native-resolution speedup.

| Quality | Before FPS | After FPS | Before / after P95 ms | After render CPU ms | After GPU ms | After draw calls | Before / after private MiB |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Low | 60.003 | 60.031 | 16.784 / 16.728 | 0.505 | 1.116 | 81.1 | 190.1 / 207.6 |
| Medium | 60.002 | 60.002 | 16.782 / 16.698 | 0.579 | 1.432 | 120.4 | 191.3 / 200.3 |
| High | 60.086 | 60.002 | 16.767 / 16.697 | 0.669 | 1.454 | 171.9 | 185.0 / 202.5 |

GPU/renderer CPU timings were not captured in the original baseline, so no before/after improvement percentage is valid. Render CPU is RenderingServer viewport work, not complete game-script CPU time. `meanEngineFrameMs` is Godot TimeProcess, not CPU-only cost. GPU timings are engine-reported; power states and driver conditions can vary. Private bytes include engine/caches/native allocations; managed bytes are recorded separately in JSON. Additional fonts, assets, higher-resolution UI and pools increase the working set.

General smoke sample: before 59.813 FPS, after 59.037 FPS across 781 frames, including capture/resize work. Controlled quality windows are the evidence for approximately 60 FPS under this fixture, not a guarantee for every frame or long-session workload. Raw evidence: `reports/premium-before/godot-smoke-Windows.json` and `reports/godot-smoke-Windows.json`.

## Lifecycle and compatibility

The finite lifecycle report returned scene nodes to baseline, detached scene/HUD observers and reported zero orphan nodes. Shared textures, fonts and animation frames remained reused. A bounded 50-scene workload is not proof against every lifetime memory/resource leak.

No tracked change to `native/`, `dist/`, or the original `SuanPleng.exe` compared with verified production-core commit `080a0cb`. Original World/Network sources and Native EXE hashes were checked; v1 farm schema and wire protocol were not changed. Display/audio accessibility settings use separate `display.json`; tests use isolated GUID profiles. No Internet co-op or clean-machine Godot export certification is inferred from local protocol/self-tests.

## Stage ledger

| Committed stage | Core / rendered premium checks | Compatibility / remaining gate |
| --- | --- | --- |
| Typography/display | 191 / 42 | Fonts, integer scaling and accessibility; native-reader/DPI acceptance open |
| Original growth/tools/effects | 191 / 62 | No rule changes; full-body/terrain artist pass open |
| Action audio/shared theme/UI size | 194 / 129 | Separate settings schema; sound-mix acceptance open |
| Shared inventory/shop cards | 194 / 168 | Original charges/spoilage retained; player usability acceptance open |
| Title/pause | 194 / 172 | Original pause command retained |
| Threaded boot/loading | 194 / 173 | Startup/recovery/lifecycle rerun |
| Guide and explicit title-clock check | 194 / 175 | Build, headless 47, rendered and lifecycle evidence; release gates listed in polish report |

All stages also passed Native 35, startup 45, recovery 10 and rendered smoke 101 where the full wrapper was run. Failures found during implementation were corrected and named above. Per-stage screenshots/reports were committed with the relevant module; `git show` retrieves intermediate evidence.

## Changed file groups

Source paths changed against the integrated production-core baseline (test evidence PNG/JSON and engine UID sidecars omitted here):

- `godot/ArtSource/crop-growth-source.png`
- `godot/ArtSource/premium-prompts.json`
- `godot/ArtSource/tool-icons-source.png`
- `godot/Assets/Audio/ATTRIBUTION.md`
- `godot/Assets/Audio/click.wav`
- `godot/Assets/Audio/click.wav.import`
- `godot/Assets/Audio/coin.wav`
- `godot/Assets/Audio/coin.wav.import`
- `godot/Assets/Audio/error.wav`
- `godot/Assets/Audio/error.wav.import`
- `godot/Assets/Audio/harvest.wav`
- `godot/Assets/Audio/harvest.wav.import`
- `godot/Assets/Audio/water.wav`
- `godot/Assets/Audio/water.wav.import`
- `godot/Assets/Fonts/ATTRIBUTION.md`
- `godot/Assets/Fonts/mitr/Mitr-Medium.ttf`
- `godot/Assets/Fonts/mitr/Mitr-Medium.ttf.import`
- `godot/Assets/Fonts/mitr/OFL.txt`
- `godot/Assets/Fonts/provenance.json`
- `godot/Assets/Fonts/sarabun/OFL.txt`
- `godot/Assets/Fonts/sarabun/Sarabun-Regular.ttf`
- `godot/Assets/Fonts/sarabun/Sarabun-Regular.ttf.import`
- `godot/Assets/Fonts/sarabun/Sarabun-SemiBold.ttf`
- `godot/Assets/Fonts/sarabun/Sarabun-SemiBold.ttf.import`
- `godot/Assets/crop-growth.png`
- `godot/Assets/crop-growth.png.import`
- `godot/Assets/polish-regions.json`
- `godot/Assets/tool-icons.png`
- `godot/Assets/tool-icons.png.import`
- `godot/Assets/tool-poses.png`
- `godot/Assets/tool-poses.png.import`
- `godot/Core/Infrastructure/Configuration/DisplaySettings.cs`
- `godot/Diagnostics/FarmScene.Diagnostics.cs`
- `godot/Diagnostics/FarmScene.Premium.cs`
- `godot/Infrastructure/Assets/AssetCatalog.cs`
- `godot/Infrastructure/Assets/FarmPreloader.cs`
- `godot/Infrastructure/Assets/FarmTypography.cs`
- `godot/Presentation/Audio/FarmAudio.cs`
- `godot/Presentation/Effects/EffectPool.cs`
- `godot/Presentation/Effects/WeatherAccentPool.cs`
- `godot/Presentation/Effects/WeatherLighting.cs`
- `godot/Presentation/Input/FarmInputRouter.cs`
- `godot/Presentation/Player/FarmerController.cs`
- `godot/Presentation/UI/FarmBoot.cs`
- `godot/Presentation/UI/FarmHud.cs`
- `godot/Presentation/UI/FarmItemCard.cs`
- `godot/Presentation/UI/FarmTheme.tres`
- `godot/Presentation/World/FarmScene.Display.cs`
- `godot/Presentation/World/FarmScene.cs`
- `godot/Presentation/World/FarmWorldPresenter.cs`
- `godot/Scenes/Boot.tscn`
- `godot/Scenes/Farm.tscn`
- `godot/Scenes/UI/ItemCard.tscn`
- `godot/Shaders/Water.gdshader`
- `godot/Shaders/Wind.gdshader`
- `godot/Tests/DisplayChecks.cs`
- `godot/Tests/Program.cs`
- `godot/Tools/PolishBake/PolishBake.csproj`
- `godot/Tools/PolishBake/Program.cs`
- `godot/Tools/synthesize_audio.py`
- `godot/default_bus_layout.tres`
- `godot/project.godot`
- `godot/verify.ps1`

Final docs include `PREMIUM_AUDIT.md`, `PREMIUM_POLISH.md`, this file, refreshed ART/README/TESTING/feature matrix/quality gates, and CI trigger coverage for `feature/premium-polish`. Source sheets, prompt JSON and fonts/licenses are included; generated profile directories and transient logs are excluded.
