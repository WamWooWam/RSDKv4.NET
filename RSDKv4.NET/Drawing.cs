using Microsoft.Xna.Framework;
using RSDKv4.External;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RSDKv4;

using static RSDKv4.Renderer;
using static RSDKv4.Scene;

public enum BlendMode
{
    None,
    Alpha,
    Additive,
    Subtractive
}

public struct DrawBlendState
{
    public int vertexOffset;
    public int vertexCount;
    public int indexOffset;
    public int indexCount;
    public BlendMode blendMode;
}

public static class Drawing
{
    private static readonly Color MAX_COLOR
        = new Color(255, 255, 255, 255);

    public const int SURFACE_MAX = 24;

    public const int VERTEX_LIMIT = 0x4000;
    public const int INDEX_LIMIT = VERTEX_LIMIT * 6;
    public const int VERTEX3D_LIMIT = 0x1904;
    public const int TEXBUFFER_SIZE = 0x100000;
    public const int TILEUV_SIZE = 0x1000;

    public const int TILE_COUNT = 0x400;
    public const int TILE_SIZE = 0x10;
    public const int CHUNK_SIZE = 0x80;
    public const int TILE_DATASIZE = (TILE_SIZE * TILE_SIZE);
    public const int TILESET_SIZE = (TILE_COUNT * TILE_DATASIZE);

    public const int PALETTE_COUNT = 0x8;
    public const int PALETTE_SIZE = 0x100;

    public const int GFXDATA_MAX = 0x800 * 0x800;

#if FAST_PALETTE
    public static byte[] textureBuffer = new byte[SURFACE_SIZE * SURFACE_SIZE];
#else
    public static ushort[] textureBuffer = new ushort[SURFACE_SIZE * SURFACE_SIZE];
#endif
    public static byte textureBufferMode = 0;

    public static byte[] graphicsBuffer = new byte[SURFACE_DATASIZE];
    public static int graphicsBufferPos = 0;

    public static byte[] tilesetGFXData = new byte[TILESET_SIZE];

    public static float[] tileUVList = new float[TILEUV_SIZE];

    public static int waterDrawPos = 0;

    public static DrawVertex[] vertexList = new DrawVertex[VERTEX_LIMIT];
    public static ushort vertexCount = 0;
    public static short[] indexList = new short[INDEX_LIMIT];
    public static ushort indexCount = 0;

    //public static ushort vertexCountOpaque = 0;
    //public static ushort indexCountOpaque = 0;

    public static DrawBlendState[] drawBlendStates = new DrawBlendState[256];
    public static int drawBlendStateIdx = 0;

    public static SurfaceDesc[] _surfaces = new SurfaceDesc[SURFACE_MAX];

    public static ushort[][] fullPalette = new ushort[PALETTE_COUNT][];
    public static Color[][] fullPalette32 = new Color[PALETTE_COUNT][];

    public const float PIXEL_TO_UV = 1.0f / SURFACE_SIZE;

#if ENABLE_3D
    public static DrawVertex3D[] vertexList3D = new DrawVertex3D[VERTEX3D_LIMIT];
    public static ushort vertexCount3D = 0;
    public static ushort indexCount3D = 0;

    public static bool isRender3DEnabled;
    public static Vector3 floor3DPos;
    public static float floor3DAngle;
#endif

    public static int texPaletteNum = 0;

    static Drawing()
    {
        for (int i = 0; i < SURFACE_MAX; i++)
            _surfaces[i] = new SurfaceDesc();

        for (int i = 0; i < PALETTE_COUNT; i++)
        {
            fullPalette32[i] = new Color[PALETTE_SIZE];
            fullPalette[i] = new ushort[PALETTE_SIZE];
        }
    }

    public static void ResetHardware()
    {
        textureBufferMode = 0;
        for (int i = 0; i < LAYER_COUNT; i++)
        {
            if (stageLayouts[i].type == LAYER.THREEDSKY)
                textureBufferMode = 1;
        }
        for (int i = 0; i < hParallax.entryCount; i++)
        {
            if (hParallax.deform[i] != 0)
                textureBufferMode = 1;
        }

        if (tilesetGFXData[0x32002] > 0)
            textureBufferMode = 0;

        if (textureBufferMode != 0)
        {
            for (int i = 0; i < TILEUV_SIZE; i += 4)
            {
                tileUVList[i + 0] = (i >> 2) % 28 * 18 + 1;
                tileUVList[i + 1] = (i >> 2) / 28 * 18 + 1;
                tileUVList[i + 2] = tileUVList[i + 0] + 16;
                tileUVList[i + 3] = tileUVList[i + 1] + 16;
            }
            tileUVList[TILEUV_SIZE - 4] = 487.0f;
            tileUVList[TILEUV_SIZE - 3] = 487.0f;
            tileUVList[TILEUV_SIZE - 2] = 503.0f;
            tileUVList[TILEUV_SIZE - 1] = 503.0f;
        }
        else
        {
            for (int i = 0; i < TILEUV_SIZE; i += 4)
            {
                tileUVList[i + 0] = (i >> 2 & 31) * 16;
                tileUVList[i + 1] = (i >> 2 >> 5) * 16;
                tileUVList[i + 2] = tileUVList[i + 0] + 16;
                tileUVList[i + 3] = tileUVList[i + 1] + 16;
            }
        }

        for (int i = 0; i < TILEUV_SIZE; i++)
        {
            tileUVList[i] *= PIXEL_TO_UV;
        }

        UpdateSurfaces();

        indexCount = 0;
        vertexCount = 0;
        drawBlendStateIdx = 0;
        drawListEntries[0] = new DrawListEntry();
        //indexCountOpaque = 0;
        //vertexCountOpaque = 0;
    }

    public static void EnsureBlendMode(BlendMode mode)
    {
        if (drawBlendStates[drawBlendStateIdx].blendMode != mode)
        {
            drawBlendStates[drawBlendStateIdx].vertexCount = vertexCount - drawBlendStates[drawBlendStateIdx].vertexOffset;
            drawBlendStates[drawBlendStateIdx].indexCount = indexCount - drawBlendStates[drawBlendStateIdx].indexOffset;
            drawBlendStateIdx++;
            drawBlendStates[drawBlendStateIdx].vertexOffset = vertexCount;
            drawBlendStates[drawBlendStateIdx].indexOffset = indexCount;
            drawBlendStates[drawBlendStateIdx].blendMode = mode;
        }
    }

    public static void FinishDraw()
    {
        drawBlendStates[drawBlendStateIdx].vertexCount = vertexCount - drawBlendStates[drawBlendStateIdx].vertexOffset;
        drawBlendStates[drawBlendStateIdx].indexCount = indexCount - drawBlendStates[drawBlendStateIdx].indexOffset;
        drawBlendStateIdx++;
    }

    public static ushort RGB_16BIT5551(byte r, byte g, byte b, byte a)
    {
        return (ushort)((a << 15) + (r >> 3 << 10) + (g >> 3 << 5) + (b >> 3));
    }

    //public static ushort RGB888_TO_RGB5551(byte r, byte g, byte b)
    //{
    //    return (ushort)((0 << 15) + (r >> 3 << 10) + (g >> 3 << 5) + (b >> 3));
    //}

    public static void UpdateTextureBufferWithTiles()
    {
        var cnt = 0;
        var bufPos = 0;
        var currentPalette = fullPalette[texPaletteNum];
        if (textureBufferMode == 0)
        {
            for (int h = 0; h < 512; h += 16)
            {
                for (int w = 0; w < 512; w += 16)
                {
                    int dataPos = cnt << 8;
                    cnt++;
                    bufPos = w + (h << 10);
                    for (int y = 0; y < TILE_SIZE; y++)
                    {
                        for (int x = 0; x < TILE_SIZE; x++)
                        {
#if FAST_PALETTE
                            textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                            textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                            bufPos++;
                            dataPos++;
                        }
                        bufPos += 1008;
                    }
                }
            }
        }
        else
        {
            for (int h = 0; h < 504; h += 18)
            {
                for (int w = 0; w < 504; w += 18)
                {
                    int dataPos = cnt << 8;
                    cnt++;
                    if (cnt == 783)
                        cnt = 1023;

                    bufPos = w + (h << 10);
#if FAST_PALETTE
                    textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                    textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    bufPos++;

                    for (int l = 0; l < 15; l++)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
                        dataPos++;
                    }

                    if (tilesetGFXData[dataPos] > 0)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    }
                    else
                    {
                        textureBuffer[bufPos] = 0;
                        bufPos++;
                        textureBuffer[bufPos] = 0;
                    }
                    bufPos++;
                    dataPos -= 15;
                    bufPos += 1006;

                    for (int k = 0; k < 16; k++)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
                        for (int l = 0; l < 15; l++)
                        {
#if FAST_PALETTE
                            textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                            textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                            bufPos++;
                            dataPos++;
                        }
                        if (tilesetGFXData[dataPos] > 0)
                        {
#if FAST_PALETTE
                            textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                            textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                            bufPos++;
#if FAST_PALETTE
                            textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                            textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        }
                        else
                        {
                            textureBuffer[bufPos] = 0;
                            bufPos++;
                            textureBuffer[bufPos] = 0;
                        }
                        bufPos++;
                        dataPos++;
                        bufPos += 1006;
                    }
                    dataPos -= 16;
#if FAST_PALETTE
                    textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                    textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    bufPos++;

                    for (int l = 0; l < 15; l++)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
                        dataPos++;
                    }

                    if (tilesetGFXData[dataPos] > 0)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    }
                    else
                    {
                        textureBuffer[bufPos] = 0;
                        bufPos++;
                        textureBuffer[bufPos] = 0;
                    }
                    bufPos++;
                    bufPos += 1006;
                }
            }
        }

        bufPos = 0;
        for (int k = 0; k < TILE_SIZE; k++)
        {
            for (int l = 0; l < TILE_SIZE; l++)
            {
#if FAST_PALETTE
                textureBuffer[bufPos] = 255;
#else
                textureBuffer[bufPos] = RGB_16BIT5551(0xFF, 0xFF, 0xFF, 1);
#endif
                bufPos++;
            }
            bufPos += 1008;
        }
    }

    public static void UpdateTextureBufferWithSprites()
    {
        for (int i = 0; i < SURFACE_MAX; ++i)
        {
            SurfaceDesc surface = _surfaces[i];
            if (surface.texStartY + surface.height <= SURFACE_SIZE && surface.texStartX > -1)
            {
                int pos = surface.dataPosition;
                int teXPos = surface.texStartX + (surface.texStartY << 10);
                for (int j = 0; j < surface.height; j++)
                {
                    for (int k = 0; k < surface.width; k++)
                    {
#if FAST_PALETTE
                        textureBuffer[teXPos] = graphicsBuffer[pos];
#else
                        textureBuffer[teXPos] = fullPalette[texPaletteNum][graphicsBuffer[pos]];
#endif

                        teXPos++;
                        pos++;
                    }
                    teXPos += SURFACE_SIZE - surface.width;
                }
            }
        }
    }

    internal static void UpdateTextureBufferWithSortedSprites()
    {
        byte surfCnt = 0;
        byte[] surfList = new byte[SURFACE_MAX];
        bool flag = true;
        for (int i = 0; i < SURFACE_MAX; i++) _surfaces[i].texStartX = -1;

        for (int i = 0; i < SURFACE_MAX; i++)
        {
            int gfxSize = 0;
            int surfID = -1;
            for (int s = 0; s < SURFACE_MAX; s++)
            {
                var surface = _surfaces[s];
                if (surface != null && surface.texStartX == -1)
                {
                    if (CheckSurfaceSize(surface.width) && CheckSurfaceSize(surface.height))
                    {
                        if (surface.width + surface.height > gfxSize)
                        {
                            gfxSize = surface.width + surface.height;
                            surfID = s;
                        }
                    }
                    else
                    {
                        surface.texStartX = 0;
                    }
                }
            }

            if (surfID == -1)
            {
                i = SURFACE_MAX;
            }
            else
            {
                _surfaces[surfID].texStartX = 0;
                surfList[surfCnt++] = (byte)surfID;
            }
        }

        for (int i = 0; i < SURFACE_MAX; i++)
            _surfaces[i].texStartX = -1;

        for (int i = 0; i < surfCnt; i++)
        {
            var curSurface = _surfaces[surfList[i]];
            curSurface.texStartX = 0;
            curSurface.texStartY = 0;
            bool loopFlag = true;
            while (loopFlag)
            {
                loopFlag = false;
                if (curSurface.height == SURFACE_SIZE)
                    flag = false;

                if (flag)
                {
                    if (curSurface.texStartX < 512 && curSurface.texStartY < 512)
                    {
                        loopFlag = true;
                        curSurface.texStartX += curSurface.width;
                        if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                        {
                            curSurface.texStartX = 0;
                            curSurface.texStartY += curSurface.height;
                        }
                    }
                    else
                    {
                        for (int s = 0; s < SURFACE_MAX; s++)
                        {
                            var surface = _surfaces[s];
                            if (surface.texStartX > -1 && s != surfList[i] && curSurface.texStartX < surface.texStartX + surface.width
                                && curSurface.texStartX >= surface.texStartX && curSurface.texStartY < surface.texStartY + surface.height)
                            {
                                loopFlag = true;
                                curSurface.texStartX += curSurface.width;
                                if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                                {
                                    curSurface.texStartX = 0;
                                    curSurface.texStartY += curSurface.height;
                                }
                                s = SURFACE_MAX;
                            }
                        }
                    }
                }
                else
                {
                    if (curSurface.width < SURFACE_SIZE)
                    {
                        if (curSurface.texStartX < 16 && curSurface.texStartY < 16)
                        {
                            loopFlag = true;
                            curSurface.texStartX += curSurface.width;
                            if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                            {
                                curSurface.texStartX = 0;
                                curSurface.texStartY += curSurface.height;
                            }
                        }
                        else
                        {
                            for (int s = 0; s < SURFACE_MAX; s++)
                            {
                                var surface = _surfaces[s];
                                if (surface.texStartX > -1 && s != surfList[i] && curSurface.texStartX < surface.texStartX + surface.width
                                    && curSurface.texStartX >= surface.texStartX && curSurface.texStartY < surface.texStartY + surface.height)
                                {
                                    loopFlag = true;
                                    curSurface.texStartX += curSurface.width;
                                    if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                                    {
                                        curSurface.texStartX = 0;
                                        curSurface.texStartY += curSurface.height;
                                    }
                                    s = SURFACE_MAX;
                                }
                            }
                        }
                    }
                }
            }

            if (curSurface.texStartY + curSurface.height <= SURFACE_SIZE)
            {
                int gfXPos = curSurface.dataPosition;
                int dataPos = curSurface.texStartX + (curSurface.texStartY << 10);
                for (int h = 0; h < curSurface.height; h++)
                {
                    for (int w = 0; w < curSurface.width; w++)
                    {
#if FAST_PALETTE
                        textureBuffer[dataPos] = graphicsBuffer[gfXPos];
#else
                        textureBuffer[dataPos] = fullPalette[texPaletteNum][graphicsBuffer[gfXPos]];
#endif
                        dataPos++;
                        gfXPos++;
                    }
                    dataPos += SURFACE_SIZE - curSurface.width;
                }
            }
        }
    }

    internal static void ClearGraphicsData()
    {
        for (int index = 0; index < 24; ++index)
            _surfaces[index].fileName = null;

        graphicsBufferPos = 0;
    }

    internal static bool CheckSurfaceSize(int size)
    {
        for (int cnt = 2; cnt < 2048; cnt <<= 1)
        {
            if (cnt == size)
                return true;
        }
        return false;
    }

    public static void SetupPolygonLists()
    {
        int vID = 0;
        for (int i = 0; i < VERTEX_LIMIT; ++i)
        {
            indexList[vID++] = (short)((i << 2) + 0);
            indexList[vID++] = (short)((i << 2) + 1);
            indexList[vID++] = (short)((i << 2) + 2);
            indexList[vID++] = (short)((i << 2) + 1);
            indexList[vID++] = (short)((i << 2) + 3);
            indexList[vID++] = (short)((i << 2) + 2);

            vertexList[i].color = MAX_COLOR;
        }

#if ENABLE_3D
        for (int index2 = 0; index2 < VERTEX3D_LIMIT; ++index2)
            vertexList3D[index2].color = MAX_COLOR;
#endif
    }

    public static int AddGraphicsFile(string filePath)
    {
        //char sheetPath[0x100];
        var sheetPath = "Data/Sprites/" + filePath;
        int sheetId = 0;
        while (_surfaces[sheetId].fileName != null)
        {
            if (_surfaces[sheetId].fileName == sheetPath)
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

    internal static void RemoveGraphicsFile(string filePath, int sheetId)
    {
        if (sheetId < 0)
        {
            for (int i = 0; i < SURFACE_MAX; i++)
                if (_surfaces[i].fileName == filePath)
                    sheetId = i;
        }

        if (sheetId >= 0 && _surfaces[sheetId].fileName != null)
        {
            var surface = _surfaces[sheetId];

            int dataPosStart = surface.dataPosition;
            int dataPosEnd = surface.dataPosition + surface.height * surface.width;

            for (int i = SURFACE_DATASIZE - dataPosEnd; i > 0; --i)
                graphicsBuffer[dataPosStart++] = graphicsBuffer[dataPosEnd++];

            graphicsBufferPos -= surface.height * surface.width;

            for (int i = 0; i < SURFACE_MAX; ++i)
            {
                if (_surfaces[i].dataPosition > surface.dataPosition)
                    _surfaces[i].dataPosition -= surface.height * surface.width;
            }

            _surfaces[sheetId] = new SurfaceDesc();
        }
    }

    public static void LoadBMPFile(string fileName, int surfaceNum)
    {
        if (!FileIO.LoadFile(fileName, out var info))
            return;

        var surface = _surfaces[surfaceNum];
        surface.fileName = fileName;

        FileIO.SetFilePosition(18);

        surface.width = FileIO.ReadByte();
        surface.width += FileIO.ReadByte() << 8;
        surface.width += FileIO.ReadByte() << 16;
        surface.width += FileIO.ReadByte() << 24;

        surface.height = FileIO.ReadByte();
        surface.height += FileIO.ReadByte() << 8;
        surface.height += FileIO.ReadByte() << 16;
        surface.height += FileIO.ReadByte() << 24;

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

    public static void LoadGIFFile(string fileName, int surfaceNum)
    {
        byte[] byteP = new byte[3];
        bool interlaced = false;
        if (!FileIO.LoadFile(fileName, out _))
            return;

        SurfaceDesc surface = _surfaces[surfaceNum];
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

    public static void LoadStageGIFFile(int zNumber)
    {
        //FileData fData = new FileData();
        byte[] byteP = new byte[3];
        bool interlaced = false;
        if (!LoadStageFile("16x16Tiles.gif", zNumber, out var fData))
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
            for (int index = 0; index < TILESET_SIZE; ++index)
            {
                if (tilesetGFXData[index] == byteP[0])
                    tilesetGFXData[index] = 0;
            }
        }
        FileIO.CloseFile();
    }

    public static void DrawStageGFX()
    {
        waterDrawPos = waterLevel - yScrollOffset;
        vertexCount = 0;
        indexCount = 0;

        if (waterDrawPos < -TILE_SIZE)
            waterDrawPos = -TILE_SIZE;
        if (waterDrawPos >= SCREEN_YSIZE)
            waterDrawPos = SCREEN_YSIZE + TILE_SIZE;

#if RETRO_SOFTWARE_RENDER
        if (waterDrawPos < 0)
            waterDrawPos = 0;

        if (waterDrawPos > SCREEN_YSIZE)
            waterDrawPos = SCREEN_YSIZE;
#endif

        if (tLayerMidPoint < 3)
        {
            EnsureBlendMode(BlendMode.None);

            DrawObjectList(0);
            if (activeTileLayers[0] < LAYER_COUNT)
            {
                switch (stageLayouts[activeTileLayers[0]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(0); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(0); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(0); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(0); break;
                    default: break;
                }
            }
            //indexCountOpaque = indexCount;
            //vertexCountOpaque = vertexCount;

            EnsureBlendMode(BlendMode.Alpha);

            DrawObjectList(1);
            if (activeTileLayers[1] < LAYER_COUNT)
            {
                switch (stageLayouts[activeTileLayers[1]].type)
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
            if (activeTileLayers[2] < LAYER_COUNT)
            {
                switch (stageLayouts[activeTileLayers[2]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(2); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(2); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(2); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(2); break;
                    default: break;
                }
            }
        }
        else if (tLayerMidPoint < 6)
        {
            EnsureBlendMode(BlendMode.None);
            DrawObjectList(0);
            if (activeTileLayers[0] < LAYER_COUNT)
            {
                switch (stageLayouts[activeTileLayers[0]].type)
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
            if (activeTileLayers[1] < LAYER_COUNT)
            {
                switch (stageLayouts[activeTileLayers[1]].type)
                {
                    case LAYER.HSCROLL: DrawHLineScrollLayer(1); break;
                    case LAYER.VSCROLL: DrawVLineScrollLayer(1); break;
                    case LAYER.THREEDFLOOR: Draw3DFloorLayer(1); break;
                    case LAYER.THREEDSKY: Draw3DSkyLayer(1); break;
                    default: break;
                }
            }

            DrawObjectList(2);
            if (activeTileLayers[2] < LAYER_COUNT)
            {
                switch (stageLayouts[activeTileLayers[2]].type)
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

        if (tLayerMidPoint < 6)
        {
            if (activeTileLayers[3] < LAYER_COUNT)
            {
                switch (stageLayouts[activeTileLayers[3]].type)
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
        //if (!drawStageGFXHQ)
        //    DrawDebugOverlays();
#endif

        FinishDraw();
    }

    public static void DrawHLineScrollLayer(byte layerNum)
    {
        int num1 = 0;
        int[] gfxDataPos = tiles128x128.gfxDataPos;
        byte[] direction = tiles128x128.direction;
        byte[] visualPlane = tiles128x128.visualPlane;
        int num2 = (int)stageLayouts[activeTileLayers[layerNum]].xsize;
        int num3 = (int)stageLayouts[activeTileLayers[layerNum]].ysize;
        int num4 = (SCREEN_XSIZE >> 4) + 3;
        byte num5 = layerNum < tLayerMidPoint ? (byte)0 : (byte)1;
        ushort[] tileMap;
        byte[] lineScrollRef;
        int num6;
        int num7;
        int[] numArray1;
        int[] numArray2;
        int num8;
        if (activeTileLayers[layerNum] == 0)
        {
            tileMap = stageLayouts[0].tiles;
            lastXSize = num2;
            int yScrollOffset = Scene.yScrollOffset;
            lineScrollRef = stageLayouts[0].lineScroll;
            hParallax.linePos[0] = xScrollOffset;
            num6 = stageLayouts[0].deformationOffset + yScrollOffset & byte.MaxValue;
            num7 = stageLayouts[0].deformationOffsetW + yScrollOffset & byte.MaxValue;
            numArray1 = bgDeformationData0;
            numArray2 = bgDeformationData1;
            num8 = yScrollOffset % (num3 << 7);
        }
        else
        {
            tileMap = stageLayouts[activeTileLayers[layerNum]].tiles;
            int num9 = stageLayouts[activeTileLayers[layerNum]].parallaxFactor * yScrollOffset >> 8;
            int num10 = num3 << 7;
            stageLayouts[activeTileLayers[layerNum]].scrollPos += stageLayouts[activeTileLayers[layerNum]].scrollSpeed;
            if (stageLayouts[activeTileLayers[layerNum]].scrollPos > num10 << 16)
                stageLayouts[activeTileLayers[layerNum]].scrollPos -= num10 << 16;
            num8 = (num9 + (stageLayouts[activeTileLayers[layerNum]].scrollPos >> 16)) % num10;
            num3 = num10 >> 7;
            lineScrollRef = stageLayouts[activeTileLayers[layerNum]].lineScroll;
            num6 = stageLayouts[activeTileLayers[layerNum]].deformationOffset + num8 & byte.MaxValue;
            num7 = stageLayouts[activeTileLayers[layerNum]].deformationOffsetW + num8 & byte.MaxValue;
            numArray1 = bgDeformationData2;
            numArray2 = bgDeformationData3;
        }
        switch (stageLayouts[activeTileLayers[layerNum]].type)
        {
            case 1:
                if (lastXSize != num2)
                {
                    int num9 = num2 << 7;
                    for (int index = 0; index < hParallax.entryCount; ++index)
                    {
                        hParallax.linePos[index] = hParallax.parallaxFactor[index] * xScrollOffset >> 8;
                        hParallax.scrollPos[index] += hParallax.scrollSpeed[index];
                        if (hParallax.scrollPos[index] > num9 << 16)
                            hParallax.scrollPos[index] -= num9 << 16;
                        hParallax.linePos[index] += hParallax.scrollPos[index] >> 16;
                        hParallax.linePos[index] %= num9;
                    }
                    num2 = num9 >> 7;
                }
                lastXSize = num2;
                break;
        }
        if (num8 < 0)
            num8 += num3 << 7;
        int num11 = num8 >> 4 << 4;
        int index1 = num1 + num11;
        int index2 = num6 + (num11 - num8);
        int index3 = num7 + (num11 - num8);
        if (index2 < 0)
            index2 += 256;
        if (index3 < 0)
            index3 += 256;
        int num12 = -(num8 & 15);
        int num13 = num8 >> 7;
        int num14 = (num8 & sbyte.MaxValue) >> 4;
        int num15 = num12 != 0 ? 272 : 256;
        waterDrawPos <<= 4;
        int num16 = num12 << 4;
        for (int index4 = num15; index4 > 0; index4 -= 16)
        {
            int num9 = hParallax.linePos[lineScrollRef[index1]] - 16;
            int index5 = index1 + 8;
            bool flag;
            if (num9 == hParallax.linePos[lineScrollRef[index5]] - 16)
            {
                if (hParallax.deform[(int)lineScrollRef[index5]] == (byte)1)
                {
                    int num10 = num16 < waterDrawPos ? numArray1[index2] : numArray2[index3];
                    int index6 = index2 + 8;
                    int index7 = index3 + 8;
                    int num17 = num16 + 64 <= waterDrawPos ? numArray1[index6] : numArray2[index7];
                    flag = num10 != num17;
                    index2 = index6 - 8;
                    index3 = index7 - 8;
                }
                else
                    flag = false;
            }
            else
                flag = true;
            int index8 = index5 - 8;
            if (flag)
            {
                int num10 = num2 << 7;
                if (num9 < 0)
                    num9 += num10;
                if (num9 >= num10)
                    num9 -= num10;
                int num17 = num9 >> 7;
                int num18 = (num9 & sbyte.MaxValue) >> 4;
                int num19 = -((num9 & 15) << 4) - 256;
                int num20 = num19;
                int index6;
                int index7;
                if (hParallax.deform[lineScrollRef[index8]] == 1)
                {
                    if (num16 >= waterDrawPos)
                        num19 -= numArray2[index3];
                    else
                        num19 -= numArray1[index2];
                    index6 = index2 + 8;
                    index7 = index3 + 8;
                    if (num16 + 64 > waterDrawPos)
                        num20 -= numArray2[index7];
                    else
                        num20 -= numArray1[index6];
                }
                else
                {
                    index6 = index2 + 8;
                    index7 = index3 + 8;
                }
                int index9 = index8 + 8;
                int index10 = (num17 <= -1 || num13 <= -1 ? 0 : tileMap[num17 + (num13 << 8)] << 6) + (num18 + (num14 << 3));
                for (int index11 = num4; index11 > 0; --index11)
                {
                    if (visualPlane[index10] == num5 && gfxDataPos[index10] > 0)
                    {
                        int num21 = 0;
                        switch (direction[index10])
                        {
                            case 0:
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num22 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num22];
                                int num23 = num22 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num23];
                                int num24 = num23 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = num16 + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num24] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 1:
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num25 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num25];
                                int num26 = num25 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num26];
                                int num27 = num26 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = num16 + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num27] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 2:
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = num16 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num28 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num28] + 1f / 128f;
                                int num29 = num28 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = num16 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num29];
                                int num30 = num29 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num30];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 3:
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = num16 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num31 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num31] + 1f / 128f;
                                int num32 = num31 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = num16 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num32];
                                int num33 = num32 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num33];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                        }
                    }
                    num19 += 256;
                    num20 += 256;
                    ++num18;
                    if (num18 > 7)
                    {
                        ++num17;
                        if (num17 == num2)
                            num17 = 0;
                        num18 = 0;
                        index10 = (tileMap[num17 + (num13 << 8)] << 6) + (num18 + (num14 << 3));
                    }
                    else
                        ++index10;
                }
                int num34 = num16 + 128;
                int num35 = hParallax.linePos[lineScrollRef[index9]] - 16;
                int num36 = num2 << 7;
                if (num35 < 0)
                    num35 += num36;
                if (num35 >= num36)
                    num35 -= num36;
                int num37 = num35 >> 7;
                int num38 = (num35 & sbyte.MaxValue) >> 4;
                int num39 = -((num35 & 15) << 4) - 256;
                int num40 = num39;
                if (hParallax.deform[lineScrollRef[index9]] == 1)
                {
                    if (num34 >= waterDrawPos)
                        num39 -= numArray2[index7];
                    else
                        num39 -= numArray1[index6];
                    index2 = index6 + 8;
                    index3 = index7 + 8;
                    if (num34 + 64 > waterDrawPos)
                        num40 -= numArray2[index3];
                    else
                        num40 -= numArray1[index2];
                }
                else
                {
                    index2 = index6 + 8;
                    index3 = index7 + 8;
                }
                index1 = index9 + 8;
                int index12 = (num37 <= -1 || num13 <= -1 ? 0 : tileMap[num37 + (num13 << 8)] << 6) + (num38 + (num14 << 3));
                for (int index11 = num4; index11 > 0; --index11)
                {
                    if (visualPlane[index12] == num5 && gfxDataPos[index12] > 0)
                    {
                        int num21 = 0;
                        switch (direction[index12])
                        {
                            case 0:
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num22 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num22] + 1f / 128f;
                                int num23 = num22 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num23];
                                int num24 = num23 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num24];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 1:
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num25 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num25] + 1f / 128f;
                                int num26 = num25 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num26];
                                int num27 = num26 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num27];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 2:
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num28 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num28];
                                int num29 = num28 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num29];
                                int num30 = num29 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num30] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 3:
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num31 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num31];
                                int num32 = num31 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num32];
                                int num33 = num32 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num33] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                        }
                    }
                    num39 += 256;
                    num40 += 256;
                    ++num38;
                    if (num38 > 7)
                    {
                        ++num37;
                        if (num37 == num2)
                            num37 = 0;
                        num38 = 0;
                        index12 = (tileMap[num37 + (num13 << 8)] << 6) + (num38 + (num14 << 3));
                    }
                    else
                        ++index12;
                }
                num16 = num34 + 128;
            }
            else
            {
                int num10 = num2 << 7;
                if (num9 < 0)
                    num9 += num10;
                if (num9 >= num10)
                    num9 -= num10;
                int num17 = num9 >> 7;
                int num18 = (num9 & sbyte.MaxValue) >> 4;
                int num19 = -((num9 & 15) << 4) - 256;
                int num20 = num19;
                if (hParallax.deform[lineScrollRef[index8]] == 1)
                {
                    if (num16 >= waterDrawPos)
                        num19 -= numArray2[index3];
                    else
                        num19 -= numArray1[index2];
                    index2 += 16;
                    index3 += 16;
                    if (num16 + 128 > waterDrawPos)
                        num20 -= numArray2[index3];
                    else
                        num20 -= numArray1[index2];
                }
                else
                {
                    index2 += 16;
                    index3 += 16;
                }
                index1 = index8 + 16;
                int index6 = (num17 <= -1 || num13 <= -1 ? 0 : tileMap[num17 + (num13 << 8)] << 6) + (num18 + (num14 << 3));
                for (int index7 = num4; index7 > 0; --index7)
                {
                    if (visualPlane[index6] == num5 && gfxDataPos[index6] > 0)
                    {
                        int num21 = 0;
                        switch (direction[index6])
                        {
                            case 0:
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num22 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num22];
                                int num23 = num22 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num23];
                                int num24 = num23 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = num16 + 256;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num24];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 1:
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num25 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num25];
                                int num26 = num25 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num26];
                                int num27 = num26 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = num16 + 256;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num27];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 2:
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = num16 + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num28 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num28];
                                int num29 = num28 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = num16 + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num29];
                                int num30 = num29 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num30];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 3:
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = num16 + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num31 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num31];
                                int num32 = num31 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = num16 + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num32];
                                int num33 = num32 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = num16;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num33];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                        }
                    }
                    num19 += 256;
                    num20 += 256;
                    ++num18;
                    if (num18 > 7)
                    {
                        ++num17;
                        if (num17 == num2)
                            num17 = 0;
                        num18 = 0;
                        index6 = (tileMap[num17 + (num13 << 8)] << 6) + (num18 + (num14 << 3));
                    }
                    else
                        ++index6;
                }
                num16 += 256;
            }
            ++num14;
            if (num14 > 7)
            {
                ++num13;
                if (num13 == num3)
                {
                    num13 = 0;
                    index1 -= num3 << 7;
                }
                num14 = 0;
            }
        }
        waterDrawPos >>= 4;
    }

    public static void DrawHLineScrollLayer1(int layerId)
    {
        TileLayer layer = stageLayouts[activeTileLayers[layerId]];
        if (layer.xsize == 0 || layer.ysize == 0)
            return;
        byte[] lineScrollPtr = null;
        int chunkPosX = 0;
        int chunkTileX = 0;
        int gfxIndex = 0;
        int yscrollOffset = 0;
        int tileGFXPos = 0;
        int deformX1 = 0;
        int deformX2 = 0;
        byte highPlane = (byte)((layerId >= tLayerMidPoint) ? 1 : 0);
        int[] deformationData = null;
        int[] deformationDataW = null;
        int deformOffset = 0;
        int deformOffsetW = 0;
        int lineID = 0;
        int layerWidth = layer.xsize;
        int layerHeight = layer.ysize;
        int renderWidth = (GFX_LINESIZE >> 4) + 3;
        bool flag = false;

        var hParallax = Scene.hParallax;
        var tiles128x128 = Scene.tiles128x128;

        if (activeTileLayers[layerId] != 0)
        {
            layer = stageLayouts[activeTileLayers[layerId]];
            yscrollOffset = layer.parallaxFactor * yScrollOffset >> 8;
            layerHeight = layerHeight << 7;
            layer.scrollPos = layer.scrollPos + layer.scrollSpeed;
            if (layer.scrollPos > layerHeight << 16)
            {
                layer.scrollPos -= (layerHeight << 16);
            }
            yscrollOffset += (layer.scrollPos >> 16);
            yscrollOffset %= layerHeight;

            layerHeight = layerHeight >> 7;
            lineScrollPtr = layer.lineScroll;
            deformOffset = (byte)(layer.deformationOffset + yscrollOffset);
            deformOffsetW = (byte)(layer.deformationOffsetW + yscrollOffset);
            deformationData = bgDeformationData2;
            deformationDataW = bgDeformationData3;
        }
        else
        {
            layer = stageLayouts[0];
            lastXSize = layerWidth;
            yscrollOffset = yScrollOffset;
            lineScrollPtr = layer.lineScroll;
            hParallax.linePos[0] = xScrollOffset;
            deformOffset = (byte)(stageLayouts[0].deformationOffset + yscrollOffset);
            deformOffsetW = (byte)(stageLayouts[0].deformationOffsetW + yscrollOffset);
            deformationData = bgDeformationData0;
            deformationDataW = bgDeformationData1;
            yscrollOffset %= (layerHeight << 7);
        }

        if (layer.type == LAYER.HSCROLL)
        {
            if (lastXSize != layerWidth)
            {
                layerWidth = layerWidth << 7;
                for (int i = 0; i < hParallax.entryCount; i++)
                {
                    hParallax.linePos[i] = hParallax.parallaxFactor[i] * xScrollOffset >> 8;
                    hParallax.scrollPos[i] = hParallax.scrollPos[i] + hParallax.scrollSpeed[i];
                    if (hParallax.scrollPos[i] > layerWidth << 16)
                    {
                        hParallax.scrollPos[i] = hParallax.scrollPos[i] - (layerWidth << 16);
                    }
                    hParallax.linePos[i] = hParallax.linePos[i] + (hParallax.scrollPos[i] >> 16);
                    hParallax.linePos[i] = hParallax.linePos[i] % layerWidth;
                }
                layerWidth = layerWidth >> 7;
            }
            lastXSize = layerWidth;
        }

        if (yscrollOffset < 0)
            yscrollOffset += (layerHeight << 7);

        int deformY = yscrollOffset >> 4 << 4;
        lineID += deformY;
        deformOffset += (deformY - yscrollOffset);
        deformOffsetW += (deformY - yscrollOffset);

        if (deformOffset < 0)
            deformOffset += 0x100;
        if (deformOffsetW < 0)
            deformOffsetW += 0x100;

        deformY = -(yscrollOffset & 15);
        int chunkPosY = yscrollOffset >> 7;
        int chunkTileY = (yscrollOffset & 127) >> 4;
        waterDrawPos <<= 4;
        deformY <<= 4;
        for (int j = (deformY != 0 ? 0x110 : 0x100); j > 0; j -= 16)
        {
            int parallaxLinePos = hParallax.linePos[lineScrollPtr[lineID]] - 16;
            lineID += 8;

            if (parallaxLinePos == hParallax.linePos[lineScrollPtr[lineID]] - 16)
            {
                if (hParallax.deform[lineScrollPtr[lineID]] != 0)
                {
                    deformX1 = deformY < waterDrawPos ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                    deformX2 = (deformY + 64) <= waterDrawPos ? deformationData[deformOffset + 8] : deformationDataW[deformOffsetW + 8];
                    flag = deformX1 != deformX2;
                }
                else
                {
                    flag = false;
                }
            }
            else
            {
                flag = true;
            }

            lineID -= 8;
            if (flag)
            {
                if (parallaxLinePos < 0)
                    parallaxLinePos += layerWidth << 7;
                if (parallaxLinePos >= layerWidth << 7)
                    parallaxLinePos -= layerWidth << 7;

                chunkPosX = parallaxLinePos >> 7;
                chunkTileX = (parallaxLinePos & 0x7F) >> 4;
                deformX1 = -((parallaxLinePos & 0xF) << 4);
                deformX1 -= 0x100;
                deformX2 = deformX1;
                if (hParallax.deform[lineScrollPtr[lineID]] != 0)
                {
                    deformX1 -= deformY < waterDrawPos ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                    deformOffset += 8;
                    deformOffsetW += 8;
                    deformX2 -= (deformY + 64) <= waterDrawPos ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                }
                else
                {
                    deformOffset += 8;
                    deformOffsetW += 8;
                }
                lineID += 8;

                gfxIndex = (chunkPosX > -1 && chunkPosY > -1) ? (layer.tiles[chunkPosX + (chunkPosY << 8)] << 6) : 0;
                gfxIndex += chunkTileX + (chunkTileY << 3);
                for (int i = renderWidth; i > 0; i--)
                {
                    if (tiles128x128.visualPlane[gfxIndex] == highPlane && tiles128x128.gfxDataPos[gfxIndex] > 0)
                    {
                        tileGFXPos = 0;
                        switch (tiles128x128.direction[gfxIndex])
                        {
                            case FLIP.NONE:
                                {
                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = (tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] - 8) * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.X:
                                {
                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] - 8 * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = (tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] + 8) * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = (tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] + 8) * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                        }
                    }

                    deformX1 += (CHUNK_SIZE * 2);
                    deformX2 += (CHUNK_SIZE * 2);
                    if (++chunkTileX < 8)
                    {
                        gfxIndex++;
                    }
                    else
                    {
                        if (++chunkPosX == layerWidth)
                            chunkPosX = 0;

                        chunkTileX = 0;
                        gfxIndex = layer.tiles[chunkPosX + (chunkPosY << 8)] << 6;
                        gfxIndex += chunkTileX + (chunkTileY << 3);
                    }
                }
                deformY += CHUNK_SIZE;
                parallaxLinePos = hParallax.linePos[lineScrollPtr[lineID]] - 16;

                if (parallaxLinePos < 0)
                    parallaxLinePos += layerWidth << 7;
                if (parallaxLinePos >= layerWidth << 7)
                    parallaxLinePos -= layerWidth << 7;

                chunkPosX = parallaxLinePos >> 7;
                chunkTileX = (parallaxLinePos & 127) >> 4;
                deformX1 = -((parallaxLinePos & 15) << 4);
                deformX1 -= 0x100;
                deformX2 = deformX1;
                if (hParallax.deform[lineScrollPtr[lineID]] == 0)
                {
                    deformOffset += 8;
                    deformOffsetW += 8;
                }
                else
                {
                    deformX1 -= deformY < waterDrawPos ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                    deformOffset += 8;
                    deformOffsetW += 8;
                    deformX2 -= (deformY + 64) <= waterDrawPos ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                }

                lineID += 8;
                gfxIndex = (chunkPosX > -1 && chunkPosY > -1) ? (layer.tiles[chunkPosX + (chunkPosY << 8)] << 6) : 0;
                gfxIndex += chunkTileX + (chunkTileY << 3);
                for (int i = renderWidth; i > 0; i--)
                {
                    if (tiles128x128.visualPlane[gfxIndex] == highPlane && tiles128x128.gfxDataPos[gfxIndex] > 0)
                    {
                        tileGFXPos = 0;
                        switch (tiles128x128.direction[gfxIndex])
                        {
                            case FLIP.NONE:
                                {
                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = (tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] + 8) * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.X:
                                {
                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = (tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] + 8) * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = (tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] - 8) * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + CHUNK_SIZE;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = (tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] - 8) * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                        }
                    }

                    deformX1 += (CHUNK_SIZE * 2);
                    deformX2 += (CHUNK_SIZE * 2);

                    if (++chunkTileX < 8)
                    {
                        gfxIndex++;
                    }
                    else
                    {
                        if (++chunkPosX == layerWidth)
                        {
                            chunkPosX = 0;
                        }
                        chunkTileX = 0;
                        gfxIndex = layer.tiles[chunkPosX + (chunkPosY << 8)] << 6;
                        gfxIndex += chunkTileX + (chunkTileY << 3);
                    }
                }
                deformY += CHUNK_SIZE;
            }
            else
            {
                if (parallaxLinePos < 0)
                    parallaxLinePos += layerWidth << 7;
                if (parallaxLinePos >= layerWidth << 7)
                    parallaxLinePos -= layerWidth << 7;

                chunkPosX = parallaxLinePos >> 7;
                chunkTileX = (parallaxLinePos & 0x7F) >> 4;
                deformX1 = -((parallaxLinePos & 0xF) << 4);
                deformX1 -= 0x100;
                deformX2 = deformX1;

                if (hParallax.deform[lineScrollPtr[lineID]] != 0)
                {
                    deformX1 -= deformY < waterDrawPos ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                    deformOffset += 16;
                    deformOffsetW += 16;
                    deformX2 -= (deformY + CHUNK_SIZE <= waterDrawPos) ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                }
                else
                {
                    deformOffset += 16;
                    deformOffsetW += 16;
                }
                lineID += 16;

                gfxIndex = (chunkPosX > -1 && chunkPosY > -1) ? (layer.tiles[chunkPosX + (chunkPosY << 8)] << 6) : 0;
                gfxIndex += chunkTileX + (chunkTileY << 3);
                for (int i = renderWidth; i > 0; i--)
                {
                    if (tiles128x128.visualPlane[gfxIndex] == highPlane && tiles128x128.gfxDataPos[gfxIndex] > 0)
                    {
                        tileGFXPos = 0;
                        switch (tiles128x128.direction[gfxIndex])
                        {
                            case FLIP.NONE:
                                {
                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.X:
                                {
                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    vertexList[vertexCount].position.X = deformX2 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX2;
                                    vertexList[vertexCount].position.Y = deformY + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].texCoord.X = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    tileGFXPos++;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1 + (CHUNK_SIZE * 2);
                                    vertexList[vertexCount].position.Y = deformY;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = tileUVList[tiles128x128.gfxDataPos[gfxIndex] + tileGFXPos] * PIXEL_TO_UV;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    vertexList[vertexCount].position.X = deformX1;
                                    vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                    vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                    vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                    vertexList[vertexCount].color.R = 0xFF;
                                    vertexList[vertexCount].color.G = 0xFF;
                                    vertexList[vertexCount].color.B = 0xFF;
                                    vertexList[vertexCount].color.A = 0xFF;
                                    vertexCount++;

                                    indexCount += 6;
                                    break;
                                }
                        }
                    }

                    deformX1 += (CHUNK_SIZE * 2);
                    deformX2 += (CHUNK_SIZE * 2);
                    if (++chunkTileX < 8)
                    {
                        gfxIndex++;
                    }
                    else
                    {
                        if (++chunkPosX == layerWidth)
                            chunkPosX = 0;

                        chunkTileX = 0;
                        gfxIndex = layer.tiles[chunkPosX + (chunkPosY << 8)] << 6;
                        gfxIndex += chunkTileX + (chunkTileY << 3);
                    }
                }
                deformY += CHUNK_SIZE * 2;
            }

            if (++chunkTileY > 7)
            {
                if (++chunkPosY == layerHeight)
                {
                    chunkPosY = 0;
                    lineID -= (layerHeight << 7);
                }
                chunkTileY = 0;
            }
        }
        waterDrawPos >>= 4;
    }

    public static void DrawVLineScrollLayer(int layer)
    {

    }

    public static void Draw3DSkyLayer(int layer)
    {

    }

    public static void Draw3DFloorLayer(int layer)
    {

    }

    public static void DrawObjectList(int layer)
    {
        int size = drawListEntries[layer].entityRefs.Count;
        for (int i = 0; i < size; ++i)
        {
            Objects.objectEntityPos = drawListEntries[layer].entityRefs[i];
            int type = Objects.objectEntityList[Objects.objectEntityPos].type;
            if (type != 0)
            {
                if (Script.scriptData[Script.objectScriptList[type].eventDraw.scriptCodePtr] > 0)
                {
                    //__drawnObjects.Add(type);
                    Script.ProcessScript(Script.objectScriptList[type].eventDraw.scriptCodePtr, Script.objectScriptList[type].eventDraw.jumpTablePtr, EVENT.DRAW);
                }
            }
        }
    }

    public static void ClearScreen(byte clearColour)
    {
        vertexList[vertexCount].position.X = 0.0f;
        vertexList[vertexCount].position.Y = 0.0f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = SCREEN_XSIZE << 4;
        vertexList[vertexCount].position.Y = 0.0f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = 0.0f;
        vertexList[vertexCount].position.Y = 3840f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = SCREEN_XSIZE << 4;
        vertexList[vertexCount].position.Y = 3840f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawSprite(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawBlendedSprite(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawSpriteFlipped(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int direction,
      int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;
        switch (direction)
        {
            case FLIP.NONE:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
            case FLIP.X:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
            case FLIP.Y:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
            case FLIP.XY:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
        }
        indexCount += 2;
    }

    public static void DrawAlphaBlendedSprite(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int alpha,
      int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;
        if (alpha < 0)
            alpha = 0;

        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawAdditiveBlendedSprite(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int alpha,
      int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Additive);

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;

        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawSubtractiveBlendedSprite(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int alpha,
      int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Subtractive);

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;

        var surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;

        var color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);

        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawRectangle(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int r,
      int g,
      int b,
      int alpha)
    {
        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;
        if (alpha < 0)
            alpha = 0;

        if (vertexCount >= VERTEX_LIMIT)
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = (byte)r;
        vertexList[vertexCount].color.G = (byte)g;
        vertexList[vertexCount].color.B = (byte)b;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawTintRectangle(int xPos, int yPos, int xSize, int ySize)
    {
        Debug.WriteLine("DrawTintRectangle({0},{1},{2},{3})", xPos, yPos, xSize, ySize);
    }

    public static void DrawTintSpriteMask(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int tableNo,
      int surfaceNum)
    {
        Debug.WriteLine("DrawTintSpriteMask({0},{1},{2},{3},{4},{5},{6},{7})", xPos, yPos, xSize, ySize, xBegin, yBegin, tableNo, surfaceNum);
    }

    public static void DrawScaledTintMask(
      byte direction,
      int xPos,
      int yPos,
      int xPivot,
      int yPivot,
      int xScale,
      int yScale,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int surfaceNum)
    {
        Debug.WriteLine("DrawScaledTintMask({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11})", direction, xPos, yPos, xPivot, yPivot, xScale, yScale, xSize, ySize, xBegin, yBegin, surfaceNum);
    }

    public static void DrawScaledSprite(
      byte direction,
      int xPos,
      int yPos,
      int xPivot,
      int yPivot,
      int xScale,
      int yScale,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int surfaceNum)
    {
        if (vertexCount >= VERTEX_LIMIT || xPos <= -512 || (xPos >= 872 || yPos <= -512) || yPos >= 752)
            return;
        xScale <<= 2;
        yScale <<= 2;
        xPos -= xPivot * xScale >> 11;
        xScale = xSize * xScale >> 11;
        yPos -= yPivot * yScale >> 11;
        yScale = ySize * yScale >> 11;
        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1)
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xScale << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + yScale << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawScaledChar(
      byte direction,
      int xPos,
      int yPos,
      int xPivot,
      int yPivot,
      int xScale,
      int yScale,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int surfaceNum)
    {
        if (vertexCount >= VERTEX_LIMIT || xPos <= -8192 || (xPos >= 13951 || yPos <= -1024) || yPos >= 4864)
            return;
        xPos -= xPivot * xScale >> 5;
        xScale = xSize * xScale >> 5;
        yPos -= yPivot * yScale >> 5;
        yScale = ySize * yScale >> 5;
        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= 4096)
            return;
        vertexList[vertexCount].position.X = xPos;
        vertexList[vertexCount].position.Y = yPos;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xScale;
        vertexList[vertexCount].position.Y = yPos;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos;
        vertexList[vertexCount].position.Y = yPos + yScale;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawRotatedSprite(
      byte direction,
      int xPos,
      int yPos,
      int xPivot,
      int yPivot,
      int xBegin,
      int yBegin,
      int xSize,
      int ySize,
      int rotAngle,
      int surfaceNum)
    {
        xPos <<= 4;
        yPos <<= 4;
        rotAngle -= rotAngle >> 9 << 9;
        if (rotAngle < 0)
            rotAngle += 512;
        if (rotAngle != 0)
            rotAngle = 512 - rotAngle;
        int num1 = FastMath.Sin512(rotAngle);
        int num2 = FastMath.Cos512(rotAngle);
        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -8192 || xPos >= 13952) || (yPos <= -8192 || yPos >= 12032))
            return;
        if (direction == 0)
        {
            int num3 = -xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xSize - xPivot;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = -xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xSize - xPivot;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
        else
        {
            int num3 = xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xPivot - xSize;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xPivot - xSize;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
    }

    public static void DrawRotoZoomSprite(
      byte direction,
      int xPos,
      int yPos,
      int xPivot,
      int yPivot,
      int xBegin,
      int yBegin,
      int xSize,
      int ySize,
      int rotAngle,
      int scale,
      int surfaceNum)
    {
        xPos <<= 4;
        yPos <<= 4;
        rotAngle -= rotAngle >> 9 << 9;
        if (rotAngle < 0)
            rotAngle += 512;
        if (rotAngle != 0)
            rotAngle = 512 - rotAngle;
        int num1 = FastMath.Sin512(rotAngle) * scale >> 9;
        int num2 = FastMath.Cos512(rotAngle) * scale >> 9;
        if (_surfaces[surfaceNum].texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -8192 || xPos >= 13952) || (yPos <= -8192 || yPos >= 12032))
            return;
        if (direction == 0)
        {
            int num3 = -xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = (_surfaces[surfaceNum].texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (_surfaces[surfaceNum].texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xSize - xPivot;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = (_surfaces[surfaceNum].texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = -xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (_surfaces[surfaceNum].texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xSize - xPivot;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
        else
        {
            int num3 = xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = (_surfaces[surfaceNum].texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (_surfaces[surfaceNum].texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xPivot - xSize;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = (_surfaces[surfaceNum].texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (_surfaces[surfaceNum].texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xPivot - xSize;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color.R = byte.MaxValue;
            vertexList[vertexCount].color.G = byte.MaxValue;
            vertexList[vertexCount].color.B = byte.MaxValue;
            vertexList[vertexCount].color.A = byte.MaxValue;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
    }

    public static void DrawQuad(Quad2D face, int rgbVal)
    {
        if (vertexCount >= VERTEX_LIMIT)
            return;
        rgbVal = (rgbVal & 2130706432) >> 23;
        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color.R = (byte)(rgbVal >> 16 & byte.MaxValue);
        vertexList[vertexCount].color.G = (byte)(rgbVal >> 8 & byte.MaxValue);
        vertexList[vertexCount].color.B = (byte)(rgbVal & byte.MaxValue);
        vertexList[vertexCount].color.A = rgbVal <= 253 ? (byte)rgbVal : byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawTexturedQuad(Quad2D face, int surfaceNum)
    {

        if (vertexCount >= VERTEX_LIMIT)
            return;
        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[0].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[0].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[1].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[1].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[2].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[2].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[3].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[3].v) * PIXEL_TO_UV;
        ++vertexCount;
        indexCount += 2;
    }

    public static void DrawTexturedBlendedQuad(Quad2D face, int surfaceNum)
    {
        if (vertexCount >= VERTEX_LIMIT)
            return;
        SurfaceDesc surfaceDesc = _surfaces[surfaceNum];
        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[0].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[0].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[1].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[1].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[2].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[2].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[3].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[3].v) * PIXEL_TO_UV;
        ++vertexCount;
        indexCount += 2;
    }


    public static void DrawFadedQuad(Quad2D face, uint colour, uint fogColour, int alpha)
    {
        EnsureBlendMode(BlendMode.Alpha);

        if (vertexCount >= VERTEX_LIMIT)
            return;

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;

        byte cr = (byte)((colour >> 16) & 0xFF);
        byte cg = (byte)((colour >> 8) & 0xFF);
        byte cb = (byte)((colour >> 0) & 0xFF);
        byte fr = (byte)((fogColour >> 16) & 0xFF);
        byte fg = (byte)((fogColour >> 8) & 0xFF);
        byte fb = (byte)((fogColour >> 0) & 0xFF);

        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color.R = (byte)((ushort)(fr * (0xFF - alpha) + alpha * cr) >> 8);
        vertexList[vertexCount].color.G = (byte)((ushort)(fg * (0xFF - alpha) + alpha * cg) >> 8);
        vertexList[vertexCount].color.B = (byte)((ushort)(fb * (0xFF - alpha) + alpha * cb) >> 8);
        vertexList[vertexCount].color.A = 0xFF;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        indexCount += 2;
    }


    public static void DrawTextMenuEntry(object menu, int rowID, int XPos, int YPos, int textHighlight, int textMenuSurface)
    {
        TextMenu tMenu = (TextMenu)menu;
        int id = tMenu.entryStart[rowID];
        for (int i = 0; i < tMenu.entrySize[rowID]; ++i)
        {
            DrawSprite(XPos + (i << 3) - (((tMenu.entrySize[rowID] % 2) & (tMenu.alignment == TextMenuAlignment.CENTER ? 1 : 0)) * 4), YPos, 8, 8, ((tMenu.textData[id] & 0xF) << 3),
                       ((tMenu.textData[id] >> 4) << 3) + textHighlight, textMenuSurface);
            id++;
        }
    }

    public static void DrawStageTextEntry(object menu, int rowID, int XPos, int YPos, int textHighlight, int textMenuSurface)
    {
        TextMenu tMenu = (TextMenu)menu;
        int id = tMenu.entryStart[rowID];
        for (int i = 0; i < tMenu.entrySize[rowID]; ++i)
        {
            if (i == tMenu.entrySize[rowID] - 1)
            {
                DrawSprite(XPos + (i << 3), YPos, 8, 8, ((tMenu.textData[id] & 0xF) << 3), ((tMenu.textData[id] >> 4) << 3), textMenuSurface);
            }
            else
            {
                DrawSprite(XPos + (i << 3), YPos, 8, 8, ((tMenu.textData[id] & 0xF) << 3), ((tMenu.textData[id] >> 4) << 3) + textHighlight,
                           textMenuSurface);
            }
            id++;
        }
    }

    public static void DrawBlendedTextMenuEntry(object menu, int rowID, int XPos, int YPos, int textHighlight, int textMenuSurface)
    {
        TextMenu tMenu = (TextMenu)menu;
        int id = tMenu.entryStart[rowID];
        for (int i = 0; i < tMenu.entrySize[rowID]; ++i)
        {
            DrawBlendedSprite(XPos + (i << 3), YPos, 8, 8, ((tMenu.textData[id] & 0xF) << 3), ((tMenu.textData[id] >> 4) << 3) + textHighlight,
                              textMenuSurface);
            id++;
        }
    }

    public static void DrawTextMenu(object menu, int XPos, int YPos, int textMenuSurface)
    {
        TextMenu tMenu = (TextMenu)menu;
        int cnt = 0;

        if (tMenu.visibleRowCount > 0)
        {
            cnt = (tMenu.visibleRowCount + tMenu.visibleRowOffset);
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
            case TextMenuAlignment.LEFT:
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

            case TextMenuAlignment.RIGHT:
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

            case TextMenuAlignment.CENTER:
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
    public static void DrawObjectAnimation(ObjectScript objectScript, Entity entity, int XPos, int YPos)
    {
        SpriteAnimation sprAnim = Animation.animationList[objectScript.animFile.animListOffset + entity.animation];
        SpriteFrame frame = Animation.animFrames[sprAnim.frameListOffset + entity.frame];
        int rotation = 0;

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
                    DrawRotatedSprite(entity.direction, XPos, YPos, -frame.pivotX, -frame.pivotY, frame.spriteX, frame.spriteY, frame.width, frame.height,
                                      rotation, frame.sheetId);
                    break;
                }
            default: break;
        }
    }

    internal static void SetFade(byte R, byte G, byte B, ushort A)
    {
        Palette.fadeMode = 1;
        Palette.fadeR = R;
        Palette.fadeG = G;
        Palette.fadeB = B;
        Palette.fadeA = (byte)(A > 0xff ? 0xff : A);
    }
}
