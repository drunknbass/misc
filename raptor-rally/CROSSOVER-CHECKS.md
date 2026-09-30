# Crossover verification

- PASS: reserved approaches; legacy validation; separate upper/lower nearest routes; unobstructed lower surface and solid upper deck; gates reject the wrong deck.
- PASS: three laps with four AI trucks: F-150 RAPTOR time=72.90 recoveries=0 airFrames=94 maxY=6.59 gate=1 laps=3
- PASS: three laps with four AI trucks: BRONCO RAPTOR time=73.36 recoveries=0 airFrames=97 maxY=6.63 gate=1 laps=3
- PASS: three laps with four AI trucks: RANGER RAPTOR time=75.66 recoveries=0 airFrames=147 maxY=7.89 gate=1 laps=3
- PASS: 20 m/s approach with nitro held: F-150 RAPTOR cleared jump and landed on exit; max chassis height=6.61m.
- PASS: 20 m/s approach with nitro held: BRONCO RAPTOR cleared jump and landed on exit; max chassis height=6.64m.
- PASS: 20 m/s approach with nitro held: RANGER RAPTOR cleared jump and landed on exit; max chassis height=6.59m.

- PASS: browser editor draws and closes a figure-eight from scratch, preserves both passes through its junction, rejects turning at the crossing and painting obstacles on its approaches, saves and restores the draft after reload, and exports/imports a version 2 course unchanged.
- PASS: desktop and 390×844 layouts; actual built WebGL player generates and races the custom crossover with no browser warnings/errors observed.
- PASS: JavaScript course validation and mobile pedal, gesture, splash and camera regression checks.
- LIMIT: phone-sized browser testing is not physical iPhone/Safari verification. Crossovers require perpendicular straight routes with clear straight approaches; east–west always uses the upper jump bridge. Legacy version 1 files remain supported.

Reproduce physics checks with the installed Unity editor: `-batchmode -nographics -projectPath /path/to/unity -buildTarget WebGL -executeMethod CrossoverVerification.VerifyAndBuild -quit`. Run `CrossoverVerification.VerifyNitro` separately for the boosted approach checks. JavaScript checks: `node tests/check-track-model.cjs` from this folder.
