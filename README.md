# RSDKv4.NET
 A port of RSDKv4 to C# and XNA for Windows Phone 7 and desktop. Based on the [original decompilation](https://github.com/Rubberduckycooly/Sonic-1-2-2013-Decompilation), with hardware rendering based on Sonic CD.

## What works
- RSDK file reading (both v4 and v4u)
- 2D Rendering (sprites, blending, etc.)
- 3D Rendering
- Animation
- Collision
- Scripting/Objects (mostly)
- Keyboard input
- Touch Input
- Save Data
- Music & Sound effects
- 40% of Native Menus
- Builds and runs flawlessly using NativeAOT on .NET 7

## What doesn't
- Controller Input
- 60% of Native Menus
- On the fly palette changing is incredibly slow on some hardware
    - There are two palette implementations, FAST_PALETTE uses a shader, default does not. 
    - FAST_PALETTE is unsupported on the XNA Reach profile (Windows Phone 7)
    - Another solution will be required
- Some background modes that aren't used by Sonic 1 & 2

## What's not planned
- Mod support
- Text script interpreting
- Software rendering (too slow in C#)