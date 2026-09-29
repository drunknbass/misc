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

## Browser visual quality acceptance

Same Chrome/Apple M2 Pro hardware renderer and dependency-free benchmark as the optimization pass; viewport 1600×900, simulated 4× CPU slowdown. The sample covers 15 seconds of full-track racing.

| Metric | Optimized prior release | Visual upgrade |
| --- | ---: | ---: |
| Mean frame rate | 60.0 FPS | 60.0 FPS |
| 95th-percentile frame interval | 16.8 ms | 16.8 ms |
| Mean WebGL draw calls / frame | 483 | 495 |
| Frames above 33.4 ms | 0 | 0 |

Draw calls increase 2.6% while the sampled race remains at 60 FPS. Follow mode also averages 60 FPS. No console errors or race long tasks were recorded. Startup still has a shader/GC warm-up hitch; CPU throttling is a comparison on this computer, not a guarantee for other devices.

Browser smoke checks cover both alternate vehicle previews, nitro driving, follow camera, reduced motion, pause, 4K resizing and actual fullscreen. Physical acceptance still passes all three complete races, 24 inward barrier re-entry scenarios, nine full-nitro jumps, checkpoint rules, wheel pivot, camera tracking, spatial-index equivalence and retry reuse. All 768 physical barriers remain unchanged; the stadium still has 37 mesh renderers.

The compressed browser payload is 10,251,356 bytes. No new package dependencies were added. Ground artwork was generated with the built-in imagegen tool; its unedited source and exact prompt are in [Surface/ART-PROVENANCE.md](unity/Assets/Resources/Surface/ART-PROVENANCE.md).
