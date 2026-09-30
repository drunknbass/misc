# Splash implementation and validation

Implemented 2026-09-29 in Unity 6000.6.3f1.

The intro lasts 11.7 seconds: an extruded pixel Ford oval rotates on black (0–4.7 s), followed by a fully black interval (4.7–5.05), the supplied Ford Performance artwork (5.05–7.85), black (7.85–8.2), the supplied Raptor badge (8.2–11.4), and black before entering the garage (11.4–11.7). Cards fade in and out. Skip uses a 0.28-second fade. Return, Escape, Space, the desktop skip button and the mobile skip button all target the intro. The Raptor badge is also the garage/title identity.

The emblem is one procedurally generated 3D mesh with front, back and silhouette side faces, sampled on a 104×40 grid. It has real depth, rotation and directional shading. Both original JPEGs are preserved byte-for-byte. GPU UV windows frame the artwork without rewriting image pixels. The Ford source is 720×1280; the badge is 339×296. The texture importer preserves those dimensions.

## Actual verification

- PASS: editor compiled the scripts and initialized the intro in the first preview attempt. An incorrect expected badge size in the QA helper stopped that attempt; the expectation and mobile crop proportions were corrected.
- PASS: complete WebGL build via the installed editor, without Hub CLI. Process exit 0; build log reports `Build Finished, Result: Success.` The updated playable files are in `Web/` beside this report.
- PASS: rebuilt WebGL game rendered in Codex’s in-app browser on this Mac at 1280×720. Visually inspected voxel front, rotated back/edge, final front, Ford Performance card, Raptor badge and branded garage. Captured a fully black transition frame.
- PASS: actual browser keyboard Escape skipped directly to the garage; a separate Enter then started the race countdown. After starting, Escape paused and Back to Garage returned immediately without replaying the intro. No browser warning/error messages were returned during these checks.
- PASS: JavaScript syntax checks for touch-controls.js and RaptorTouch.jslib. An isolated Node DOM-stub check verified mobile intro hides garage/race/controller, skip visibility follows the bridge flag, clicking skip sends TouchAction("skip"), and garage/countdown visibility returns afterward. This is a bridge behavior check, not a physical mobile-device test.
- PASS: SHA-256 equality between supplied reference JPEGs and resource/template copies.
- Static review: Intro appends phase value 4, preserving phases 0–3. C# and WebGL bridge both have 13 matching parameters. Intro guards StartRace, Garage, touch actions, touch throttle and race ticking. All trucks are kinematic during startup. The intro advances on unscaled time and disposes its temporary mesh/material/camera/texture when complete. Existing dust/Rive updates are excluded during intro.

## Limits

The Mac was locked during the second native editor preview attempt, and computer-use inspection reported that limitation. That attempt was stopped; browser rendering and a headless WebGL build succeeded afterward. A standalone macOS player was not built or visually tested. The opt-in SplashCapture native runtime assertions did not complete, so its full assertions (including paused-clock and direct guard checks) are not claimed as passing. Mobile rendering/touch on a real device was not tested. No Keychain access was granted and no Hub CLI command was used for this validation. No deployment or push was performed.

## Changed source files

- Assets/Scripts/StartupIntro.cs — artwork framing, extruded pixel mesh, animation, timing, skip and cleanup.
- Assets/Scripts/RallyGame.cs — Intro phase, gameplay/input guards, menu branding, WebGL state bridge.
- Assets/Scripts/RallyDust.cs and RiveRaceHud.cs — prevent effects/HUD activity during intro.
- Assets/Scripts/PreviewCapture.cs — existing race capture waits for intro completion.
- Assets/Scripts/SplashCapture.cs — opt-in editor QA helper, excluded from player builds.
- Assets/Editor/BrandArtImporter.cs — explicit sRGB/uncompressed imports, readable Ford source, original dimensions.
- Assets/Resources/Brand/ — supplied JPEGs, Artwork.shader and VoxelEmblem.shader, associated Unity metadata.
- Assets/Plugins/WebGL/RaptorTouch.jslib — intro skip visibility flag.
- Assets/WebGLTemplates/RaptorPages/{index.html,touch-controls.js,touch-controls.css,raptor-badge.jpg} — loading/intro visibility, mobile skip and branded garage.
- README.md — startup instructions and source map.

## Preview

Open `splash-preview/index.html` for the captured stages, or serve `Web/` over HTTP to replay the actual build. Screenshots are unmodified browser captures. `RaptorRally-source.zip` contains the updated project without generated Library/Temp/Logs/UserSettings data.
