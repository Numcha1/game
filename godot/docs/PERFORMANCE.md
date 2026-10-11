# Performance comparison

Measured before (`ecf7e327`) and after in the actual checkout on the same Windows 10 machine, Intel i5-12400F, NVIDIA RTX 3060, driver 610.62, Godot 4.7.2 .NET / OpenGL Compatibility. Default window 1600×900, logical 800×450, nearest/integer scaling, 60 FPS cap. Same isolated fixture: 24 ripe crops, storm, evening lights, one player, 60 warmup frames followed by 600 sampled frames per quality. Each rendered run passed 91 scene/gameplay/viewport checks.

Raw reports: `reports/architecture-before.json` and `reports/architecture-after.json`. Times are wall-clock engine frame intervals, not GPU timestamps. A capped run cannot establish higher maximum throughput.

| Quality | FPS before → after | Mean ms | P95 ms | P99 ms | Private MiB after sample | Managed MiB after sample |
|---|---|---|---|---|---|---|
| Low | 60.002 → 60.003 | 16.6660 → 16.6658 | 16.7082 → 16.7875 | 16.7712 → 16.8538 | 189.48 → 190.57 | 3.07 → 3.78 |
| Medium | 60.002 → 60.003 | 16.6660 → 16.6658 | 16.7133 → 16.7871 | 16.7893 → 16.8467 | 190.35 → 191.11 | 3.23 → 4.30 |
| High | 60.003 → 60.004 | 16.6659 → 16.6657 | 16.7082 → 16.7778 | 16.7429 → 16.8473 | 189.86 → 182.14 | 3.39 → 4.81 |

All three remain approximately 60 FPS. Small percentile differences are within this finite machine run; no statistically supported speedup is claimed. Disk replacement moved off the game loop and resources are no longer loaded per frame, but snapshot serialization still occurs on the session thread. Managed memory includes short-lived cancellable diagnostics waits and report allocations and increased in this sample; this is not presented as a memory optimization. Private bytes also vary with runtime collection and driver allocation, so reductions do not prove lower steady-state memory.

## Lifecycle/resource soak

`reports/lifecycle-headless.json`: 10 complete scene creations/teardowns and 100 bag/shop cycles. Sixty checks passed. Every post-teardown sample returned to 371 nodes, 22 resources, zero orphan nodes, and zero observers on the disposed session. Managed memory after forced GC stayed around 1.3–1.4 MiB; report/sample objects themselves accumulate a small bounded amount. Async saves and logging were drained before sampling. Pending signal work was cancelled and explicitly disconnected. Forced GC makes this workload unsuitable for an FPS comparison.

The rendered lifecycle run (`reports/lifecycle-Windows.json`) also passed 60 checks with 371 nodes, 22 resources and zero orphans in every round. Managed memory after GC ranged from 1.30 to 1.33 MiB. Private process memory varied from roughly 193 to 199 MiB; the driver/runtime allocation is not flat, and this finite run cannot classify that variation as a leak or certify GPU memory stability.

This detects no retained scene nodes or increasing Godot resource count in the tested scope. It does not prove absence of all memory leaks, long-session multiplayer leaks, GPU/driver leaks, or 60 FPS on other machines. Run longer release-build/hardware soaks before setting product minimum specifications.

## Reproduce

Use `dotnet build`, then run the engine separately with `-- --smoke` and `-- --lifecycle`; both isolate user saves. Rendered and headless runs produce different report names. Do not compare headless FPS with rendered FPS. Do not run simultaneous graphics benchmarks. Refer to PHASES.md for commands and intermediate failures resolved during the work.
