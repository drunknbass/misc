# Raptor Rally

A Unity arcade-racing prototype inspired by the full-track stadium presentation of the supplied Super Truck Rally / Sidewinder reference. Three selectable stylized 3D vehicles represent the **Ford F-150 Raptor, Bronco Raptor, and Ranger Raptor**.

## Play in Unity

1. Open this folder with **Unity 6000.6.3f1** (the installed editor on the SanDisk volume).
2. Open `Assets/Scenes/CoyoteBasin.unity`.
3. Press Unity's Play button, choose a truck, then **Start Race**.

The scene contains a bootstrap component. It constructs the stadium and truck geometry on entering Play mode; an empty edit-time scene is expected. Recreate the bootstrap scene through **Raptor Rally → Create or reset prototype scene** if needed. That menu action replaces the active scene.

| Control | Action |
| --- | --- |
| W / Up | Accelerate |
| S / Down | Brake, then reverse |
| A / D or Left / Right | Steer relative to the truck's heading |
| Space | Use limited nitro while grounded |
| R | Recover at the last checkpoint |
| Escape | Pause / resume |
| C / footer camera button | Switch whole-track view / zoomed player tracking |
| M / footer motion button | Toggle reduced HUD motion |
| 1 / 2 / 3 | Select truck in the garage |
| Return | Start from the garage |

Race three laps against three AI drivers. Complete checkpoints in order; reversing and recovering do not grant laps. AI racers continue after the player's finish. Best times are saved locally per vehicle and course version with Unity PlayerPrefs. These are separate for editor, Mac and browser.

## Scope and provisional choices

- One original course, **Coyote Basin**, with four long lanes, three hairpins, three main jumps, a low roller, textured dirt, timber ramp facing, blue course flags, red/white barriers, tiered stands, pit scenery, finish flags and a live race board.
- Rigidbody collision/gravity, arcade yaw steering and lateral grip. Pitch/roll are constrained for forgiving handling.
- Fictional balance values and original stylized models with model-specific body, grille, lighting, fender and wheel details. These are not measured Ford models or vehicle specifications.
- Keyboard controls only in this first pass. Gamepad and wheel input remain follow-up work.
- No sound, multiplayer, accounts, online leaderboard or hosted website.
- Track and truck geometry are original procedural code. The wheel HUD uses a generated image based on the user-provided Bronco Raptor photo. The racing instruments use original Rive artwork and the official Rive Unity runtime, pinned to version 0.5.1's commit; typography uses Adobe's OFL-licensed Source Sans 3. Dependency and asset provenance is recorded in `Design/RacingHud/README.md`.

## Build and verify

Use **Raptor Rally → Verify race simulation** for physics-driven races with every vehicle, checkpoint/reset/pause checks and module detection. It saves `../VALIDATION.md`. The AI-driven test is not a substitute for human handling review.

Build menu commands:
- **Raptor Rally → Build Mac** → `../Mac/Raptor Rally.app`
- **Raptor Rally → Build Browser** → `../Web/`

These commands require the corresponding official Unity build-support modules. Web output uses gzip compression with Unity’s decompression fallback for GitHub Pages and simple local serving. Native WebAssembly threads are disabled, so no custom isolation headers are required. After a successful Web build, serve the `Web` directory with an HTTP server (for example, the system Python's `python3 -m http.server 8080 --bind 127.0.0.1` from that directory), then open `http://127.0.0.1:8080`. Do not open the Web build as a `file://` URL.

For unattended verification with the installed editor:

```sh
"/Volumes/SanDisk/Unity/6000.6.3f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath "/absolute/path/to/RaptorRally" \
  -executeMethod PrototypeBuilder.Verify -quit -logFile "/absolute/path/to/verify.log"
```

## Source map

- `Assets/Scripts/Stadium.cs`: course centerline, ribbon mesh, jumps, scenery, materials.
- `Assets/Scripts/RaptorTruck.cs`: vehicle presets, driving, AI and checkpoint progress.
- `Assets/Scripts/RaptorModel.cs`: shaped body meshes, wheel-well cutouts, flares, FORD grilles, distinct lights and treaded wheels.
- `Assets/Scripts/GaragePreview.cs`: studio preview rendered from the same models used in races.
- `Assets/Scripts/RallyGame.cs`: garage/countdown/race/results states, keyboard input, HUD and local best times.
- `Assets/Scripts/RiveRaceHud.cs`: live race-state bindings, rendering, pause/reduced motion and disposal for the Rive instruments.
- `Design/RacingHud/`: editable RML, generator, fonts/license, compiled `.riv` and the runtime data contract.
- `Assets/Editor/PrototypeBuilder.cs`: scene creation, verification and platform builds.
- `Assets/Scripts/PreviewCapture.cs`: editor-only, opt-in screenshot capture for visual QA; excluded from builds.
- `Assets/Scripts/SteeringWheelHud.cs`: wheel image rendering and rotation around its fixed HUD center.
- `Assets/Resources/HUD/RaptorSteeringWheel.png`: transparent leather/carbon-fiber wheel art, generated from the user’s reference; prompt recorded in `ART-PROVENANCE.md` beside it.

Dependencies are pinned in `Packages/manifest.json` and `Packages/packages-lock.json`. Rive's official package brings Unity rendering dependencies; the package manager resolves these when opening the project. Generated `Library`, `Temp`, `Logs`, and `UserSettings` are ignored. The separate project brief records reference evidence, unknowns and follow-up scope.

## Stadium visual pass

The first art pass adds pickup bed rails, Bronco roof rails and a round spare, wider fenders, wheel hubs with roll and steering animation, and visual body movement. The course now uses an original 452 m switchback layout with procedurally textured dirt and gold jump-crest markers. Six-tier stands with access aisles, segmented floodlights, finish flags, a compact outside-lane service compound and a rear scoreboard give Coyote Basin a coherent miniature-stadium setting. The scoreboard uses the same race state, player lap and standings as the HUD.

The new track uses 384 samples and 48 sequential gates; three hairpins replace the oval. Truck collision shapes are unchanged; the re-entry and jump pass below updates barrier handling, ground contact and jump profiles. Scenery is visual only. Best-time keys use course version 3, preserving older course records separately. Vehicle geometry remains an original stylized placeholder. Dust, audio, gamepad support and a human handling review remain future work. See `../PROJECT-BRIEF.md` for reference evidence and visual priorities.

## Steering-wheel HUD

A detailed Bronco Raptor wheel with leather, carbon-fiber trim and button clusters sits at bottom right during races. Its red 12-o’clock stripe points up at center; the wheel turns smoothly with the player truck steering command (up to 135 degrees either way), freezes while paused, and centers on release or race reset. This is a visual input indicator; hardware wheel and gamepad support remain future work. The transparent PNG was created with the built-in image generator using the user’s photo. The original PNG is preserved; a runtime color conversion keeps black leather from washing out in Unity’s linear-color IMGUI rendering. Rotation is composed in HUD-local coordinates before screen scaling, so it stays anchored at its own center across window sizes. The old procedural wheel has been replaced.

## Vehicle models and camera tracking

The garage now shows a large live 3D preview of the selected truck, alongside a smaller circuit preview. The same model geometry runs on the track. F-150 has a long open bed, vented hood, C-shaped lamps and amber markers; Bronco has an upright enclosed cabin, round lamps, wider arches and a rear spare; Ranger has a smaller crew-cab pickup silhouette and its own grille/hood details. All models include shaped body panels with wheel cutouts, dark glazing, six-spoke wheels, tire tread, bumpers and skid plates. They remain stylized game models rather than detailed production replicas.

All three use **Raptor trim** as the exterior reference. The trim pass adds higher-coverage flares, darker off-road wheels with thicker tire sidewalls, fog lamps and model-specific details:

- **F-150 Raptor:** hood extractor, wide fenders, amber grille markers, dark FORD tailgate applique, dual exhaust, 17:35 wheel/tire proportions.
- **Bronco Raptor:** wide angular body-color flares, amber circular running lights and flare markers, white projector centers, hood extractors, dark bash plate, rear spare, 17:37 wheel/tire proportions. Its grille correctly reads FORD.
- **Ranger Raptor:** extra-large flares, twin hood vents, gray steel-style bumpers, body-color handles/mirror caps, dual rear exhaust, 17:33 wheel/tire proportions.

References: Ford's official [F-150 Raptor](https://www.ford.com/trucks/f150/models/raptor/), [Bronco Raptor](https://www.ford.com/suvs/bronco/models/raptor/) and [Ranger Raptor](https://www.ford.com/trucks/ranger/models/raptor/) pages, plus the [Bronco Raptor launch description](https://media.ford.com/content/fordmedia/fna/ca/en/news/2022/01/24/bronco-raptor.html) for amber signature lighting. These are visual proportions in the arcade scale, not dimensionally exact replicas or a simulation of the production suspension.

Press **C** or click the footer camera button during a race to transition smoothly between the complete stadium and a close view following your player truck. Tracking preserves the elevated viewing angle, includes a little look-ahead, and remains selected through retry. The garage always shows its studio layout. The default race mode remains the whole-track overview.

## Re-entry and jump handling

Off-course trucks can drive inward through any track barrier. Collision permission is per truck and per wall; it remains open until the entire truck clears the wall, then becomes solid again. On-track trucks are still blocked from driving outward. The infield is flush with the flat track, and graded shoulders allow re-entry beside raised jumps. Chassis ground probes preserve traction when climbing a bank. Recovery never grants checkpoint progress.

Jump crests now sit farther before turns, with lower, rounded landing profiles and capped upward velocity. Course-v3 best times preserve the older course records separately. Automated checks cover 24 physical re-entry scenarios (three trucks, both barrier sides, flat dirt plus all three jumps), outward blocking, collision restoration/isolation, and nine full-nitro jump runs.

## Rive racing instruments

The racing HUD now uses live Rive vector artwork for speed, nitro, position, lap, timer, selected truck, surface state and off-course guidance. Boost engages a short warm highlight; M disables decorative motion. The steering-wheel image still rotates at its fixed center. Garage, countdown, pause, results and keyboard controls remain Unity-owned. The game state drives every displayed value.

The editable `Design/RacingHud/scene.rml`, licensed fonts, runtime contract and `.riv` export ship with the source. Runtime bytes live in `Assets/Resources/HUD/RacingHud.bytes`. The official SDK is pinned to commit `f18bf43f26c3346659d5130575ace91fa480b033` (0.5.1). Actual native runtime checks and platform limits are recorded in `../VALIDATION.md`.

## GitHub Pages

The public browser build is published in [`drunknbass/misc`](https://github.com/drunknbass/misc/tree/main/raptor-rally) at [Raptor Rally](https://drunknbass.github.io/misc/raptor-rally/). `Assets/WebGLTemplates/RaptorPages/index.html` provides its loading screen, error display and fullscreen button.

The build retains a `Resources/RacingSurface.mat` shader reference because the track and trucks create their materials at runtime; otherwise Unity can strip Standard from player builds. Web assets use hashed filenames. Browser controls require a keyboard; there are no touch controls yet.
