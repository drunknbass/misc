# Raptor Rally Rive HUD

Original editable racing HUD, authored with official Rive CLI 1.1.1.

## Integration contract

- Artboard: `RaceHUD`, 1600 × 900, transparent background; contain within the same letterboxed game HUD canvas.
- State machine: `RaceHUD`; view model: `RaceHUD`; instance: `Default`.
- Number properties: `speed` (mph, clamped visually to 0–80), `nitro` (0–100), `recovery` (0 hidden / 1 visible).
- String properties: `speedText`, `nitroText`, `position`, `lap`, `raceTime`, `truck`, `surface`.
- Boolean: `boosting`; drives a 120 ms, non-looping warm highlight transition. No idle pulse.
- No outgoing events, scripts, remote assets or account operations.
- Unity owns every value. Interpolated bars decorate exact numeric readouts; no fabricated speed, nitro refill, lap or finish events.
- `M` toggles reduced motion: immediate meter updates and no boost highlight transition. Pause freezes presentation. Garage stops updating/rendering the race HUD. Race reset clears interpolation and repopulates all fields.
- Keyboard/native IMGUI controls remain usable independently of the artwork, including pause, recovery and camera; a readable fallback HUD is retained if Rive initialization fails. Screen-reader support is not claimed or verified.

## Editable source and export

`scene.rml` and `rive.yaml` are authoritative editable Rive source. `build_source.py` reproduces this composition and is original project code. Manual RML changes must be retained or ported into the generator before rebuilding.

```
python3 build_source.py
rive . --verify --format=json
rive inspect . --json
rive . --once --format=json
```

The compiled artifact is `build/racing-hud.riv`. The identical runtime bytes are copied to `Assets/Resources/HUD/RacingHud.bytes` and loaded by `RiveRaceHud.cs`. This asset has no scripts and needs no signing service; no remote Rive file was created or modified. The RML remains editable without a cloud `.rev` export.

## Dependency provenance

Official Rive Unity runtime **0.5.1**, MIT licensed, pinned to commit `f18bf43f26c3346659d5130575ace91fa480b033` through Unity Package Manager. Source: https://github.com/rive-app/rive-unity/tree/v0.5.1 . Reviewed package manifest, native library platform, assembly definitions, import code and editor startup hooks. The package declares Unity Shader Graph; Unity's resolved versions and transitive official packages are recorded in `Packages/packages-lock.json`. Unity UGUI (requested 2.0.0, resolved to the editor's built-in 2.6.0) supplies the SDK's UI assembly references. No community plugin/template was added.

Fonts: Adobe **Source Sans 3**, Bold and Regular, from the official `adobe-fonts/source-sans` release branch. Font license is retained in `FONT-LICENSE.md` (SIL OFL 1.1). Source: https://github.com/adobe-fonts/source-sans/tree/release/TTF . Font bytes are embedded in the Rive artifact; no network font fetch occurs at runtime.

## Checks

CLI verification and resolved-object inspection; screenshots at 1600×900 and 960×540; bound-data readback for driving, nitro consumption, boost and recovery. Native Unity runtime verification is reported in the top-level `VALIDATION.md`.

CLI-only 300-frame benchmark at 1600×900: advance mean 0.001 ms; render mean 0.159 ms, p95 0.315 ms; zero reported WASM memory growth. These are local CLI measurements, not Unity frame-time or device performance claims.

## Embedded asset hashes (SHA-256)

- `SourceSans3-Bold.ttf`: `9214b9d95e4231c609802815c2646c98174e2102d0d37f88978a7f8e71006e6a`
- `SourceSans3-Regular.ttf`: `4644c81b86ec9caaa76b634889968ed3c4f4f52f054855933acc7c2b21e53b0f`
- `build/racing-hud.riv`: `d98d208227108d368146a4cfe4584f785a4cf5a51671b8095fab75fe2be63be6`
