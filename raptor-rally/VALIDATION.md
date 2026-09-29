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
- PASS: spatial recovery matches exhaustive collision rules across 240 positions, cell boundaries and teleports; at most 119 candidates instead of 768 per truck. Stadium has 37 mesh renderers with all 768 barriers retained.
- PASS: retry reuses the four truck models and resets charge, velocity, laps, finish state and pose.

Build modules: Mac=False, Web=True.

These are automated physics/AI checks. Human driving feel, gamepad, wheel input and browser behavior are separate validation tasks.

## Optimized WebGL release

- PASS: release WebGL player builds with original vertex-color stadium shader. Browser benchmark and smoke checks captured no errors.
- PASS: changing garage models refreshes the cached preview; racing, nitro, follow camera, pause and reduced motion render correctly.
- PASS: 4K/Retina and fullscreen drawing-buffer sizes remain within the 1920×1080 budget.
- Actual before/after measurements and limits are documented in `performance/RESULTS.md` in the public repository and `PERFORMANCE.md` in the output folder.
