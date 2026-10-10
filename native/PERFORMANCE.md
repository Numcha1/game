# Graphics update 0.3 — verification and performance

## Result and changed files

The farm remains a native C#/.NET 8 WinForms game. The update replaces the flat procedural scene with original cached pixel artwork, full-width farm view, camera, seasonal ground, detailed materials and props, soft shadows, ambient weather/time lighting and warm night windows. The GUI now uses a HUD, tool hotbar and native inventory/shop/seed/help/settings panels. Farm action effects are still driven by confirmed server events.

- `FarmArt.cs`: original production sprite atlas, four cached ground layers and prop/shadow/glow assets.
- `PremiumScene.cs`: depth sorting, camera, lighting/weather, day transition, plant/character rendering.
- `GameUi.cs`, `HudCache.cs`: Thai game UI, modal input and cached HUD regions.
- `Scene.cs`: bounded particle pool, event deduplication and existing sound/animation checks.
- `GameForm.cs`: responsive presentation/input transforms, preferences and deterministic resource disposal.
- `FrameClock.cs`, `FramePulse.cs`: 60 Hz visual pacing with at most one queued UI frame; balanced native timer-resolution request.
- `Diagnostics.cs`, `LoopDiagnostics.cs`, `Program.cs`: reproducible render, input, animation and live-loop checks.

`World.cs` and `Network.cs` are byte-identical to version 0.2. Planting/harvesting/selling, seasons/weather simulation, authority, invitation auth, multiplayer command deduplication and save version 1 remain unchanged. Graphics settings are stored separately from the farm save.

## Test environment and method

Measured on Intel Core i5-12400F (12 logical processors), Windows 10 build 19045 and .NET 8.0.29. Both measurements use self-contained compressed Windows EXEs with the same .NET runtime. Before/after offscreen benchmarks render the complete scene plus HUD with 24 mature crops and four fixture players. The before test uses 600 measured frames per scene; the after sweep uses 360 per case, 50 warm-up frames each, three qualities and three client sizes (6,480 measured frames). After fixtures explicitly keep all players active during the entire sweep.

Rendering is CPU/GDI+. These numbers include animation and complete UI work. They exclude monitor/DWM presentation, Internet latency and cloudflared's separate process. Other machine workloads can affect timing. They are measured results on this machine, not a promise of 60 FPS on every PC.

## Comparable 1280×800 results (High)

| Scene | Before mean ms | After mean ms | Before p95 ms | After p95 ms | Mean improvement |
| --- | ---: | ---: | ---: | ---: | ---: |
| day | 13.74 | 6.98 | 17.10 | 10.61 | 49.2% |
| storm-night | 16.86 | 10.65 | 20.16 | 13.86 | 36.8% |

The 60 FPS frame budget is 16.67 ms. The measured High p95 CPU render times fit that budget at both the default size and 1920×1080 in this run. Slower machines can select Medium/Low and reduced motion. HUD/sprites/ground/shadow resources are cached; larger displays scale one fixed-size backing image instead of rerasterizing every object at full display resolution.

## Actual game-loop check

The packed EXE ran the actual frame scheduler and network timers, walking animation, storm/night rendering and two local protocol clients for ten measured seconds. It produced 59.99 frames/s, mean frame interval 16.67 ms, p95 interval 18.01 ms and mean CPU render 10.80 ms, with 44 peer polls and 2 players. It rendered into an offscreen bitmap under the real Windows message loop, without opening a visible window. Some interval jitter remains; this is not a display-presentation or end-to-end Internet FPS measurement.

The previous development WM_TIMER pacing showed roughly 40 FPS under the same moving/networked scene despite sufficient CPU render capacity. The visual scheduler now posts one deadline-based UI callback at a time. It does not change the authoritative server clock or movement speeds.

## Correctness checks

- 35 simulation/local protocol assertions: farming, crop windows, weather damage, soil rotation, sale, sleep vote, old save loading, corrupt-save refusal, room auth, room capacity and action races.
- 42 assertions with two clients through a real temporary public Internet tunnel (includes the local assertions). The first restricted-network run failed to reach the temporary host; the unrestricted Internet check passed. No firewall/router settings were changed.
- 255 graphics/input checks: every plot's camera inverse at nine window/zoom combinations, physical click dispatch, modal ownership and blocked clicks, all menu panels, four seasonal palettes, storm/night, quality settings, day transition and animation/pool bounds.
- Successful single-file self-contained Windows publish with zero compiler warnings/errors.

## Memory and handles

Maximum private process memory observed across the sweep was 90.5 MiB (before: 64.9 MiB). Cached artwork adds a fixed memory cost compared with the old renderer. Managed memory returned near baseline after collection; native GDI handle counts remained equal before/after each measured case (15–15 handles). These are finite render-sweep observations, not an overnight leak certification. Bitmaps, atlas, brushes, fonts, sound streams, native clock request and frame scheduler are disposed with the form. Particles and UI callbacks have hard bounds.

## Full quality/resolution sweep

| Client size | Quality | Scene | Mean ms | p95 ms | p99 ms | Managed bytes/frame |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| 1280×800 | Low | day | 6.21 | 9.11 | 11.60 | 6,232 |
| 1280×800 | Low | storm-night | 8.46 | 11.68 | 16.03 | 7,464 |
| 1280×800 | Medium | day | 6.20 | 7.39 | 9.63 | 8,707 |
| 1280×800 | Medium | storm-night | 10.81 | 14.23 | 19.84 | 12,000 |
| 1280×800 | High | day | 6.98 | 10.61 | 12.29 | 10,720 |
| 1280×800 | High | storm-night | 10.65 | 13.86 | 18.08 | 16,760 |
| 960×640 | Low | day | 5.05 | 5.87 | 7.65 | 6,232 |
| 960×640 | Low | storm-night | 6.46 | 8.45 | 11.16 | 7,464 |
| 960×640 | Medium | day | 5.04 | 5.68 | 7.65 | 8,704 |
| 960×640 | Medium | storm-night | 8.44 | 11.31 | 17.02 | 12,000 |
| 960×640 | High | day | 5.37 | 6.18 | 7.90 | 10,720 |
| 960×640 | High | storm-night | 8.61 | 11.53 | 14.16 | 16,760 |
| 1920×1080 | Low | day | 9.09 | 11.48 | 13.91 | 6,336 |
| 1920×1080 | Low | storm-night | 11.19 | 16.41 | 21.04 | 7,568 |
| 1920×1080 | Medium | day | 9.20 | 10.62 | 17.65 | 8,808 |
| 1920×1080 | Medium | storm-night | 12.75 | 15.75 | 17.97 | 12,104 |
| 1920×1080 | High | day | 9.21 | 10.04 | 12.06 | 10,824 |
| 1920×1080 | High | storm-night | 12.72 | 15.27 | 17.98 | 16,864 |

## Reproduce / raw evidence

Run `build.ps1`, then `--self-test`, `--internet-test`, `--visual-test`, `--loop-test` and `--benchmark` on the generated EXE. Reports use isolated test profiles. Raw results are in `reports/before/` and `reports/after/`; production atlas/screenshots are in `preview/`. Visual fixture screenshots show the actual game renderer and do not replace game assets.

The public relay remains a temporary Cloudflare Quick Tunnel, and YouTube music still opens in the browser. No permanent relay service or GPU rendering backend was introduced. Native Windows testing was done on this machine; different CPUs, Windows versions and monitor presentation paths require their own validation.

Native clock details: [Microsoft timeBeginPeriod](https://learn.microsoft.com/en-us/windows/win32/api/timeapi/nf-timeapi-timebeginperiod) and [WinForms timer limitations](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/limitations-of-the-timer-component-interval-property).
