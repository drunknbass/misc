# Header and builder loading fixes

The garage badge applied the GUI transform twice, so its position drifted into the title as screen scale and letterboxing changed. Artwork now converts its rectangle once and draws with an identity GUI matrix, then restores the original matrix for the rest of the HUD.

Builder actions now show a blocking, accessible loading status immediately. Two animation frames allow it to paint before calling Unity. Opening also displays the editor shell immediately, while Unity publishes its ready state directly. Opening, building and returning have separate messages. Duplicate requests and editing during generation are blocked. Engine errors, send failures and a 20-second timeout release the loading state and allow retry. A reduced-motion preference disables spinner animation.

Validation:
- Unity 6000.6.3f1 WebGL build succeeded.
- Browser header verified at 1280×720 and 1600×800; badge and title remain separate.
- Browser editor opening, custom-track generation, reopening and closing passed, including 390×844 layout. No browser warnings/errors observed.
- Node tests cover immediate feedback, paint-before-send scheduling, duplicate requests, old state while opening, editor locking, build errors, timeout/retry, synchronous acknowledgement and closing.
- Existing course model, pedal, gesture and splash/camera bridge checks pass. Physical iPhone testing was not performed.

Run `node tests/check-builder-loading.cjs` from the deployed source folder to reproduce the transition tests.
