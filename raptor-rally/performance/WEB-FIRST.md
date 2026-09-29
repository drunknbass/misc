# Web-first rendering — September 29, 2026

This pass targets Safari and the flickering rail seams in the previous visual release (`b4336772dbd95301c4552aaadbaff0a667e99f9e`). No package dependencies were added.

## Rendering changes

- Replace 768 overlapping, closed barrier sections with two continuous rail meshes. Adjacent sections share identical boundary positions and normals; color changes do not create overlapping faces. Preserve every separate physics collider and all one-way re-entry behavior.
- Combine each truck body and each moving wheel into one draw per part. Vertex colors and per-vertex metallic/smoothness values preserve material differences using one shared surface shader per model. All three Raptor variants retain their geometry and independently rotating wheels.
- Cache the stationary garage circuit preview, just like the existing truck preview. Invalidate it when the lineup changes. Exclude the race dust mesh from this cached image.
- Use a single 2048-square shadow map, disable unnecessary HDR camera targets and additional forward-light passes in the custom shaders. Keep soft shadows, 2× MSAA, environment reflections and mipmapped dirt.
- Render the live Rive vector HUD into at most 1280×720 at 30 Hz. Fold its color-space/alpha conversion into the final composite, removing the intermediate converted texture and separate conversion blit. Driving, steering-wheel rotation and camera updates remain at display rate.
- Cap the browser drawing buffer at one device pixel per CSS pixel and 1600×900, including Retina/fullscreen. CSS size and the letterboxed UI layout are unchanged.

- Disable both the Unity splash screen and Unity logo in saved PlayerSettings and in the build entry point; retain the game's branded HTML loading screen.

## Measurement method

Safari measurements use foreground Safari Technology Preview on Apple M2 Pro (reported UA Version/27.0, WebKit 605.1.15), a 1600×900 outer window, the same full-track race and a 15-second held-throttle sample. A lightweight script counts WebGL draws and requestAnimationFrame intervals without GPU queries. The previous canvas was 1920×898; the new budget gives 1600×748 in the same window. Resolution reduction is part of the change, so this is a release-to-release comparison, not an isolated mesh-batching benchmark. FPS is derived from measured animation-frame intervals; Unity's separate moving-average metric is included in the raw JSON.

Pass breakdown and asynchronous GPU timing use a separate Chrome headless run on the same Apple M2 Pro with hardware ANGLE/Metal, a 1600×900 CSS viewport and simulated 4× CPU slowdown. These instrumented Chrome results describe rendering work; they are not substituted for Safari FPS. The profiler categorizes draw submissions by target dimensions and color mask. Target binding changes are not hardware render-pass counts. Unity still runs its screen-depth/shadow preparation and MSAA resolve; this pass reduces their work rather than claiming to eliminate them.

## Safari release comparison

| Metric | Previous release | Final release |
| --- | ---: | ---: |
| Mean frame interval | 69.96 ms | 24.69 ms |
| Derived mean frame rate | 14.3 FPS | 40.5 FPS |
| p95 frame interval | 72 ms | 34 ms |
| Draw calls/frame | 523 | 205 |
| Frames above 33.4 ms | 215 / 215 | 39 / 608 |

The final sample reduces mean frame time by 64.7% and WebGL draw calls by 60.8%. Earlier runs of the same rendering changes averaged 46–47 FPS; the final splash-free run above is the conservative result used for release reporting. Some frame spikes remain. No Safari console errors were captured. The close-camera check retains readable live Rive instruments and continuous rail seams.

## Rendering profile

| Metric | Previous release | Web-first build |
| --- | ---: | ---: |
| Garage draws/frame | 491 | 40 |
| Full-track draws/frame | 495 | 198 |
| Follow-camera draws/frame | 318 | 112 |
| Full-track triangle submissions/frame | 309,532 | 262,959 |
| Full-track GPU query time, mean | 9.36 ms | 4.96 ms |
| Full-track GPU query time, p95 | 17.01 ms | 9.42 ms |

At the same 1600×852 drawing buffer in Chrome, full-track draws fall 60%. Scene-depth submissions fall 156 → 57, shadow submissions 148 → 49, and scene color submissions 174 → 75. The shadow atlas falls 4096×4096 → 2048×2048. Rive uses a smaller surface and one fewer color draw per update. Both measured Chrome races remain display-limited near 60 FPS with zero sampled frames above 33.4 ms and no console errors. GPU timer results are instrumented samples, not end-to-end input latency.

## Reproduce

Serve the old and new builds over HTTP. Use official installed Chrome with a dedicated temporary profile and debugging port 9334, plus Node.js 22+ with its built-in WebSocket:

```sh
node web-first.mjs http://localhost:8874/build/ /absolute/path/to/result 4
node smoke.mjs http://localhost:8874/build/ /absolute/path/to/smoke
```

For Safari, load a fresh build, keep its window visible, inject `safari-light-hook.js` once after Unity is ready, click the canvas and press Enter, then execute `safari-light-sample.js`. Read `window.safariBenchmark` after 20 seconds. Reload between releases. Do not run another GPU benchmark concurrently. Do not include the separate GPU-query profiler in this lightweight sample.

Automated acceptance checks include every rail seam, nondegenerate faces and complete vertex material data. All three full AI races, checkpoint rules, fixed wheel pivot, follow camera, 24 re-entry cases, nine full-nitro jumps, spatial-index equivalence and retry reuse pass. The 768 barrier colliders are unchanged. Browser smoke checks cover the three vehicle presentations, Rive HUD, nitro, follow camera, pause, resize and fullscreen.

These measurements are from one Mac and Safari Technology Preview. They do not guarantee 60 FPS in the user's stable Safari version or on other hardware. Initial startup/shader warm-up and WebGL memory use remain areas for later work. In this sample used WASM heap increased from about 131 MB to 173 MB after the geometry/material changes; GPU-target savings are separate from that heap measurement.
