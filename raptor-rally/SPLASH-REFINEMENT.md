# Splash refinement — verified local build

## Fixed defect

The previous mesh used source brightness > 0.13 as a geometry occupancy test. Reproducing that exact 104×40 mask against the imported original JPEG found three enclosed missing cells: (60,15), (61,15), (64,17). The adjacent first two form one opening; the third forms another. These two openings match the two bright rear flecks in the prior browser capture. The old front depth also varied by 0.004 per cell without faces sealing those height differences.

The new mesh fills the interior between each row's silhouette endpoints and uses coplanar front faces with identical shared-edge coordinates. It preserves the stepped outer boundary. Unity verification reports **2,151 occupied cells, zero open/non-manifold edges, and 0.42 depth**. Browser captures from both rear angles and near the direct rear show no isolated flecks; both sides of the rotation show continuous geometry.

## Visual changes

- Ford grid: 104×40 → 88×34, approximately 18% larger cells at the same overall width. Ford lettering remains legible.
- The voxel shader adds a restrained view/light-dependent specular highlight with small, fixed per-cell normal tilts. The highlight moves with rotation; there is no time-driven flashing. Surface normals change only shading, not geometry.
- Ford Performance is sampled on a 192×76 grid; the Raptor badge uses 116×96. A shader snaps UV coordinates to cell centers within the original crop, keeping every cell's color constant. Original JPEG bytes are unchanged.
- Desktop menu badge uses the same shader/grid. The mobile menu draws the unchanged JPEG into a 116×96 canvas, using the same crop and nearest sampling, displayed with pixelated scaling.
- Intro durations, full-black transitions, fades, skip handling and gameplay input guards are unchanged.

## Verification completed

- Unity 6000.6.3f1 direct editor batch run: `SplashMeshVerification.VerifyAndBuild`; exit 0, `Build Finished, Result: Success.` No Hub CLI or Keychain grant.
- Unity mesh topology, solid depth and black-interval checks passed; exact results are in `splash-refinement-mesh.txt`.
- Visually inspected 13 browser frames covering the front, both edges, rear from multiple angles, return to front, full black, both later cards and menu at 1280×720. The rear regression is absent; all three logo styles are pixelated.
- Actual browser Enter during the intro returned to the garage without starting a race. A separate Enter started the countdown. The saved skip screenshot confirms the intermediate garage state.
- No warning/error messages were returned by the browser log check during the captured sequence.
- Existing isolated mobile bridge check passed, and the updated JavaScript passed syntax checking. Mobile canvas rendering was statically reviewed; no physical touch device was tested.
- SHA-256 of both source JPEGs still matches the supplied references (and the mobile badge copy).

## Files changed for this refinement

- `RaptorRally/Assets/Scripts/StartupIntro.cs`
- `RaptorRally/Assets/Resources/Brand/VoxelEmblem.shader`
- `RaptorRally/Assets/Resources/Brand/Artwork.shader`
- `RaptorRally/Assets/Editor/SplashMeshVerification.cs` and its Unity metadata
- `RaptorRally/Assets/WebGLTemplates/RaptorPages/index.html`
- `RaptorRally/Assets/WebGLTemplates/RaptorPages/touch-controls.js`
- `RaptorRally/Assets/WebGLTemplates/RaptorPages/touch-controls.css`
- Updated README, source ZIP and the local `Web/` build.

Native standalone macOS and physical mobile-device testing were not performed. These checks cover the rebuilt browser files published alongside this report.
