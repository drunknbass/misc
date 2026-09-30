import AppKit
guard CommandLine.arguments.count == 3 else { fatalError("Usage: swift render-ford.swift input.svg output.png") }
let source = CommandLine.arguments[1]
let image = NSImage(contentsOfFile:source)!
let bitmap = NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:2048,pixelsHigh:800,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:0,bitsPerPixel:0)!
NSGraphicsContext.saveGraphicsState()
NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep:bitmap)
image.draw(in:NSRect(x:0,y:0,width:2048,height:800),from:.zero,operation:.copy,fraction:1)
NSGraphicsContext.restoreGraphicsState()
try bitmap.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:CommandLine.arguments[2]))
