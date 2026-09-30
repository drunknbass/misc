# Ford intro wipe checks

- PASS: 2048×800 sharp Ford artwork, fade-in, top-to-bottom reveal timeline, pixel hold, fade-out and completion.
- PASS: only Ford/black states; Skip stays present during the entire intro including fades, wipe and black tail. Skip works before, during and after the wipe and freezes the reveal during its exit fade.
- PASS: Unity WebGL build, editor play-mode input guards, unscaled clock, automatic garage entry and no replay on retry.
- PASS: built web player displays the sharp opening and sharp menu badge; touch Skip exits into the garage. No browser warnings/errors observed.
- PASS: JavaScript intro bridge, pedal controls and builder loading regression checks.
- Visual inspection confirms a top-to-bottom pixel reveal. First native editor capture displayed Unity's temporary shader-compilation cyan; the built WebGL opening was checked separately and renders correctly. Editor QuickSearch also logged an unrelated startup indexing exception.
- Physical iPhone/Safari testing was not repeated for this release.
