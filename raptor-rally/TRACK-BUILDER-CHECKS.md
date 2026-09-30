# Track builder verification

- PASS: valid preset and all six pieces; rejects malformed, missing, duplicate, disconnected, corner-obstacle, unknown-piece and version-mismatched courses; finite nondegenerate closed geometry.
- PASS: F-150 RAPTOR completed 3 laps with all 4 AI drivers; time=43.64s, recoveries=0, max air=2.21m.
- PASS: BRONCO RAPTOR completed 3 laps with all 4 AI drivers; time=45.24s, recoveries=0, max air=2.18m.
- PASS: RANGER RAPTOR completed 3 laps with all 4 AI drivers; time=44.14s, recoveries=0, max air=2.23m.
- PASS: invalid edits leave current track intact; rebuild rebinds all trucks; closing keeps custom course; stock circuit restores and races.

- PASS: alternate 20-tile layout reaching every grid edge: F-150 completed three laps without recovery in 72.54s.
- PASS: mud produces additional physical deceleration versus flat dirt; nitro pad refills the truck reservoir.

- PASS: browser checks at 390×844 and 1280×800: edit terrain; draw and close a loop from scratch; reject corner obstacles; undo/redo; save/load a named course; restore draft after reload; export JSON and parse the downloaded file; import JSON; start custom race; change through driver-seat and overview cameras; return to original circuit. No game warnings/errors observed.
- PASS: JavaScript course rules, mobile driving/slide controls, Safari gesture guards and splash/camera bridge regression checks.
- LIMIT: browser testing uses the Codex in-app browser with phone-sized viewports. Physical iPhone/Safari touch testing remains outstanding. Local saves are per browser/device; export/import transfers courses.
