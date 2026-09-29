# Raptor Rally

[Play in your browser](https://drunknbass.github.io/misc/raptor-rally/)

A keyboard arcade racer with three stylized Raptor-inspired trucks, four racers, three laps, an original dirt stadium, nitro, a following camera and live Rive instruments.

## Controls

| Key | Action |
| --- | --- |
| WASD / arrows | Drive and steer |
| Space | Nitro |
| C | Follow your truck / whole track |
| R | Recover |
| Escape | Pause |
| M | Reduced HUD motion |
| 1 / 2 / 3 | Select a truck in the garage |
| Enter | Start race |

Off-course trucks can rejoin through barriers; collisions restore after the truck clears the wall. Best times stay in this browser. A keyboard and WebGL 2 browser are required; touch/gamepad input is not implemented.

## Source and rebuilding

The complete editable Unity project is in [`unity/`](unity/), including the Rive source in `unity/Design/RacingHud/`. Open it with Unity **6000.6.3f1** plus official Web Build Support. Dependencies are pinned in the package manifest and lock file.

Run **Raptor Rally → Build Browser**, or invoke the editor with `-batchmode -nographics -projectPath /absolute/path/to/unity -buildTarget WebGL -executeMethod PrototypeBuilder.BuildWeb -quit -logFile /absolute/path/to/build.log`. The build appears at `../Web/`. Copy its `index.html`, `Build/` and any `StreamingAssets/` into this directory, removing only obsolete generated build files.

The `RaptorPages` template supplies the loading screen and fullscreen button. Gzip with Unity's decompression fallback works without custom response headers. Native WebAssembly threads are disabled; no cross-origin-isolation headers or third-party hosting service are needed. Hashed build filenames prevent stale assets across deployments. GitHub Pages serves this folder from the repository's `main` branch.

## Assets and notices

Original game code and procedural track/truck geometry are covered by the repository MIT license. The wheel is generated reference-guided artwork; its provenance is in `unity/Assets/Resources/HUD/ART-PROVENANCE.md`. The models are stylized, not production CAD models. Ford names are used to identify the visual inspiration; this is an unofficial prototype.

Rive's official Unity runtime is MIT licensed ([notice](RIVE-LICENSE.txt)). Adobe Source Sans 3 is SIL OFL 1.1 ([notice](FONT-LICENSE.md)); font bytes are embedded in the Rive asset. Unity's generated player/runtime remains subject to Unity's applicable terms.

See [validation](VALIDATION.md) for tested behavior and platform limits.

## Performance update

The browser build now batches static scenery, indexes nearby barriers, reuses trucks and cached garage graphics, limits Rive refresh work and caps Retina/fullscreen rendering resolution. See [before/after results and test method](performance/RESULTS.md).

The latest [visual quality pass](performance/VISUAL-QUALITY.md) adds revised truck proportions, material/lighting detail, original dirt artwork, shaped barriers, pooled dust, contact shadows and corrected garage presentation while retaining the browser performance gains.
