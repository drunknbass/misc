# Open jump verification

- PASS: jump recovery restores the truck to solid ground before the takeoff run-up.
- PASS: reserved approaches; legacy validation; separate upper/lower nearest routes; unobstructed lower surface and open jump gap; gates distinguish the airborne and ground-level routes.
- PASS: three laps with four AI trucks: F-150 RAPTOR time=72.56 recoveries=0 airFrames=120 maxY=5.15 gate=1 laps=3
- PASS: three laps with four AI trucks: BRONCO RAPTOR time=72.90 recoveries=0 airFrames=120 maxY=5.15 gate=1 laps=3
- PASS: three laps with four AI trucks: RANGER RAPTOR time=73.70 recoveries=0 airFrames=119 maxY=5.16 gate=1 laps=3
- PASS: 20 m/s approach with nitro held: F-150 RAPTOR cleared jump and landed on exit; max chassis height=5.30m.
- PASS: 20 m/s approach with nitro held: BRONCO RAPTOR cleared jump and landed on exit; max chassis height=5.38m.
- PASS: 20 m/s approach with nitro held: RANGER RAPTOR cleared jump and landed on exit; max chassis height=5.37m.

The earlier bridge has been replaced by two dirt ramps with a genuinely open gap across the junction. Road triangles, rail meshes, barriers and surface markings all stop at the gap. Dirt banks have no bridge supports or deck. The center raycast reaches the lower track. Trucks use their sequential route when navigating the flight and recovery returns them to the run-up.

JavaScript course rules and mobile pedal, gesture, splash and camera regression checks pass. The version 2 file format remains compatible: existing crossing courses now generate open jumps. No dependencies were added.

Browser verification: the final WebGL build renders two dirt ramps and a visible open gap with clean ramp shading. The updated editor displays a dashed flight arc, the Figure eight · jump preset, and carry-speed guidance. No browser warnings/errors were observed. Physical iPhone testing was not repeated for this change.
