# misc

Small web experiments by [@drunknbass](https://x.com/drunknbass).

| Experiment | What it is | Try it |
|---|---|---|
| [DIG//PAGE](digpage/) | A bookmarklet that turns any web page into a Dig Dug-style level: dig through the real content and pump the page's own buttons until they pop. Works with the keyboard or an on-screen NES-style controller on phones. | https://drunknbass.github.io/misc/digpage/ |
| [RAPTOR RALLY](raptor-rally/) | A Unity stadium racer with three Raptor-inspired trucks, nitro, a following camera and a live Rive HUD. Keyboard required. | https://drunknbass.github.io/misc/raptor-rally/ |
| [FESTER'S QUEST](festers-quest/) | A Rive-driven ROM demo with Original and Modern visuals, keyboard and touch controls. | https://drunknbass.github.io/misc/festers-quest/ |

Original experiment code is MIT licensed. Third-party runtimes and assets retain their respective licenses; see each experiment’s notices.

To stage a reviewed static Fester's Quest demo for GitHub Pages, run `tools/stage-festers-quest.sh /absolute/path/to/static-demo`. The script rejects the shell-only `.not-publishable` build and requires the game, runtime, preview, and `release.json`; it accepts only the pinned official WebGL2 or Canvas 2.43.1 runtime files and checks their hashes before copying to an absent `festers-quest/` directory. This consistency check does not validate the Rive signature. Review the complete diff and test the nested `/misc/festers-quest/` path in a browser before publishing.
