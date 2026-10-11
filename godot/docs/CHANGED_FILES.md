# Changed files

Base: godot/vertical-slice (ecf7e327). Branch: godot/modular-architecture.

The source changes are confined to the Godot slice. Application/domain projections and persistence/network/config/logging implementations are in Core; engine presentation is grouped by World, UI, Player, Input, Camera and Effects; the engine asset catalog is in Infrastructure/Assets. Existing scripts and their UIDs moved; Farm.tscn changes only the three script paths. Tests, diagnostics, docs and generated verification reports are included. PNG changes below are captured reports, not game asset edits.

Original native source, World.cs rules, Network.cs protocol, game Assets, ArtSource, shaders, viewport configuration and root SuanPleng.exe have no changes relative to the base. See reports/preservation.json for verification hashes. No NuGet packages or projects added.

Status: A added, M modified, D old location removed, R moved (similarity percentage). Git may represent an extraction as add/delete when similarity falls below its rename threshold. Existing Native Game files were not deleted. This document is an additional new file.

```text
A	godot/Core/Application/GameReadModel.cs
A	godot/Core/Application/GameReadModel.cs.uid
A	godot/Core/Application/GameSession.cs
A	godot/Core/Application/GameSession.cs.uid
A	godot/Core/Application/MusicLink.cs
A	godot/Core/Application/MusicLink.cs.uid
A	godot/Core/Application/NetworkService.cs
A	godot/Core/Application/NetworkService.cs.uid
A	godot/Core/Application/Services.cs
A	godot/Core/Application/Services.cs.uid
A	godot/Core/Domain/FarmInvariants.cs
A	godot/Core/Domain/FarmInvariants.cs.uid
A	godot/Core/Domain/GameViews.cs
A	godot/Core/Domain/GameViews.cs.uid
A	godot/Core/Infrastructure/Configuration/GraphicsSettings.cs
A	godot/Core/Infrastructure/Configuration/GraphicsSettings.cs.uid
A	godot/Core/Infrastructure/Logging/JsonGameLog.cs
A	godot/Core/Infrastructure/Logging/JsonGameLog.cs.uid
A	godot/Core/Infrastructure/Network/HttpRoomTransport.cs
A	godot/Core/Infrastructure/Network/HttpRoomTransport.cs.uid
A	godot/Core/Infrastructure/Persistence/SaveService.cs
A	godot/Core/Infrastructure/Persistence/SaveService.cs.uid
M	godot/Core/LocalFarmSession.cs
A	godot/Core/Properties/AssemblyInfo.cs
A	godot/Core/Properties/AssemblyInfo.cs.uid
M	godot/Core/SeasonsTogether.Core.csproj
A	godot/Diagnostics/FarmScene.Diagnostics.cs
A	godot/Diagnostics/FarmScene.Diagnostics.cs.uid
A	godot/Diagnostics/FarmScene.Lifecycle.cs
A	godot/Diagnostics/FarmScene.Lifecycle.cs.uid
A	godot/Diagnostics/FrameTimeMonitor.cs
A	godot/Diagnostics/FrameTimeMonitor.cs.uid
A	godot/Infrastructure/Assets/AssetCatalog.cs
R100	godot/Scripts/AssetCatalog.cs.uid	godot/Infrastructure/Assets/AssetCatalog.cs.uid
A	godot/Presentation/Camera/PixelCameraFollower.cs
A	godot/Presentation/Camera/PixelCameraFollower.cs.uid
R086	godot/Scripts/EffectPool.cs	godot/Presentation/Effects/EffectPool.cs
R100	godot/Scripts/EffectPool.cs.uid	godot/Presentation/Effects/EffectPool.cs.uid
R079	godot/Scripts/WeatherLighting.cs	godot/Presentation/Effects/WeatherLighting.cs
R100	godot/Scripts/WeatherLighting.cs.uid	godot/Presentation/Effects/WeatherLighting.cs.uid
A	godot/Presentation/Input/FarmInputRouter.cs
A	godot/Presentation/Input/FarmInputRouter.cs.uid
R067	godot/Scripts/FarmerController.cs	godot/Presentation/Player/FarmerController.cs
R100	godot/Scripts/FarmerController.cs.uid	godot/Presentation/Player/FarmerController.cs.uid
R082	godot/Scripts/FarmHud.cs	godot/Presentation/UI/FarmHud.cs
R100	godot/Scripts/FarmHud.cs.uid	godot/Presentation/UI/FarmHud.cs.uid
A	godot/Presentation/World/FarmScene.cs
R100	godot/Scripts/FarmScene.cs.uid	godot/Presentation/World/FarmScene.cs.uid
A	godot/Presentation/World/FarmWorldPresenter.cs
A	godot/Presentation/World/FarmWorldPresenter.cs.uid
M	godot/README.md
M	godot/Scenes/Farm.tscn
D	godot/Scripts/AssetCatalog.cs
D	godot/Scripts/FarmScene.cs
A	godot/Tests/ApplicationChecks.cs
A	godot/Tests/ApplicationChecks.cs.uid
A	godot/Tests/ArchitectureChecks.cs
A	godot/Tests/ArchitectureChecks.cs.uid
A	godot/Tests/NetworkChecks.cs
A	godot/Tests/NetworkChecks.cs.uid
A	godot/Tests/PersistenceChecks.cs
A	godot/Tests/PersistenceChecks.cs.uid
M	godot/Tests/Program.cs
A	godot/docs/ARCHITECTURE.md
A	godot/docs/AUDIT.md
A	godot/docs/PERFORMANCE.md
A	godot/docs/PHASES.md
A	godot/reports/architecture-after.json
A	godot/reports/architecture-before.json
M	godot/reports/farm-day.png
M	godot/reports/farm-storm.png
A	godot/reports/final-core-tests.json
M	godot/reports/godot-smoke-Windows.json
M	godot/reports/godot-smoke-headless.json
A	godot/reports/lifecycle-Windows.json
A	godot/reports/lifecycle-headless.json
A	godot/reports/native-regression.json
A	godot/reports/phase2-tests.json
A	godot/reports/phase3-tests.json
A	godot/reports/phase4-tests.json
A	godot/reports/phase5-save-tests.json
A	godot/reports/phase6-tests.json
A	godot/reports/phase7-tests.json
A	godot/reports/preservation.json
M	godot/reports/shop.png
```
