# Driver-seat camera

Local implementation verified 2026-09-29 with Unity 6000.6.3f1. This report describes the updated browser build published alongside it.

## Behavior

C and the mobile camera button cycle **Whole track → Follow truck → Driver seat → Whole track**. Buttons label the current mode; mobile accessibility text additionally names the next mode. Desktop retains the whole-track default; mobile retains follow until the user chooses another mode. Selection survives retries, garage visits and truck changes. The intro ignores camera actions as before.

Driver seat uses a 62° vertical perspective FOV, a 0.04 m near plane, 3° downward aim and a level horizon. Position follows the interpolated chassis horizontally; vertical lag is limited to 0.055 m and yaw lag to 3°. Reduced motion removes both lags. Camera-mode cuts avoid moving through bodywork. Each cabin includes a left A-pillar joined to the roof, closed dashboard/door/floor geometry, the model-specific hood and vents, and a steering wheel driven by the existing steering animation. The redundant external wheel HUD is hidden only in cockpit mode. Race position, laps, timer, speed and nitro remain visible.

The external models have opaque window geometry and cosmetic body roll/bob, so the cockpit uses a separate procedural driving shell sized from their actual cabin dimensions. Only the cockpit view excludes the player's exterior model and enables its interior; other trucks and the player's other views retain the original bodywork. No physics or collision shapes were changed. No new dependency was added.

| Truck | Local left-hand eye (x, y, z) | Cabin |
| --- | --- | --- |
| F-150 | (-0.360, 0.960, 0.180) | Lower sloped windshield, blue raised hood/extractor |
| Bronco | (-0.345, 1.190, 0.050) | Taller upright windshield, orange flat hood/outer vents |
| Ranger | (-0.323, 0.960, 0.180) | Narrower pickup cabin, green hood/twin vents |

Coordinates are relative to the chassis origin (normally 0.92 m above the dirt on the grid), not world height. Cockpits are original stylized geometry, not production interior replicas.

## Verification

- **PASS — final WebGL build:** direct installed-editor batch command `CockpitVerification.VerifyAndBuild`, exit 0; log reports `COCKPIT VERIFICATION PASSED` and `Build Finished, Result: Success.` No Hub CLI command or Keychain grant.
- **PASS — automated Unity checks for each truck:** 3-mode cycle; perspective/near plane and visibility layers; eye inside the left-hand cabin; six central windshield rays clear of interior triangles; vertical/yaw bounds after movement; reduced-motion alignment; retry and garage transitions. Exact test output: `COCKPIT-CHECKS.md`.
- **PASS — mobile contracts:** Unity TouchAction cycles follow → driver seat → whole track. The isolated JavaScript bridge check verifies all three current-mode labels and bridged numeric modes. Both JavaScript files pass syntax checking. Bridge arity remains 13, replacing the old boolean following field with cameraMode 0/1/2.
- **PASS — actual browser driving at 1280×720:** keyboard acceleration captured F-150 at 22 mph, Bronco at 21 mph and Ranger at 21 mph. Inspected road visibility, hood/cabin framing, moving opponents and camera alignment for every truck. Applied steering in the F-150 and observed the wheel and road orientation change together. The initial floating pillar/roof join was found visually and corrected before the final build.
- **PASS — browser transitions:** cycled driver seat → whole track → follow, with original truck geometry and external wheel HUD restored. Returned from paused cockpit to the garage; starting another race retained driver-seat mode and did not replay the splash. Saved both intermediate states.
- **PASS — viewport check:** road framing remained usable at 844×390; viewport was reset afterward. This was a desktop browser resize, not a physical mobile-device test.
- **PASS — console:** no warning/error messages returned during final driving checks.
- **PASS — asset preservation:** supplied Ford/Raptor JPEG hashes remain unchanged. Latest pixel splash code/shaders were not edited. Deployment checkout `work/misc-pages` remains clean and was not modified.

No standalone macOS player or physical touch-device run was performed for this change. Driver-seat race completion/results were not separately captured; automated retry/menu checks and the existing results flow retain the same camera mode.

## Changed files

- `RaptorRally/Assets/Scripts/DriverCockpit.cs` (new procedural interior and resource lifecycle)
- `RaptorRally/Assets/Scripts/RaptorTruck.cs` (player-only cockpit and exterior render layer)
- `RaptorRally/Assets/Scripts/RaptorModel.cs` (reuse the existing geometry builder)
- `RaptorRally/Assets/Scripts/RallyGame.cs` (three modes, perspective placement/damping, labels and HUD visibility)
- `RaptorRally/Assets/Plugins/WebGL/RaptorTouch.jslib` (numeric mode bridge)
- `RaptorRally/Assets/WebGLTemplates/RaptorPages/{index.html,touch-controls.js}` (instructions and mobile labels)
- `RaptorRally/Assets/Editor/CockpitVerification.cs` (new focused geometry/state checks)
- Associated Unity metadata, README, source ZIP and regenerated Web build.

## Review artifacts

Open `cockpit-preview/index.html` for all captured views. `cockpit-preview/web-build-sha256.json` records the verified build's file hashes. `RaptorRally-source.zip` contains updated project source without generated Library/Temp/Logs/UserSettings directories.
