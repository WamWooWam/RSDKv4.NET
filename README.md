# RSDKv4.NET
 A port of RSDKv4 to C# and XNA for Windows Phone 7 and desktop. Based on the [original decompilation](https://github.com/Rubberduckycooly/Sonic-1-2-2013-Decompilation), with hardware rendering based on Sonic CD.

## What works
- RSDK file reading
- 2D Rendering (sprites, blending, etc.)
- 3D Rendering (kinda)
- Animation
- Collision (mostly)
- Scripting/Objects (mostly)
- Keyboard input
- Music & Sound effects

## What doesn't
- Replays desync so something must be broken somewhere
    - Collision?
- Save Data
- Controller Input
- Touch Input
- Basically all native menus
- Anything related to on the fly palette changing is incredibly slow
- Some blending modes that aren't used by Sonic 1 & 2
- Music is buggy
- Some stage events dont trigger?

## What's not planned
- Mod support
- Text script interpreting
- Software rendering (too slow in C#)