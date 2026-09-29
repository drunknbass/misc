# Prototype validation

Unity 6000.6.3f1

- PASS: F-150 RAPTOR / all four trucks finished three laps. Player AI time: 01:53.05. Peak height: 2.05 m. Recoveries: 0.
- PASS: BRONCO RAPTOR / all four trucks finished three laps. Player AI time: 01:52.61. Peak height: 2.03 m. Recoveries: 0.
- PASS: RANGER RAPTOR / all four trucks finished three laps. Player AI time: 01:52.57. Peak height: 2.03 m. Recoveries: 0.
- PASS: out-of-order gate rejection, reverse gate rejection, reset preserving progress, pause clock.
- PASS: throttle acceleration, nitro increasing acceleration/consuming charge, reverse driving.
- PASS: scoreboard garage, countdown, lap, pause, finish and leader agree with race state.
- PASS: steering wheel turns left/right, respects its limit and pause, and returns to center.
- PASS: steering wheel center and radius stay fixed at five angles across three HUD scales with letterboxing.
- PASS: follow camera zooms, frames and tracks the player; switching back restores the complete stadium view.
- PASS: all three trucks physically re-enter through inner and outer barriers on flat dirt and beside every raised jump; barriers re-enable after clearance and still block outward driving. Collision changes stay per truck.
- PASS: all three trucks cross every jump at full nitro speed within the lane; off-course gate crossing and manual recovery preserve progress.

Original physics-run module detection: Mac=False, Web=False. Official Web Build Support was subsequently installed for the GitHub Pages release below.

These are automated physics/AI checks. Human driving feel, gamepad, wheel input and browser behavior are separate validation tasks.

## Rive and visual validation

- PASS: official Rive CLI 1.1.1 verifies the final editable source with zero errors/warnings; compiled bytes match the Unity resource. Original source, embedded Adobe fonts and license are included.
- PASS: CLI bound-data readback and rendered states at 1600×900 and 960×540 for speed, nitro, boost and off-course recovery.
- PASS: Unity native Metal runtime loads the `RaceHUD` artboard/state machine/view model, receives real speed/nitro/recovery values, freezes the paused clock and disables the boost state in reduced-motion mode.
- PASS: native screenshots reviewed for garage, follow camera, race, recovery, boost and pause. Rive texture orientation and alpha handoff are handled explicitly. Raised road/shoulder surfaces and finish markings have separate visual elevations to avoid coplanar flicker.
- Runtime and physics checks cover the installed macOS Unity editor. Standalone Mac was not built because that build module is absent. Web player validation was completed subsequently as recorded below.
- Unity Editor SearchDatabase emitted its existing startup index exception; the game compiled, ran and completed the native HUD checks without game exceptions.

Evidence logs: `work/verify-reentry-rive-final.log` (physics) and `work/preview-rive-delivery.log` (final native visual run) in the working folder. CLI-only timing measurements and dependency provenance are in `RaptorRally/Design/RacingHud/README.md`.

## GitHub Pages web release — September 28, 2026

- PASS: Unity 6000.6.3f1 official Web Build Support installed through Unity Hub; release WebGL build completed successfully with IL2CPP, gzip decompression fallback, hashed filenames and native threads disabled.
- FIXED: retained Standard via a material resource; preserved SphereCollider/CapsuleCollider types required by runtime-generated primitives. These issues were found in the actual browser player rather than editor tests.
- PASS: final browser player starts with no new console warnings/errors; Rive reports its artboard, state machine and live view model ready. Track, truck models and HUD render.
- PASS: browser keyboard start, throttle (visible nonzero speed), camera toggle, pause with frozen race clock, resume and reduced-motion toggle. This is a browser smoke test, not a complete race or cross-browser certification.
- PASS: four generated resources total 9,586,021 bytes; HTML references resolve locally and all three compressed resources pass gzip integrity checks.
- Published target: `https://drunknbass.github.io/misc/raptor-rally/` in the existing `drunknbass/misc` main-branch Pages site. Final live-site verification is reported with delivery.
- Desktop keyboard controls are required. Touch controls, gamepads, Safari/Firefox testing and standalone Mac packaging remain outside this release.
