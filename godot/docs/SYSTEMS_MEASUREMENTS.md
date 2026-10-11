# Systems Milestone 1 performance evidence

Actual Windows renderer on i5-12400F / RTX 3060, Godot 4.7.2 .NET Compatibility, NVIDIA 610.62. Same upgraded integer-canvas pipeline, 1600×900 requested window, deterministic 24-ripe-crop storm/evening fixture, 60 warmup frames and 600 recorded frames per quality, cap 60 FPS.

All numeric columns show **integrated baseline / final milestone**. Source JSON: `reports/systems-m1-before.json` and `reports/godot-smoke-Windows.json`.

| Quality | FPS | P95 frame ms | Render CPU mean ms | GPU mean ms | Private MiB |
| --- | --- | --- | --- | --- | --- |
| Low | 60.003 / 60.048 | 16.780 / 16.726 | 0.573 / 0.508 | 1.001 / 1.093 | 209.7 / 212.1 |
| Medium | 60.039 / 60.002 | 16.747 / 16.688 | 0.590 / 0.564 | 1.195 / 1.442 | 214.8 / 216.5 |
| High | 60.002 / 60.002 | 16.690 / 16.689 | 0.667 / 0.673 | 1.444 / 1.449 | 204.9 / 207.4 |

The averages remain approximately 60 FPS under this workload. These short samples do not establish a speedup or a long-session guarantee. Renderer CPU excludes game scripts; GPU is engine-reported viewport timing. Private bytes include unmanaged engine/caches; separate managed bytes, engine TimeProcess and draw calls are in JSON. General smoke includes capture/resize work: 59.037 FPS across 781 frames. It is not substituted for the controlled quality windows.

Extended lifecycle: 350 checks, 50 immediate scene teardowns and 500 menu cycles. Each teardown also queued 0→1→2 graphics choices and a manual save; final setting/farm/tmp-file checks passed. Node baseline remained 442 and every sample reported zero orphan nodes. Private memory after the first/last forced-GC samples: 226.3 / 228.3 MiB. This finite workload did not reveal growing scene-node counts; it cannot prove the absence of every lifetime memory leak. Lifecycle deliberately forces GC and must not be used as a normal frame-pacing benchmark.

Unknown-field/duplicate-name checks run during load/import recovery, not every frame. Snapshot capture remains bounded synchronous serialization; disk replacement and logging remain background work. No performance improvement is attributed to synthetic unit tests. Long gameplay soak, larger persistent worlds, networked Godot actors and physical DPI/4K hardware remain future acceptance work.
