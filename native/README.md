# สวนเพลง — Seasons Together 0.3

Native Windows x64 farming game built with C# / .NET 8 WinForms and GDI+. The farm is rendered by the game itself; gameplay does not run in a browser. The self-contained EXE includes the .NET runtime and the online connector.

## Play

Open `SuanPleng.exe`. Choose solo play to continue your existing farm, create an online room, or join a friend's room. Saves remain in `%LOCALAPPDATA%\SuanPleng\farm.json` with the same version 1 format. Graphics preferences are separate in `visual-settings.json`.

The host shares the entire invitation, including the `#` fragment, privately with friends. Up to four active players share the calendar, weather and money, with separate bags and energy. The host must keep the game running. Cloudflare Quick Tunnels require no account or router port forwarding, but rooms are temporary and invitations change on each launch.

## Controls

| Control | Action |
| --- | --- |
| WASD / arrows | Walk |
| Click a nearby plot | Use selected tool |
| 1–7 | Seed, water, harvest, treatment, fertilizer, stakes, clear |
| Tab | Select seeds and inspect season/harvest information |
| B / I | Inventory and current sale prices |
| O | Farm shop, greenhouse and emergency funds |
| E | Sell near shipping crate |
| Space | Sleep near home; all active players must vote |
| Esc | Host toggles pause; closes an open menu |
| F1 | Thai farming guide |
| F2 | Reduce ambient motion and particles |
| F3 | Low / Medium / High graphics, sound and zoom |
| M | Toggle action sounds |
| + / − | Change camera zoom |

The UI scales with the window, including letterboxing. Recommended minimum client size is 960×640; the default is 1280×800. Camera zoom changes presentation only; plot coordinates, action distances and collision rules remain the same.

## Farming systems

Four 14-day seasons, roughly ten-minute days, eight seasonal crops, daily watering, crop harvest windows, soil fertility/rotation, weather, heat, storms, stakes, adjacent disease spread, treatment, fertilizer, variable sale prices, overnight bag spoilage, shared funds, all-player sleep and host-only greenhouse approval. Time stops while the host game is closed. Previous/corrupt-save protection is retained.

YouTube links still open music in the default browser separately from gameplay.

## Graphics update

Original cached pixel sprites, seasonal textured ground, detailed buildings/props, soft contact shadows, smooth camera, idle/walk/tool animations, depth sorting, weather-driven wind, layered rain, pond ripples/splashes, butterflies, snow, falling leaves, morning/evening lighting, glowing night windows and a day transition. Confirmed host events drive planting dust, watering, harvest arcs and animated sale coins for all players. The particle pool is bounded; failed/retried actions do not replay successful effects.

The native UI now uses a HUD, icon hotbar, seed selection, inventory, shop, guide and graphics settings panels. Modal panels exclusively own input. Thai text uses the Windows Tahoma font.

See [original art details](ART.md) and [measured performance](PERFORMANCE.md). High targets 60 FPS at the default window on the measured machine; use Medium or Low for additional headroom at larger resolutions or on slower CPUs. This software renderer does not guarantee the same performance on every Windows machine.

## Build and verify

Install the .NET SDK 8 on Windows, then run:

```powershell
./build.ps1
```

The script obtains the pinned official cloudflared release, verifies SHA-256 and publishes `release/SuanPleng.exe`. No other art/runtime assets need downloading.

```powershell
./release/SuanPleng.exe --self-test
./release/SuanPleng.exe --internet-test
./release/SuanPleng.exe --visual-test
./release/SuanPleng.exe --loop-test
./release/SuanPleng.exe --benchmark
```

Checks create isolated profiles and reports beside the EXE. `--internet-test` opens and closes a temporary public tunnel. `--visual-test` exports the production atlas and visual verification images. `--loop-test` exercises the actual UI frame scheduler, network polling and two clients without opening a visible window. `--benchmark` measures full offscreen rendering across three qualities and resolutions with four active fixture players. Presentation by the monitor/DWM and Internet latency are excluded from FPS benchmarks.
