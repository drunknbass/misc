# Rive web runtime provenance

The browser shell self-hosts one official Rive runtime pinned to version **2.43.1**. The release default is `@rive-app/canvas`, which passed signed gameplay, graphics-theme, controls, mute, and pause checks; `@rive-app/webgl2` remains an explicit diagnostic build option after repeated context loss in this game. The active package and exact runtime-file hashes are recorded in `release.json` for signed builds. Both are published by Rive from `https://github.com/rive-app/rive-wasm`, use the MIT license, and report no runtime dependencies or install scripts.

The official npm tarball integrity values are:

- `@rive-app/webgl2@2.43.1`: `sha512-Er2zB3bH8CIyNoHEMLU5dAtx2Ie1t4wvxOFhTIrOfr8Cya7HE5lFzAYBozSwHtBDnZ7/6NwT4oz6CuPyR06u9g==`
- `@rive-app/canvas@2.43.1`: `sha512-GqENtpSOK5KOnF+kmzDQ70tfEQYksSOb39jcPPAV5vj5XDJ3vekPdbqTWuQdEyOFMhJUir4iBo3On9Gy2hghtQ==`

`runtime/rive.js`, `runtime/rive.wasm`, and `runtime/rive_fallback.wasm` in the staged site are copied without modification from the selected pinned tarball. `build.mjs` verifies their SHA-256 hashes before staging. The JavaScript/WASM pair stays at the same package version and loads from the site itself, with no CDN fallback.

The official MIT license text for the runtime is shipped at [`runtime/LICENSE`](runtime/LICENSE), sourced from the Rive `rive-wasm` 2.43.1 release tag. The Rive game source, artwork, and ROM retain their existing ownership.

The `.riv` file is a separate artifact from the editable project. Because it contains Luau scripts, it must be signed by the official Rive CLI for web execution. The shell-only build intentionally omits it and marks `dist/` as not publishable.
