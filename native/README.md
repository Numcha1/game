# สวนเพลง — Seasons Together (Windows prototype 0.2)

Native 2D Windows game. The game scene and character movement are drawn by Windows/.NET, with no webpage or browser used for gameplay.

## Run

Open `SuanPleng.exe`. The online connector is bundled in the executable and is extracted automatically on first use. The self-contained build includes the .NET runtime. Windows x64 only.

## Internet co-op

1. Host chooses สร้างห้องออนไลน์ and enters a name.
2. Copy the entire invite (including the fragment after `#`) and give it to friends privately.
3. Each friend opens their own copy of the game, chooses เข้าห้องเพื่อน, pastes the invite and enters a name.
4. Up to four active players share the farm, calendar, weather and money. Each has separate energy and harvested inventory. Expensive greenhouse purchase is host-only.
5. Host must keep the game running. Closing the host saves the farm and stops its tunnel. The invite changes at the next online launch. Rejoining with the same Windows profile restores that player's bag.

Prototype networking uses Cloudflare Quick Tunnels, which require no account or router port forwarding but have no uptime guarantee. This is suitable for testing with friends; a permanent release should use a stable managed relay or authenticated dedicated server. The room only exposes a loopback game JSON service through its tunnel, requires a random invitation secret, never serves local files, and validates commands on the host. Do not publish invites publicly.

## Controls and mechanics

- WASD/arrows: walk. Select seed/tool then click a nearby plot. 1–7 select tools.
- E near shipping crate: sell your inventory to shared funds.
- Space near the house: sleep; all active players must sleep to skip the day. Space again cancels sleep.
- Esc: host pauses/unpauses world. F1: help.
- One farming day is about 10 real minutes, with four 14-day seasons. Time stops while the game is closed.
- Eight seasonal crops, daily watering, harvest windows, soil depletion and rotation, weather/heat/storms, stakes, disease and adjacent spread, treatment, fertilizers, variable sale prices, overnight inventory spoilage, host-approved greenhouse roof and emergency money.
- Hover a plot for its moisture, fertility, crop age, quality and remaining harvest window.
- Saves: `%LOCALAPPDATA%\SuanPleng\farm.json`; previous version is `.bak`. Start-new menu preserves a dated backup. Corrupt saves are refused, never silently overwritten.
- YouTube music currently opens a validated YouTube link in the default browser; it plays separately while the native game remains running. Embedded music playback is a future feature.

## Animation update 0.2

Layered scene rendering with ground shadows, raised soil beds, detailed roofs/trees, eight distinct crop drawings, and depth sorting around characters. Local characters animate arms, legs, facing direction and tool gestures; remote movement is interpolated between network snapshots. Confirmed server action events drive planting dust, watering droplets/ripples, harvest arcs, floating labels and sale effects for all players. Rejected/retried commands do not generate duplicate successful effects. Trees sway, water ripples, ducks paddle, butterflies flutter, weather creates rain/snow/leaves, and the farm dims toward evening. Soft generated action sounds can be toggled with M; F2 reduces ambient motion and particle density. Save version 1 remains compatible.

## Development

`dotnet build -c Release`

`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release`

Run `SuanPleng.exe --self-test` for simulation/network checks, `--internet-test` for two HTTP clients through a real temporary public tunnel, or `--render-test` to create offscreen menu/farm screenshots. Tests write JSON results next to the executable. GUI tests do not open a visible window.

Native self-contained publishing: https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview

Temporary tunnel limitations: https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/

Cloudflared redistribution: official Cloudflare build, Apache-2.0. See CLOUDFlARED-LICENSE.txt supplied in release.

For a clean Git clone, run ./build.ps1 to download the pinned, checksum-verified tunnel connector and publish the Windows executable.
