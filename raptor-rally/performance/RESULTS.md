# Browser performance — September 28, 2026

Measured in Chrome/154.0.8037.58, headless with hardware-accelerated ANGLE/Metal on Apple M2 Pro. The same dependency-free CDP harness, viewport and input sequence were used before and after. WebGL draw entry points were counted in the browser; these are actual API calls, not Unity Editor estimates.

## Full-track race, 1600×900 viewport, 4× CPU slowdown

| Metric | Deployed baseline | Optimized release |
| --- | ---: | ---: |
| Mean WebGL draw calls/frame | 4215 | 483 |
| Mean animation-frame rate | 30.4 FPS | 60.0 FPS |
| Mean frame interval | 32.86 ms | 16.67 ms |
| 95th percentile frame interval | 49.9 ms | 16.8 ms |
| Frames longer than 33.4 ms | 116 / 457 | 0 / 900 |

Draw calls fell **88.5%** in this 15-second race sample. CPU slowdown is a simulated stress test, not a measurement on a different physical computer. Instrumentation adds some overhead to both runs. Unthrottled baseline racing already approached 60 FPS on this machine; this change provides substantially more headroom.

## Changes

- Combined 1,263 static renderers into 28 spatial chunks using original per-vertex colors. The whole stadium now has 37 mesh renderers, including textured ground and signs. All 768 barrier colliders remain separate.
- Indexed barriers spatially and cached the nearest course sample. The regression sweep checks at most 119 candidate barriers per truck instead of all 768, and matches the exhaustive collision rules at 240 positions, including cell transitions, teleports and reactivation.
- Reused all four truck models when starting or retrying the same lineup; reset race state and collision permissions without rebuilding mesh geometry.
- Cached the stationary garage render and scoreboard state; reused the standings list and procedural dirt texture.
- Limited browser Rive redraws to 30 Hz and a 1600×900 surface; state changes bypass the interval. Only changed strings are rebound. Camera, controls and steering-wheel animation remain at display rate.
- Capped the browser drawing buffer at 1920×1080 while tracking resize/fullscreen. A 1920×1080, 2×-density viewport previously drew 2880×1548 and now draws 1920×1032: **55.6% fewer pixels**. Both Retina runs stayed near 60 FPS on this GPU.

## Acceptance and limits

All three full AI races retain identical finish times. Existing input, wheel-pivot, camera, checkpoint, 24 re-entry and nine nitro-jump scenarios pass. Spatial-index equivalence and pooled-grid reset checks pass. Browser checks covered both alternate garage models, driving with nitro, follow camera, reduced motion, pause, 4K resizing and actual fullscreen; no console errors were captured. The 3840×2160, 2×-density smoke test stayed within a 1920×1056 drawing buffer, and fullscreen remained within budget.

This targets sustained racing cost. Initial shader/GC warm-up still produced a garage long task; it is not a zero-hitch startup claim. The stress-test total WASM heap grew from roughly 134 MB to 153 MB during startup batching, while used heap remained around 128 MB. GPU memory savings from the smaller Rive surfaces are separate from that heap measurement. No Safari/Firefox or lower-powered physical-device performance guarantee is made.

## Reproduce without additional packages

Use official installed Chrome and Node.js 22+ (built-in WebSocket). Start a dedicated headless Chrome process with a new temporary profile and `--remote-debugging-port=9334 --window-size=1600,1000`; never attach this harness to a personal browsing profile. Serve the build over HTTP.

```sh
node benchmark.mjs http://127.0.0.1:8874/perf-optimized/ /absolute/path/to/results 4
node benchmark.mjs http://127.0.0.1:8874/perf-optimized/ /absolute/path/to/retina 1 retina
node smoke.mjs http://127.0.0.1:8874/perf-optimized/ /absolute/path/to/smoke
```

The harness waits for game startup, samples the garage, starts a race, holds throttle in the full-track view, samples follow mode, captures a screenshot and navigates its dedicated tab to a blank page. Raw JSON measurements are alongside this report.
