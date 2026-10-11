# Building milestone changed files

Milestone 1 `80b0761`, Milestone 2 `32b2a5c`, collision/occlusion correction `51339af`. Baseline `e376f97`. Final documentation and real-run evidence are a separate commit.

## Domain and application

- `godot/Core/Application/GameReadModel.cs`
- `godot/Core/Application/GameSession.Buildings.cs`
- `godot/Core/Application/GameSession.Buildings.cs.uid`
- `godot/Core/Application/GameSession.cs`
- `godot/Core/Application/Services.cs`
- `godot/Core/Domain/Buildings.cs`
- `godot/Core/Domain/Buildings.cs.uid`
- `godot/Core/Infrastructure/Persistence/BuildingSaveDocument.cs`
- `godot/Core/Infrastructure/Persistence/BuildingSaveDocument.cs.uid`
- `godot/Core/Infrastructure/Persistence/SaveService.cs`

## Assets and loading

- `godot/Assets/Audio/ATTRIBUTION.md`
- `godot/Assets/Audio/door.wav`
- `godot/Assets/Audio/door.wav.import`
- `godot/Assets/Audio/interior-ambience.wav`
- `godot/Assets/Audio/interior-ambience.wav.import`
- `godot/Assets/Interiors/farmhouse-studio.png`
- `godot/Assets/Interiors/farmhouse-studio.png.import`
- `godot/Assets/Interiors/interior-prompts.json`

## Infrastructure

- `godot/Infrastructure/Assets/AssetCatalog.cs`
- `godot/Infrastructure/Assets/FarmPreloader.cs`
- `godot/Infrastructure/Assets/InteriorSceneLoader.cs`
- `godot/Infrastructure/Assets/InteriorSceneLoader.cs.uid`

## Presentation

- `godot/Presentation/Audio/FarmAudio.cs`
- `godot/Presentation/Buildings/InteriorRoom.cs`
- `godot/Presentation/Buildings/InteriorRoom.cs.uid`
- `godot/Presentation/Camera/PixelCameraFollower.cs`
- `godot/Presentation/Input/FarmInputRouter.cs`
- `godot/Presentation/Player/FarmerController.cs`
- `godot/Presentation/UI/FarmHud.Storage.cs`
- `godot/Presentation/UI/FarmHud.Storage.cs.uid`
- `godot/Presentation/UI/FarmHud.cs`
- `godot/Presentation/World/FarmScene.Buildings.cs`
- `godot/Presentation/World/FarmScene.Buildings.cs.uid`
- `godot/Presentation/World/FarmScene.Display.cs`
- `godot/Presentation/World/FarmScene.Persistence.cs`
- `godot/Presentation/World/FarmScene.cs`

## Scenes

- `godot/Scenes/Interiors/Farmhouse.tscn`

## Automated verification

- `godot/Tests/BuildingChecks.cs`
- `godot/Tests/BuildingChecks.cs.uid`
- `godot/Tests/Program.cs`
- `godot/Tests/StorageChecks.cs`
- `godot/Tests/StorageChecks.cs.uid`

## Engine diagnostics

- `godot/Diagnostics/FarmScene.Diagnostics.cs` (deterministic synthetic input disposal)
- `godot/Diagnostics/FarmScene.BuildingChecks.cs`
- `godot/Diagnostics/FarmScene.BuildingChecks.cs.uid`

## Tools and evidence

- `godot/Tools/synthesize_interior_audio.py`
- `godot/reports/building-m1-Windows.json`
- `godot/reports/building-m1-core.json`
- `godot/reports/building-m1-performance.json`
- `godot/reports/building-m2-core.json`
- `godot/reports/farmhouse-interior.png`
- `godot/verify.ps1`

Final evidence also updates existing regression reports/screenshots, adds rendered/headless building reports, farmhouse storage/night captures and headless lifecycle measurements. Documentation adds BUILDINGS.md, BUILDINGS_MEASUREMENTS.md and this list, and updates README.md/SYSTEMS_ROADMAP.md. Native sources and dist remain unchanged.
