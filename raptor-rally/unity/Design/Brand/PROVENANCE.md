# Intro artwork

FordSignature-official.svg was retrieved from Ford's official From the Road site on 2026-09-30:
https://www.fromtheroad.ford.com/content/dam/fordmediasite/sample/site/logos/Ford-logo-white.svg

The vector path is preserved in FordOval.svg. The blue gradient oval and silver edging are presentation artwork matching the supplied Ford reference; this composed oval is not represented as a separately supplied official Ford asset. The vector is rasterized at 2048×800 using macOS AppKit (render-ford.swift) for Unity. The same texture is sampled sharply and on an 88×34 grid during the intro wipe.

RaptorBadge.jpg remains the user's original image. Desktop menus sample it without a pixel grid; mobile menus draw its 292×242 source crop with smooth scaling. No AI upscaling or additional packages were used.

From the Unity project root, reproduce with `swift Design/Brand/render-ford.swift Design/Brand/FordOval.svg Assets/Resources/Brand/FordOval.png`.
