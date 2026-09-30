# Raptor Rodeo

[Play in your browser](https://drunknbass.github.io/misc/raptor-rally/)

An arcade racer with keyboard and mobile touch controls with three stylized Raptor-inspired trucks, four racers, three laps, an original dirt stadium, nitro, a following camera and live Rive instruments.

## Opening sequence

The 6.6-second Ford-only intro fades in a sharp oval, wipes downward to reveal an 88×34 pixel version, then fades to black and enters the garage. Skip remains visible throughout; Enter, Escape and Space also skip. In-game Raptor badges use the original artwork without pixel filtering. Race retries do not replay the intro. See [intro validation](INTRO-WIPE-CHECKS.md) and [artwork provenance](unity/Design/Brand/PROVENANCE.md).

## Controls

| Key | Action |
| --- | --- |
| WASD / arrows | Drive and steer |
| Space | Nitro |
| C | Whole track → follow truck → driver seat |
| R | Recover |
| Escape | Pause |
| M | Reduced HUD motion |
| 1 / 2 / 3 | Select a truck in the garage |
| Enter | Start race |

Off-course trucks can rejoin through barriers; collisions restore after the truck clears the wall. Best times stay in this browser. A WebGL 2 browser is required. On phones and touch tablets, translucent bottom controls provide steering, gas, brake/reverse and nitro, with simultaneous touches supported. The right cluster runs gas → nitro → brake: hold the ribbed gas pedal and slide onto nitro without lifting to boost while accelerating, then onto brake to stop. Sliding back works too. Add `?controls=touch` to try the touch layout on a desktop. Portrait and landscape layouts include touch truck selection, race information, camera switching, recovery and pause. Gamepad input is not implemented. See [mobile implementation and validation](MOBILE.md).

Glancing wall impacts now slide along the rail, and truck impacts transfer momentum with reduced spin. See [handling changes](HANDLING.md) and the [84 Play Mode contact checks](COLLISIONS.md).

The on-screen Raptor wheel has 2.5 turns from center to either lock (±900°), five turns lock to lock. It keeps its fixed center while rotating and unwinds continuously when reversing. See [steering checks](STEERING.md).

## Driver-seat view

Press C or use the mobile camera button to cycle through whole-track, follow and driver-seat views. Each truck has a left-hand cockpit with an A-pillar, dashboard, hood and animated wheel. Camera choice persists across retries and truck changes. See [camera validation](COCKPIT.md).

## Track builder

Open **Track builder** in the header. Opening, generating a course and returning to the garage show immediate loading feedback; controls unlock when the game is ready. Draw a closed route on the 7×5 grid (8–48 route tiles), then paint flat dirt, jumps, tabletops, rollers, mud or nitro recharge pads. **Race this track** generates the Unity terrain, barriers, AI racing line and checkpoints and starts a three-lap race. All three Raptors and camera views work on custom circuits. **Edit track** returns to your draft; **Race Coyote Basin** restores the original course.

**Junction jumps:** choose **Figure eight · jump**. While drawing, cross an existing straight at right angles and continue straight through the junction. The east–west route gets a dirt takeoff ramp, an open gap across the north–south road, and a landing ramp. Carry speed to clear the gap; there is no bridge deck. Leave one straight, flat approach tile on each side of both routes. Amber tiles mark the ramps, and a dashed flight arc marks the gap. Recover returns you to the run-up after a missed jump. Existing crossing courses automatically use the new jump; version 1 saved tracks remain compatible.

Corners and the two starting grid tiles must stay flat. Use **Start line** to choose two consecutive flat straights, and Undo/Redo to revise the layout. Drafts autosave locally; **Save course** stores up to 12 named courses or unfinished drafts in this browser (saving an existing name replaces it). Saving and exporting work before a loop is race-ready; only **Race this track** requires a valid circuit. Saved drafts retain their open/closed state and appear with a Draft label. Export/import `.raptor.json` files to move finished or unfinished courses between devices. Files are validated in the browser and Unity; imports never run code. Custom times do not overwrite Coyote Basin records. The visual editor is part of the web shell; Unity consumes the same versioned course schema.

See [physics validation](TRACK-BUILDER-CHECKS.md) and [crossover checks](CROSSOVER-CHECKS.md). Physical iPhone testing remains outstanding.

## Source and rebuilding

The complete editable Unity project is in [`unity/`](unity/), including the Rive source in `unity/Design/RacingHud/`. Open it with Unity **6000.6.3f1** plus official Web Build Support. Dependencies are pinned in the package manifest and lock file.

Run **Raptor Rally → Build Browser**, or invoke the editor with `-batchmode -nographics -projectPath /absolute/path/to/unity -buildTarget WebGL -executeMethod PrototypeBuilder.BuildWeb -quit -logFile /absolute/path/to/build.log`. The build appears at `../Web/`. Copy its `index.html`, `touch-controls.css`, `touch-controls.js`, `track-builder.css`, `track-builder.js`, `track-model.js`, `raptor-badge.jpg`, `Build/` and any `StreamingAssets/` into this directory, removing only obsolete generated build files.

The `RaptorPages` template supplies the loading screen and fullscreen button. Gzip with Unity's decompression fallback works without custom response headers. Native WebAssembly threads are disabled; no cross-origin-isolation headers or third-party hosting service are needed. Hashed build filenames prevent stale assets across deployments. GitHub Pages serves this folder from the repository's `main` branch.

All three truck models have amber running-light signatures, with white headlamp projectors and fog lamps. The same geometry and colors are used in the garage and during races.

## Assets and notices

Original game code and procedural track/truck geometry are covered by the repository MIT license. The wheel is generated reference-guided artwork; its provenance is in `unity/Assets/Resources/HUD/ART-PROVENANCE.md`. The models are stylized, not production CAD models. Ford names are used to identify the visual inspiration; this is an unofficial prototype.

Rive's official Unity runtime is MIT licensed ([notice](RIVE-LICENSE.txt)). Adobe Source Sans 3 is SIL OFL 1.1 ([notice](FONT-LICENSE.md)); font bytes are embedded in the Rive asset. Unity's generated player/runtime remains subject to Unity's applicable terms.

See [validation](VALIDATION.md) for tested behavior and platform limits.

## Performance update

The latest [Safari and render-pass optimization](performance/WEB-FIRST.md) consolidates truck materials, removes overlapping rail faces, caches the garage circuit preview, reduces shadow/HUD work and applies a 1600×900 web pixel budget. The Unity splash screen and logo are disabled; startup uses a black loading page followed by the Ford sharp-to-pixel intro.

The browser build now batches static scenery, indexes nearby barriers, reuses trucks and cached garage graphics, limits Rive refresh work and caps Retina/fullscreen rendering resolution. See [before/after results and test method](performance/RESULTS.md).

The latest [visual quality pass](performance/VISUAL-QUALITY.md) adds revised truck proportions, material/lighting detail, original dirt artwork, shaped barriers, pooled dust, contact shadows and corrected garage presentation while retaining the browser performance gains.

See [header and builder loading checks](NAV-BUILDER-CHECKS.md) for the garage badge alignment fix and transition feedback.
