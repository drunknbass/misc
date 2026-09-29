# Visual quality pass — September 28, 2026

This release improves the original Unity scene with shaped concrete barriers, natural granular dirt, revised Raptor proportions, smooth tire shoulders, grille/bed/wiper/tow-loop details, reflective painted bodywork and lower-panel dust. A small procedural environment cubemap provides reflection detail without realtime probe rendering. Warm directional light, sky/ground ambient fill, two high-resolution shadow cascades and a pooled dust/contact-shadow mesh add depth.

The garage now uses separate studio lighting and correctly samples its camera texture in linear space. Removing a second gamma-to-linear conversion restores detail in the tires, grille and bodywork. Its circuit camera and background now follow the UI's letterboxed layout, including narrow browser panes.

The trucks remain original stylized Raptor-inspired meshes. This is a visual refinement of the demo, not a claim of photorealistic production models.

## Browser validation

Same Chrome/Apple M2 Pro hardware renderer and dependency-free benchmark as the optimization pass; viewport 1600×900, simulated 4× CPU slowdown. The sample covers 15 seconds of full-track racing.

| Metric | Optimized prior release | Visual upgrade |
| --- | ---: | ---: |
| Mean frame rate | 60.0 FPS | 60.0 FPS |
| 95th-percentile frame interval | 16.8 ms | 16.8 ms |
| Mean WebGL draw calls / frame | 483 | 495 |
| Frames above 33.4 ms | 0 | 0 |

Draw calls increase 2.6% while the sampled race remains at 60 FPS. Follow mode also averages 60 FPS. No console errors or race long tasks were recorded. Startup still has a shader/GC warm-up hitch; CPU throttling is a comparison on this computer, not a guarantee for other devices.

Browser smoke checks cover both alternate vehicle previews, nitro driving, follow camera, reduced motion, pause, 4K resizing and actual fullscreen. Physical acceptance still passes all three complete races, 24 inward barrier re-entry scenarios, nine full-nitro jumps, checkpoint rules, wheel pivot, camera tracking, spatial-index equivalence and retry reuse. All 768 physical barriers remain unchanged; the stadium still has 37 mesh renderers.

The compressed browser payload is 10,251,356 bytes. No new package dependencies were added. Ground artwork was generated with the built-in imagegen tool; its unedited source and exact prompt are in [Surface/ART-PROVENANCE.md](../unity/Assets/Resources/Surface/ART-PROVENANCE.md).
