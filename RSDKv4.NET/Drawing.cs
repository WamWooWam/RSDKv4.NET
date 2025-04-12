using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.External;
using RSDKv4.Native;
using RSDKv4.Render;
using RSDKv4.Utility;

namespace RSDKv4;

public abstract class Drawing
{
    public const int SURFACE_MAX = 24;

    public const int TEXTMENU_SURFACE = SURFACE_MAX - 1;

    public const int VERTEX_LIMIT = 0x4000;
    public const int INDEX_LIMIT = VERTEX_LIMIT * 6;
    public const int VERTEX3D_LIMIT = 0x1904;
    public const int TEXBUFFER_SIZE = 0x100000;
    public const int TILEUV_SIZE = 0x4000;

    public const int PALETTE_COUNT = 0x8;
    public const int PALETTE_SIZE = 0x100;

    public const int GFXDATA_MAX = 0x800 * 0x800;

    public const int SCREEN_XSIZE = 400;
    public const int SCREEN_CENTERX = SCREEN_XSIZE / 2;

    public const int SCREEN_YSIZE = 240;
    public const int SCREEN_CENTERY = SCREEN_YSIZE / 2;

    public int GFX_LINESIZE = 0;
    public int GFX_LINESIZE_MINUSONE = 0;
    public int GFX_LINESIZE_DOUBLE = SCREEN_YSIZE / 2;

    public const int SURFACE_LIMIT = PALETTE_COUNT;
    public const int SURFACE_SIZE = 1024 + 256;

    public const int SURFACE_DATASIZE = SURFACE_SIZE * SURFACE_SIZE * sizeof(short);

    public byte[] graphicsBuffer = new byte[SURFACE_DATASIZE];
    public int graphicsBufferPos = 0;

    public byte[] tilesetGFXData = new byte[Scene.TILESET_SIZE];

    public SurfaceDesc[] surfaces = new SurfaceDesc[SURFACE_MAX];

    public ushort[][] fullPalette = new ushort[PALETTE_COUNT][];
    public Color[][] fullPalette32 = new Color[PALETTE_COUNT][];

    public bool surfaceDirty = true;

#if ENABLE_3D
    public DrawVertex3D[] vertexList3D = new DrawVertex3D[VERTEX3D_LIMIT];
    public ushort vertexCount3D = 0;
    public ushort indexCount3D = 0;

    public bool isRender3DEnabled;
    public Vector3 floor3DPos;
    public float floor3DAngle;
#endif

    public int texPaletteNum = 0;

    protected FileIO FileIO;
    protected Animation Animation;
    protected Objects Objects;
    protected Font Font;
    protected Palette Palette;
    protected Scene Scene;
    protected Script Script;
    protected Input Input;
    protected Hooks Hooks;

    protected NativeRenderer Renderer;

    protected GifLoader GifLoader;

    public Drawing()
    {
        Helpers.Memset(surfaces, () => new SurfaceDesc());
        Helpers.Memset(fullPalette, () => new ushort[PALETTE_SIZE]);
        Helpers.Memset(fullPalette32, () => new Color[PALETTE_SIZE]);
    }

    public virtual void Initialize(Engine engine)
    {
        Hooks = engine.hooks;
        FileIO = engine.FileIO;
        Animation = engine.Animation;
        Objects = engine.Objects;
        Font = engine.Font;
        Palette = engine.Palette;
        Scene = engine.Scene;
        Script = engine.Script;
        Input = engine.Input;
        Renderer = engine.Renderer;

        GifLoader = new GifLoader(engine.FileIO);
    }

    public static ushort RGB_16BIT5551(byte r, byte g, byte b, byte a) => (ushort)((a << 15) + (r >> 3 << 10) + (g >> 3 << 5) + (b >> 3));

    public void ClearGraphicsData()
    {
        for (int index = 0; index < SURFACE_MAX; ++index)
            surfaces[index].fileName = null;

        graphicsBufferPos = 0;
        surfaceDirty = true;
    }

    public bool CheckSurfaceSize(int size)
    {
        for (int cnt = 2; cnt < 2048; cnt <<= 1)
        {
            if (cnt == size)
                return true;
        }
        return false;
    }

    public int AddGraphicsFile(string filePath)
    {
        surfaceDirty = true;

        //char sheetPath[0x100];
        var sheetPath = "Data/Sprites/" + filePath;
        int sheetId = 0;
        while (surfaces[sheetId].fileName != null)
        {
            if (surfaces[sheetId].fileName == sheetPath)
                return sheetId;
            if (++sheetId == SURFACE_MAX) // Max Sheet cnt
                return 0;
        }

        //byte fileExtension = (byte)sheetPath[(StrLength(sheetPath) - 1) & 0xFF];
        switch (filePath[filePath.Length - 1])
        {
            case 'f': LoadGIFFile(sheetPath, sheetId); break;
            case 'p': LoadBMPFile(sheetPath, sheetId); break;
            //case 'r': LoadPVRFile(sheetPath, sheetId); break;
            default: throw new NotSupportedException();
        }

        return sheetId;
    }

    public void RemoveGraphicsFile(string filePath, int sheetId)
    {
        if (sheetId < 0)
        {
            for (int i = 0; i < SURFACE_MAX; i++)
                if (surfaces[i].fileName == filePath)
                    sheetId = i;
        }

        if (sheetId >= 0 && surfaces[sheetId].fileName != null)
        {
            var surface = surfaces[sheetId];

            int dataPosStart = surface.dataPosition;
            int dataPosEnd = surface.dataPosition + surface.height * surface.width;

            Array.Copy(graphicsBuffer, dataPosStart, graphicsBuffer, dataPosEnd, SURFACE_DATASIZE - dataPosEnd);

            //for (int i = SURFACE_DATASIZE - dataPosEnd; i > 0; --i)
            //    graphicsBuffer[dataPosStart++] = graphicsBuffer[dataPosEnd++];

            graphicsBufferPos -= surface.height * surface.width;

            for (int i = 0; i < SURFACE_MAX; ++i)
            {
                if (surfaces[i].dataPosition > surface.dataPosition)
                    surfaces[i].dataPosition -= surface.height * surface.width;
            }

            surfaces[sheetId] = new SurfaceDesc();
        }

        surfaceDirty = true;
    }

    public void LoadBMPFile(string fileName, int surfaceNum)
    {
        if (!FileIO.LoadFile(fileName, out var info))
            return;

        var surface = surfaces[surfaceNum];
        surface.fileName = fileName;

        FileIO.SetFilePosition(18);

        surface.width = FileIO.ReadInt32();
        surface.height = FileIO.ReadInt32();

        FileIO.SetFilePosition((int)((ulong)info.fileSize - (ulong)(surface.width * surface.height)));

        surface.dataPosition = graphicsBufferPos;

        int index1 = surface.dataPosition + surface.width * (surface.height - 1);
        for (int index2 = 0; index2 < surface.height; ++index2)
        {
            for (int index3 = 0; index3 < surface.width; ++index3)
            {
                graphicsBuffer[index1] = FileIO.ReadByte();
                ++index1;
            }
            index1 -= surface.width << 1;
        }
        graphicsBufferPos += surface.width * surface.height;
        if (graphicsBufferPos >= SURFACE_DATASIZE)
            graphicsBufferPos = 0;

        FileIO.CloseFile();
    }

    public void LoadGIFFile(string fileName, int surfaceNum)
    {
        byte[] byteP = new byte[3];
        bool interlaced = false;
        if (!FileIO.LoadFile(fileName, out _))
            return;

        SurfaceDesc surface = surfaces[surfaceNum];
        surface.fileName = fileName;

        FileIO.SetFilePosition(6);

        byteP[0] = FileIO.ReadByte();
        int num1 = byteP[0];
        byteP[0] = FileIO.ReadByte();
        int width = num1 + (byteP[0] << 8);
        byteP[0] = FileIO.ReadByte();
        int num2 = byteP[0];
        byteP[0] = FileIO.ReadByte();
        int height = num2 + (byteP[0] << 8);
        byteP[0] = FileIO.ReadByte();
        byteP[0] = FileIO.ReadByte();
        byteP[0] = FileIO.ReadByte();
        for (int index = 0; index < 256; ++index)
            FileIO.ReadFile(byteP, 0, 3);

        byteP[0] = FileIO.ReadByte();
        while (byteP[0] != 44)
            byteP[0] = FileIO.ReadByte();
        if (byteP[0] == 44)
        {
            FileIO.ReadFile(byteP, 0, 2);
            FileIO.ReadFile(byteP, 0, 2);
            FileIO.ReadFile(byteP, 0, 2);
            FileIO.ReadFile(byteP, 0, 2);
            byteP[0] = FileIO.ReadByte();
            if ((byteP[0] & 64) >> 6 == 1)
                interlaced = true;
            if ((byteP[0] & 128) >> 7 == 1)
            {
                for (int index = 128; index < 256; ++index)
                    FileIO.ReadFile(byteP, 0, 3);
            }
            surface.width = width;
            surface.height = height;
            surface.dataPosition = graphicsBufferPos;
            graphicsBufferPos += surface.width * surface.height;
            if (graphicsBufferPos >= SURFACE_DATASIZE)
                graphicsBufferPos = 0;
            else
                GifLoader.ReadGifPictureData(width, height, interlaced, ref graphicsBuffer, surface.dataPosition);
        }
        FileIO.CloseFile();
    }

    public void LoadStageGIFFile(int zNumber)
    {
        //FileData fData = new FileData();
        byte[] byteP = new byte[3];
        bool interlaced = false;
        if (!Scene.LoadStageFile("16x16Tiles.gif", zNumber, out var fData))
            return;
        FileIO.SetFilePosition(6);
        byteP[0] = FileIO.ReadByte();
        int num1 = byteP[0];
        byteP[0] = FileIO.ReadByte();
        int width = num1 + (byteP[0] << 8);
        byteP[0] = FileIO.ReadByte();
        int num2 = byteP[0];
        byteP[0] = FileIO.ReadByte();
        int height = num2 + (byteP[0] << 8);
        byteP[0] = FileIO.ReadByte();
        byteP[0] = FileIO.ReadByte();
        byteP[0] = FileIO.ReadByte();
        for (int index = 128; index < 256; ++index)
            FileIO.ReadFile(byteP, 0, 3);
        for (int index = 128; index < 256; ++index)
        {
            FileIO.ReadFile(byteP, 0, 3);
            Palette.SetPaletteEntry(0xFF, (byte)index, byteP[0], byteP[1], byteP[2]);
        }
        byteP[0] = FileIO.ReadByte();
        if (byteP[0] == 44)
        {
            FileIO.ReadFile(byteP, 0, 2);
            FileIO.ReadFile(byteP, 0, 2);
            FileIO.ReadFile(byteP, 0, 2);
            FileIO.ReadFile(byteP, 0, 2);
            byteP[0] = FileIO.ReadByte();
            if ((byteP[0] & 64) >> 6 == 1)
                interlaced = true;
            if ((byteP[0] & 128) >> 7 == 1)
            {
                for (int index = 128; index < 256; ++index)
                    FileIO.ReadFile(byteP, 0, 3);
            }
            GifLoader.ReadGifPictureData(width, height, interlaced, ref tilesetGFXData, 0);
            byteP[0] = tilesetGFXData[0];
            for (int index = 0; index < Scene.TILESET_SIZE; ++index)
            {
                if (tilesetGFXData[index] == byteP[0])
                    tilesetGFXData[index] = 0;
            }
        }
        FileIO.CloseFile();

        surfaceDirty = true;
    }

    public void DrawStageGFX()
    {
        Scene.waterDrawPos = Scene.waterLevel - Scene.yScrollOffset;

        BeginDraw();

        if (Scene.waterDrawPos < -Scene.TILE_SIZE)
            Scene.waterDrawPos = -Scene.TILE_SIZE;
        if (Scene.waterDrawPos >= SCREEN_YSIZE)
            Scene.waterDrawPos = SCREEN_YSIZE + Scene.TILE_SIZE;

#if RETRO_SOFTWARE_RENDER
        if (waterDrawPos < 0)
            waterDrawPos = 0;

        if (waterDrawPos > SCREEN_YSIZE)
            waterDrawPos = SCREEN_YSIZE;
#endif

        if (Scene.tLayerMidPoint < 3)
        {
            EnsureBlendMode(BlendMode.None);

            DrawObjectList(0);
            if (Scene.activeTileLayers[0] < Scene.LAYER_COUNT)
            {
                switch (Scene.stageLayouts[Scene.activeTileLayers[0]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(0); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(0); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(0); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(0); break;
                    default: break;
                }
            }

            EnsureBlendMode(BlendMode.Alpha);

            DrawObjectList(1);
            if (Scene.activeTileLayers[1] < Scene.LAYER_COUNT)
            {
                switch (Scene.stageLayouts[Scene.activeTileLayers[1]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(1); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(1); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(1); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(1); break;
                    default: break;
                }
            }

            DrawObjectList(2);
            DrawObjectList(3);
            DrawObjectList(4);
            if (Scene.activeTileLayers[2] < Scene.LAYER_COUNT)
            {
                switch (Scene.stageLayouts[Scene.activeTileLayers[2]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(2); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(2); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(2); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(2); break;
                    default: break;
                }
            }
        }
        else if (Scene.tLayerMidPoint < 6)
        {
            EnsureBlendMode(BlendMode.None);
            DrawObjectList(0);
            if (Scene.activeTileLayers[0] < Scene.LAYER_COUNT)
            {
                switch (Scene.stageLayouts[Scene.activeTileLayers[0]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(0); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(0); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(0); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(0); break;
                    default: break;
                }
            }

            EnsureBlendMode(BlendMode.Alpha);

            DrawObjectList(1);
            if (Scene.activeTileLayers[1] < Scene.LAYER_COUNT)
            {
                switch (Scene.stageLayouts[Scene.activeTileLayers[1]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(1); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(1); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(1); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(1); break;
                    default: break;
                }
            }

            DrawObjectList(2);
            if (Scene.activeTileLayers[2] < Scene.LAYER_COUNT)
            {
                switch (Scene.stageLayouts[Scene.activeTileLayers[2]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(2); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(2); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(2); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(2); break;
                    default: break;
                }
            }
            DrawObjectList(3);
            DrawObjectList(4);
        }

        if (Scene.tLayerMidPoint < 6)
        {
            if (Scene.activeTileLayers[3] < Scene.LAYER_COUNT)
            {
                switch (Scene.stageLayouts[Scene.activeTileLayers[3]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(3); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(3); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(3); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(3); break;
                    default: break;
                }
            }

            DrawObjectList(5);
            DrawObjectList(6);
        }

#if !RETRO_USE_ORIGINAL_CODE
        //if (drawStageGFXHQ)
        //    DrawDebugOverlays();
#endif

#if RETRO_SOFTWARE_RENDER
    if (drawStageGFXHQ) {
        CopyFrameOverlay2x();
        if (fadeMode > 0) {
            DrawRectangle(0, 0, SCREEN_XSIZE, SCREEN_YSIZE, fadeR, fadeG, fadeB, fadeA);
            SetFadeHQ(fadeR, fadeG, fadeB, fadeA);
        }
    }
    else {
        if (fadeMode > 0) {
            DrawRectangle(0, 0, SCREEN_XSIZE, SCREEN_YSIZE, fadeR, fadeG, fadeB, fadeA);
        }
    }
#endif
        if (Palette.fadeMode > 0)
        {
            DrawRectangle(0, 0, SCREEN_XSIZE, SCREEN_YSIZE, Palette.fadeR, Palette.fadeG, Palette.fadeB, Palette.fadeA);
        }

#if !RETRO_USE_ORIGINAL_CODE
        //for (int i = 0; i < Input.touches; i++)
        //{
        //    if (Input.touchDown[i] == 0) continue;

        //    Drawing.DrawRectangle(Input.touchX[i], Input.touchY[i], 10, 10, 255, 0, 0, 255);
        //}

        //for (int i = 0; i < rects.Count; i++)
        //{
        //    var item = rects[i];
        //    var col = colors[i % colors.Length];
        //    Drawing.DrawRectangle(item.X, item.Y, item.Width, item.Height, col.R, col.G, col.B, 64);
        //}
#endif

        EndDraw();
    }

    public void DrawObjectList(int layer)
    {
        int size = Scene.drawListEntries[layer].listSize;
        for (int i = 0; i < size; ++i)
        {
            Objects.objectEntityPos = Scene.drawListEntries[layer].entityRefs[i];
            int type = Objects.objectEntityList[Objects.objectEntityPos].type;
            if (type != 0)
            {
                Script.ProcessScript(Script.objectScriptList[type].eventDraw.scriptCodePtr, Script.objectScriptList[type].eventDraw.jumpTablePtr, EVENT.DRAW);
            }
        }
    }

    public abstract void BeginDraw();
    public abstract void EndDraw();
    public abstract void EnsureBlendMode(BlendMode mode);
    public abstract void Draw();
    public abstract void Reset();
    public abstract void SetScreenDimensions(int w, int h);
    public abstract Texture2D CopyRetroBuffer();
    public abstract void Copy16x16Tile(int dest, int src);
    public abstract void DrawHLineScrollLayer(byte layerNum);
    public abstract void ClearScreen(byte clearColour);
    public abstract void DrawSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    public abstract void DrawBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    public abstract void DrawSpriteFlipped(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int direction, int surfaceNum);
    public abstract void DrawAlphaBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum);
    public abstract void DrawAdditiveBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum);
    public abstract void DrawSubtractiveBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum);
    public abstract void DrawRectangle(int xPos, int yPos, int xSize, int ySize, int r, int g, int b, int a);
    public abstract void DrawScaledSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    public abstract void DrawScaledChar(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    public abstract void DrawRotatedSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xBegin, int yBegin, int xSize, int ySize, int rotAngle, int surfaceNum);
    public abstract void DrawRotoZoomSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xBegin, int yBegin, int xSize, int ySize, int rotAngle, int scale, int surfaceNum);
    public abstract void DrawQuad(Quad2D face, int rgbVal);
    public abstract void DrawTexturedQuad(Quad2D face, int surfaceNum);
    public abstract void DrawTexturedBlendedQuad(Quad2D face, int surfaceNum);
    public abstract void DrawFadedQuad(Quad2D face, uint colour, uint fogColour, int alpha);
    public abstract void DrawVLineScrollLayer(int layer);
    public abstract void Draw3DSkyLayer(int layer);
    public abstract void Draw3DFloorLayer(int layer);
    public abstract void DrawTintRectangle(int xPos, int yPos, int xSize, int ySize);
    public abstract void DrawTintSpriteMask(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int tableNo, int surfaceNum);
    public abstract void DrawScaledTintMask(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    
    public void DrawTextMenuEntry(TextMenu tMenu, int rowID, int XPos, int YPos, int textHighlight, int textMenuSurface)
    {
        int id = tMenu.entryStart[rowID];
        for (int i = 0; i < tMenu.entrySize[rowID]; ++i)
        {
            DrawSprite(XPos + (i << 3) - (((tMenu.entrySize[rowID] % 2) & (tMenu.alignment == ALIGN.CENTER ? 1 : 0)) * 4), YPos, 8, 8, (tMenu.textData[id] & 0xF) << 3,
                       ((tMenu.textData[id] >> 4) << 3) + textHighlight, textMenuSurface);
            id++;
        }
    }

    public void DrawStageTextEntry(TextMenu tMenu, int rowID, int XPos, int YPos, int textHighlight, int textMenuSurface)
    {
        int id = tMenu.entryStart[rowID];
        for (int i = 0; i < tMenu.entrySize[rowID]; ++i)
        {
            if (i == tMenu.entrySize[rowID] - 1)
            {
                DrawSprite(XPos + (i << 3), YPos, 8, 8, (tMenu.textData[id] & 0xF) << 3, (tMenu.textData[id] >> 4) << 3, textMenuSurface);
            }
            else
            {
                DrawSprite(XPos + (i << 3), YPos, 8, 8, (tMenu.textData[id] & 0xF) << 3, ((tMenu.textData[id] >> 4) << 3) + textHighlight, textMenuSurface);
            }
            id++;
        }
    }

    public void DrawBlendedTextMenuEntry(TextMenu tMenu, int rowID, int XPos, int YPos, int textHighlight, int textMenuSurface)
    {
        int id = tMenu.entryStart[rowID];
        for (int i = 0; i < tMenu.entrySize[rowID]; ++i)
        {
            DrawBlendedSprite(XPos + (i << 3), YPos, 8, 8, (tMenu.textData[id] & 0xF) << 3, ((tMenu.textData[id] >> 4) << 3) + textHighlight, textMenuSurface);
            id++;
        }
    }

    public void DrawTextMenu(TextMenu tMenu, int XPos, int YPos, int textMenuSurface = TEXTMENU_SURFACE)
    {
        int cnt = 0;
        if (tMenu.visibleRowCount > 0)
        {
            cnt = tMenu.visibleRowCount + tMenu.visibleRowOffset;
        }
        else
        {
            tMenu.visibleRowOffset = 0;
            cnt = tMenu.rowCount;
        }

        if (tMenu.selectionCount == 3)
        {
            tMenu.selection2 = -1;
            for (int i = 0; i < tMenu.selection1 + 1; ++i)
            {
                if (tMenu.entryHighlight[i])
                {
                    tMenu.selection2 = i;
                }
            }
        }

        switch (tMenu.alignment)
        {
            case ALIGN.LEFT:
                for (int i = tMenu.visibleRowOffset; i < cnt; ++i)
                {
                    switch (tMenu.selectionCount)
                    {
                        case 1:
                            if (i == tMenu.selection1)
                                DrawTextMenuEntry(tMenu, i, XPos, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, XPos, YPos, 0, textMenuSurface);
                            break;

                        case 2:
                            if (i == tMenu.selection1 || i == tMenu.selection2)
                                DrawTextMenuEntry(tMenu, i, XPos, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, XPos, YPos, 0, textMenuSurface);
                            break;

                        case 3:
                            if (i == tMenu.selection1)
                                DrawTextMenuEntry(tMenu, i, XPos, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, XPos, YPos, 0, textMenuSurface);

                            if (i == tMenu.selection2 && i != tMenu.selection1)
                                DrawStageTextEntry(tMenu, i, XPos, YPos, 128, textMenuSurface);
                            break;
                    }
                    YPos += 8;
                }
                break;

            case ALIGN.RIGHT:
                for (int i = tMenu.visibleRowOffset; i < cnt; ++i)
                {
                    int entryX = XPos - (tMenu.entrySize[i] << 3);
                    switch (tMenu.selectionCount)
                    {
                        case 1:
                            if (i == tMenu.selection1)
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 0, textMenuSurface);
                            break;

                        case 2:
                            if (i == tMenu.selection1 || i == tMenu.selection2)
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 0, textMenuSurface);
                            break;

                        case 3:
                            if (i == tMenu.selection1)
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 0, textMenuSurface);

                            if (i == tMenu.selection2 && i != tMenu.selection1)
                                DrawStageTextEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            break;
                    }
                    YPos += 8;
                }
                break;

            case ALIGN.CENTER:
                for (int i = tMenu.visibleRowOffset; i < cnt; ++i)
                {
                    int entryX = XPos - (tMenu.entrySize[i] >> 1 << 3);
                    switch (tMenu.selectionCount)
                    {
                        case 1:
                            if (i == tMenu.selection1)
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 0, textMenuSurface);
                            break;
                        case 2:
                            if (i == tMenu.selection1 || i == tMenu.selection2)
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 0, textMenuSurface);
                            break;
                        case 3:
                            if (i == tMenu.selection1)
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            else
                                DrawTextMenuEntry(tMenu, i, entryX, YPos, 0, textMenuSurface);

                            if (i == tMenu.selection2 && i != tMenu.selection1)
                                DrawStageTextEntry(tMenu, i, entryX, YPos, 128, textMenuSurface);
                            break;
                    }
                    YPos += 8;
                }
                break;

            default: break;
        }
    }
    public void DrawObjectAnimation(ObjectScript objectScript, Entity entity, int XPos, int YPos)
    {
        SpriteAnimation sprAnim = Animation.animationList[objectScript.animFile.animListOffset + entity.animation];
        SpriteFrame frame = Animation.animFrames[sprAnim.frameListOffset + entity.frame];

        switch (sprAnim.rotationStyle)
        {
            case ROTSTYLE.NONE:
                switch (entity.direction)
                {
                    case FLIP.NONE:
                        DrawSpriteFlipped(frame.pivotX + XPos, frame.pivotY + YPos, frame.width, frame.height, frame.spriteX, frame.spriteY, FLIP.NONE,
                                          frame.sheetId);
                        break;
                    case FLIP.X:
                        DrawSpriteFlipped(XPos - frame.width - frame.pivotX, frame.pivotY + YPos, frame.width, frame.height, frame.spriteX,
                                          frame.spriteY, FLIP.X, frame.sheetId);
                        break;
                    case FLIP.Y:
                        DrawSpriteFlipped(frame.pivotX + XPos, YPos - frame.height - frame.pivotY, frame.width, frame.height, frame.spriteX,
                                          frame.spriteY, FLIP.Y, frame.sheetId);
                        break;
                    case FLIP.XY:
                        DrawSpriteFlipped(XPos - frame.width - frame.pivotX, YPos - frame.height - frame.pivotY, frame.width, frame.height,
                                          frame.spriteX, frame.spriteY, FLIP.XY, frame.sheetId);
                        break;
                    default: break;
                }
                break;
            case ROTSTYLE.FULL:
                DrawRotatedSprite(entity.direction, XPos, YPos, -frame.pivotX, -frame.pivotY, frame.spriteX, frame.spriteY, frame.width, frame.height,
                                  entity.rotation, frame.sheetId);
                break;
            case ROTSTYLE.FORTYFIVEDEG:
                if (entity.rotation >= 0x100)
                    DrawRotatedSprite(entity.direction, XPos, YPos, -frame.pivotX, -frame.pivotY, frame.spriteX, frame.spriteY, frame.width,
                                      frame.height, 0x200 - ((0x214 - entity.rotation) >> 6 << 6), frame.sheetId);
                else
                    DrawRotatedSprite(entity.direction, XPos, YPos, -frame.pivotX, -frame.pivotY, frame.spriteX, frame.spriteY, frame.width,
                                      frame.height, (entity.rotation + 20) >> 6 << 6, frame.sheetId);
                break;
            case ROTSTYLE.STATICFRAMES:
                {
                    int rotation = 0;
                    if (entity.rotation >= 0x100)
                        rotation = 8 - ((532 - entity.rotation) >> 6);
                    else
                        rotation = (entity.rotation + 20) >> 6;
                    int frameID = entity.frame;
                    switch (rotation)
                    {
                        case 0: // 0 deg
                        case 8: // 360 deg
                            rotation = 0x00;
                            break;
                        case 1: // 45 deg
                            frameID += sprAnim.frameCount;
                            if (entity.direction != 0)
                                rotation = 0;
                            else
                                rotation = 0x80;
                            break;
                        case 2: // 90 deg
                            rotation = 0x80;
                            break;
                        case 3: // 135 deg
                            frameID += sprAnim.frameCount;
                            if (entity.direction != 0)
                                rotation = 0x80;
                            else
                                rotation = 0x100;
                            break;
                        case 4: // 180 deg
                            rotation = 0x100;
                            break;
                        case 5: // 225 deg
                            frameID += sprAnim.frameCount;
                            if (entity.direction != 0)
                                rotation = 0x100;
                            else
                                rotation = 384;
                            break;
                        case 6: // 270 deg
                            rotation = 384;
                            break;
                        case 7: // 315 deg
                            frameID += sprAnim.frameCount;
                            if (entity.direction != 0)
                                rotation = 384;
                            else
                                rotation = 0;
                            break;
                        default: break;
                    }

                    frame = Animation.animFrames[sprAnim.frameListOffset + frameID];
                    DrawRotatedSprite(entity.direction, XPos, YPos, -frame.pivotX, -frame.pivotY, frame.spriteX, frame.spriteY, frame.width, frame.height, rotation, frame.sheetId);
                    break;
                }
            default: break;
        }
    }

    public void DrawBitmapText(TextMenu menu, int XPos, int YPos, int scale, int spacing, int rowStart, int rowCount)
    {
        int Y = YPos << 9;
        if (rowCount < 0)
            rowCount = menu.rowCount;
        if (rowStart + rowCount > menu.rowCount)
            rowCount = menu.rowCount - rowStart;

        while (rowCount > 0)
        {
            int X = XPos << 9;
            for (int i = 0; i < menu.entrySize[rowStart]; ++i)
            {
                ushort c = menu.textData[menu.entryStart[rowStart] + i];
                var fChar = Font.fontCharacterList[c];
                DrawScaledSprite(FLIP.NONE, X >> 9, Y >> 9, -fChar.pivotX, -fChar.pivotY, scale, scale, fChar.width, fChar.height, fChar.srcX,
                                 fChar.srcY, TEXTMENU_SURFACE);

                X += fChar.xAdvance * scale;
            }
            Y += spacing * scale;
            rowStart++;
            rowCount--;
        }
    }
}
