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
- PASS: spatial recovery matches exhaustive collision rules across 240 positions, cell boundaries and teleports; at most 119 candidates instead of 768 per truck. Stadium has 33 mesh renderers with all 768 barriers retained.
- PASS: both rail ribbons close exactly at every cross-section with continuous normals, nondegenerate faces and no end caps; all 768 colliders retained. Each truck uses one shared material across five or six articulated meshes, with valid color, metallic, roughness and normal data.
- PASS: retry reuses the four truck models and resets charge, velocity, laps, finish state and pose.

Build modules: Mac=False, Web=True.

These are automated physics/AI checks. Human driving feel, gamepad, wheel input and browser behavior are separate validation tasks.

Browser release checks: all three truck presentations, Rive HUD, nitro driving, follow camera, reduced motion, pause, 4K resize and fullscreen passed with no console errors. The 4K test used a 1600×880 buffer and actual fullscreen used 1600×900. Unity splash screen and logo are disabled in saved settings and enforced by the build script; the first ready frame shows the garage. See performance/WEB-FIRST.md for measured render work and Safari Technology Preview results.
