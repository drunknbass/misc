# Mobile browser controls

The WebGL build detects a coarse pointer or a touch-capable viewport up to 1100 CSS pixels wide. It supplies a translucent bottom controller, responsive garage and race menus, and readable position/lap/time/speed/nitro instruments in portrait and landscape. Mobile begins in player-follow mode; the camera button switches to the whole track.

## Controls and implementation

- Left/right buttons steer. Gas accelerates. Brake slows the truck and becomes reverse once stopped. Nitro can be held alongside gas and steering.
- Pause, resume, truck selection, race start, recovery, retry and camera changes are available without a keyboard.
- Pointer capture maintains each held finger separately. Release, cancellation, lost capture, page hiding, focus loss, resizing and pause clear inputs. Controls remain disabled during the countdown.
- Safe-area insets and dynamic viewport height accommodate browser chrome and screen cutouts. Buttons remain at least 44 CSS pixels. Desktop keyboard controls and Rive instruments remain available.
- `RaptorTouch.jslib` publishes authoritative Unity state at 10 Hz. Input changes are sent immediately through `SendMessage`; a bit mask combines fingers. Mobile skips the desktop Rive rendering path and unused circuit preview. No new packages were added, and the existing web pixel budget is retained.

## Validation — September 29, 2026

- Unity 6000.6.3f1 WebGL release build succeeded.
- All 25 checks in `performance/mobile.json` passed using Chrome DevTools touch emulation: actual multi-touch event injection for gas + right + nitro, acceleration/charge use, release/cancel, pause/clock/resume, orientation clearing, left + brake, simulated focus loss, truck selection, garage return, cameras and desktop restoration.
- Inspected portrait and landscape screenshots. Layout assertions pass at 320×568, 568×320 and 844×390 CSS pixels; 390×844 was used for multi-touch gameplay. Driving buttons stay onscreen and at least 44×44, with no overlap between top instruments and actions.
- Desktop smoke passed at simulated 4K/Retina and 1600×900, including garage selection, keyboard driving/boost, camera, pause and fullscreen. No runtime errors; drawing buffer remains within 1600×900.
- Safari Technology Preview (WebKit, Safari 27.0) loaded the release and rendered portrait/landscape overlays; truck selection, start, combined control holds, acceleration/boost, focus-loss release and pause/resume worked. No console errors. This used a desktop Safari window with touch capability forced and synthetic keyboard holds on the controls, not physical multi-touch.
- Physical iPhone/iPad/Android hardware performance, mobile Safari browser chrome transitions and device safe-area behavior still require device testing. Browser emulation does not establish mobile FPS or thermal performance.

The dependency-free `performance/mobile.mjs` test expects Node 22+, a dedicated Chrome debugging endpoint at localhost:9334, a served build URL passed as its first argument, and a `work/performance/` output directory. It uses no npm dependencies. The earlier physics/contact test reports remain applicable; this change does not modify truck collision or suspension code.
