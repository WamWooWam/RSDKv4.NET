using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using RSDKv4.Utility;

namespace RSDKv4;

public class Scene
{
    public const int LAYER_COUNT = 9;
    public const int DEFORM_STORE = 0x100;
    public const int DEFORM_SIZE = 320;
    public const int DEFORM_COUNT = DEFORM_STORE + DEFORM_SIZE;
    public const int PARALLAX_COUNT = 0x100;

    public const int TILE_COUNT = 0x400;
    public const int TILE_SIZE = 0x10;
    public const int CHUNK_SIZE = 0x80;
    public const int TILE_DATASIZE = TILE_SIZE * TILE_SIZE;
    public const int TILESET_SIZE = TILE_COUNT * TILE_DATASIZE;

    public const int TILELAYER_CHUNK_W = 0x100;
    public const int TILELAYER_CHUNK_H = 0x100;
    public const int TILELAYER_CHUNK_MAX = TILELAYER_CHUNK_W * TILELAYER_CHUNK_H;
    public const int TILELAYER_SCROLL_MAX = TILELAYER_CHUNK_H * CHUNK_SIZE;

    public const int CHUNKTILE_COUNT = 0x200 * 8 * 8;

    public const int CPATH_COUNT = 2;

    public const int DRAWLAYER_COUNT = 8;

    public static int stageMode = STAGEMODE.LOAD;

    public static int cameraTarget = -1;
    public static int cameraStyle = CAMERASTYLE.FOLLOW;
    public static bool cameraEnabled = false;
    public static int cameraAdjustY = 0;
    public static int xScrollOffset = 0;
    public static int yScrollOffset = 0;
    public static int cameraXPos = 0;
    public static int cameraYPos = 0;
    public static int cameraShift = 0;
    public static int cameraLockedY = 0;
    public static int cameraShakeX = 0;
    public static int cameraShakeY = 0;
    public static int cameraLag = 0;
    public static int cameraLagStyle = 0;

    public static int curXBoundary1;
    public static int newXBoundary1;
    public static int curYBoundary1;
    public static int newYBoundary1;
    public static int curXBoundary2;
    public static int curYBoundary2;
    public static int waterLevel;
    public static int waterDrawPos;
    public static int newXBoundary2;
    public static int newYBoundary2;

    public static int SCREEN_SCROLL_LEFT;
    public static int SCREEN_SCROLL_RIGHT;
    public static int SCREEN_SCROLL_UP => (Drawing.SCREEN_YSIZE / 2) - 16;
    public static int SCREEN_SCROLL_DOWN => (Drawing.SCREEN_YSIZE / 2) + 16;

    public static int lastXSize;
    public static int lastYSize;

    public static bool pauseEnabled;
    public static bool timeEnabled;
    public static bool debugMode;
    public static int frameCounter;
    public static int stageMilliseconds;
    public static int stageSeconds;
    public static int stageMinutes;

    // Category and Scene IDs
    public static int activeStageList;
    public static int stageListPosition;
    public static string currentStageFolder;
    public static int actId;

    public static string titleCardText;
    public static byte titleCardWord2;

    public static byte[] activeTileLayers = new byte[4];
    public static byte tLayerMidPoint;
    public static TileLayer[] stageLayouts = new TileLayer[LAYER_COUNT];

    public static int[] bgDeformationData0 = new int[DEFORM_COUNT];
    public static int[] bgDeformationData1 = new int[DEFORM_COUNT];
    public static int[] bgDeformationData2 = new int[DEFORM_COUNT];
    public static int[] bgDeformationData3 = new int[DEFORM_COUNT];

    public static LineScroll hParallax = new LineScroll();
    public static LineScroll vParallax = new LineScroll();

    public static Tiles128x128 tiles128x128 = new Tiles128x128();
    public static CollisionMasks[] collisionMasks = new CollisionMasks[2];
    public static DrawListEntry[] drawListEntries = new DrawListEntry[DRAWLAYER_COUNT];

    //public static ushort tile3DFloorBuffer[0x100 * 0x100];
    //public static bool drawStageGFXHQ;

    static Scene()
    {
        Helpers.Memset(stageLayouts, () => new TileLayer());
        Helpers.Memset(collisionMasks, () => new CollisionMasks());
        Helpers.Memset(drawListEntries, () => new DrawListEntry());
    }

    public static void InitFirstStage()
    {
        xScrollOffset = 0;
        yScrollOffset = 0;
        Audio.StopAllMusic();
        Audio.StopAllSfx();
        Audio.ReleaseStageSfx();
        Palette.fadeMode = 0;
        Drawing.ClearGraphicsData();
        Animation.ClearAnimationData();

        Palette.SetActivePalette(0, 0, 0);
        stageMode = STAGEMODE.LOAD;
        Engine.engineState = ENGINE_STATE.WAIT;
        activeStageList = 0;
        stageListPosition = 0;
    }

    public static void InitStartingStage(int list, int stage, int player)
    {
        xScrollOffset = 0;
        yScrollOffset = 0;
        Audio.StopAllMusic();
        Audio.StopAllSfx();
        Audio.ReleaseStageSfx();
        Palette.fadeMode = 0;
        Drawing.ClearGraphicsData();
        Animation.ClearAnimationData();
        currentStageFolder = "";
        activeStageList = list;
        stageMode = STAGEMODE.LOAD;
        stageListPosition = stage;

        Objects.playerListPos = player;
        Palette.SetActivePalette(0, 0, 0);
        Engine.engineState = ENGINE_STATE.MAINGAME;
    }

    public static void ProcessStage()
    {
        Scene3D.vertexCount = 0;
        Scene3D.faceCount = 0;

        Palette.activePalettes[0] = new PaletteEntry(0, 0, Drawing.SCREEN_YSIZE);
        Palette.activePaletteCount = 0;

        switch (stageMode)
        {
            case STAGEMODE.LOAD:
                {
                    Engine.hooks.OnStageWillLoad();

                    Palette.SetActivePalette(0, 0, Drawing.SCREEN_YSIZE);
                    Text.gameMenu[0].visibleRowOffset = 0;
                    Text.gameMenu[1].alignment = 0;
                    Text.gameMenu[1].selectionCount = 0;

                    Palette.fadeMode = 0;

                    cameraEnabled = true;
                    cameraTarget = -1;
                    cameraShift = 0;
                    cameraStyle = CAMERASTYLE.FOLLOW;
                    cameraXPos = 0;
                    cameraYPos = 0;
                    cameraLockedY = 0;
                    cameraAdjustY = 0;
                    xScrollOffset = 0;
                    yScrollOffset = 0;
                    cameraShakeX = 0;
                    cameraShakeY = 0;
                    frameCounter = 0;
                    pauseEnabled = false;
                    timeEnabled = false;
                    stageMilliseconds = 0;
                    stageSeconds = 0;
                    stageMinutes = 0;
                    stageMode = STAGEMODE.NORMAL;

                    ResetBackgroundSettings();
                    LoadStageFiles();

                    Engine.hooks.OnStageDidLoad();

                    Drawing.Reset();
                    break;
                }
            case STAGEMODE.NORMAL:
                {
                    Engine.hooks.OnStageWillStep();

                    //drawStageGFXHQ = false;
                    if (Palette.fadeMode > 0)
                        Palette.fadeMode--;

                    lastXSize = -1;
                    lastYSize = -1;
                    Input.CheckKeyDown(ref Input.keyDown);
                    Input.CheckKeyPress(ref Input.keyPress);
                    if (pauseEnabled && Input.keyPress.start)
                    {
                        stageMode = STAGEMODE.NORMAL_STEP;
                        Audio.PauseSound();
                    }

                    if (timeEnabled)
                    {
                        if (++frameCounter == 60)
                        {
                            frameCounter = 0;
                            if (++stageSeconds > 59)
                            {
                                stageSeconds = 0;
                                if (++stageMinutes > 59)
                                    stageMinutes = 0;
                            }
                        }
                        stageMilliseconds = 100 * frameCounter / 60;
                    }
                    else
                    {
                        frameCounter = 60 * stageMilliseconds / 100;
                    }

                    // Update
                    Objects.ProcessObjects();

                    if (cameraTarget > -1)
                    {
                        if (cameraEnabled)
                        {
                            switch (cameraStyle)
                            {
                                case CAMERASTYLE.FOLLOW:
                                    SetPlayerScreenPosition(Objects.objectEntityList[cameraTarget]); break;
                                case CAMERASTYLE.EXTENDED:
                                case CAMERASTYLE.EXTENDED_OFFSET_L:
                                case CAMERASTYLE.EXTENDED_OFFSET_R:
                                //SetPlayerScreenPositionCDStyle(Objects.objectEntityList[cameraTarget]); break;
                                case CAMERASTYLE.HLOCKED:
                                //SetPlayerHLockedScreenPosition(Objects.objectEntityList[cameraTarget]); break;
                                default: break;
                            }
                        }
                        else
                        {
                            SetPlayerLockedScreenPosition(Objects.objectEntityList[cameraTarget]);
                        }
                    }

                    ProcessParallaxAutoScroll();
                    Drawing.DrawStageGFX();

                    Engine.hooks.OnStageDidStep();
                    break;
                }
            case STAGEMODE.PAUSED:
                {
                    if (Palette.fadeMode > 0)
                        Palette.fadeMode--;

                    lastXSize = -1;
                    lastYSize = -1;
                    Input.CheckKeyDown(ref Input.keyDown);
                    Input.CheckKeyPress(ref Input.keyPress);

                    if (pauseEnabled && Input.keyPress.start)
                    {
                        stageMode = STAGEMODE.PAUSED_STEP;
                        Audio.PauseSound();
                    }

                    // Update
                    Objects.ProcessPausedObjects();

                    ProcessParallaxAutoScroll();
                    Drawing.DrawStageGFX();

                    break;
                }
            case STAGEMODE.FROZEN:
                {
                    //drawStageGFXHQ = false;
                    if (Palette.fadeMode > 0)
                        Palette.fadeMode--;

                    lastXSize = -1;
                    lastYSize = -1;

                    Input.CheckKeyDown(ref Input.keyDown);
                    Input.CheckKeyPress(ref Input.keyPress);

                    // Update
                    Objects.ProcessFrozenObjects();

                    if (cameraTarget > -1)
                    {
                        if (cameraEnabled)
                        {
                            switch (cameraStyle)
                            {
                                case CAMERASTYLE.FOLLOW: SetPlayerScreenPosition(Objects.objectEntityList[cameraTarget]); break;
                                case CAMERASTYLE.EXTENDED:
                                case CAMERASTYLE.EXTENDED_OFFSET_L:
                                //case CAMERASTYLE.EXTENDED_OFFSET_R: SetPlayerScreenPositionCDStyle(objectEntityList[cameraTarget]); break;
                                //case CAMERASTYLE.HLOCKED: SetPlayerHLockedScreenPosition(objectEntityList[cameraTarget]); break;
                                default: break;
                            }
                        }
                        else
                        {
                            SetPlayerLockedScreenPosition(Objects.objectEntityList[cameraTarget]);
                        }
                    }

                    Drawing.DrawStageGFX();
                    break;
                }
            default:
                Debug.WriteLine("Invalid stage mode!!");
                break;
        }
    }


    public static void ProcessParallaxAutoScroll()
    {
        for (int i = 0; i < hParallax.entryCount; ++i) hParallax.scrollPos[i] += hParallax.scrollSpeed[i];
        for (int i = 0; i < vParallax.entryCount; ++i) vParallax.scrollPos[i] += vParallax.scrollSpeed[i];
    }

    public static void LoadStageFiles()
    {
        Audio.StopAllSfx(); // ?

        int scriptId = 1;
        FileInfo infoStore;
        if (!CheckCurrentStageFolder(stageListPosition))
        {
            Debug.WriteLine("Loading scene: {0} {1}", (object)Engine.stageListNames[activeStageList], Engine.stageList[activeStageList][stageListPosition].name);
            Audio.ReleaseStageSfx(); // again?
            Script.ClearScriptData();
            Drawing.ClearGraphicsData();

            var loadGlobalScripts = false;
            if (LoadStageFile("StageConfig.bin", stageListPosition, out var info))
            {
                loadGlobalScripts = FileIO.ReadByte() != 0;
                FileIO.CloseFile();
            }

            if (loadGlobalScripts && FileIO.LoadFile("Data/Game/GameConfig.bin", out info))
            {
                FileIO.ReadLengthPrefixedString();
                FileIO.ReadLengthPrefixedString();

                byte[] buffer = new byte[3];
                for (int c = 0; c < 0x60; ++c)
                {
                    FileIO.ReadFile(buffer, 0, 3);
                    Palette.SetPaletteEntry(0xff, (byte)c, buffer[0], buffer[1], buffer[2]);
                }

                byte globalObjectCount = FileIO.ReadByte();
                for (byte i = 0; i < globalObjectCount; ++i)
                {
                    Objects.SetObjectTypeName(FileIO.ReadLengthPrefixedString(), scriptId + i);
                }

                FileIO.GetFileInfo(out infoStore);
                FileIO.CloseFile();
                Script.LoadBytecode(4, scriptId);
                scriptId += globalObjectCount;
                FileIO.SetFileInfo(infoStore);
                FileIO.CloseFile();
            }

            if (LoadStageFile("StageConfig.bin", stageListPosition, out info))
            {
                FileIO.ReadByte();

                byte[] buffer = new byte[3];
                for (int c = 0x60; c < 0x80; ++c)
                {
                    FileIO.ReadFile(buffer, 0, 3);
                    Palette.SetPaletteEntry(0xff, (byte)c, buffer[0], buffer[1], buffer[2]);
                }

                Audio.stageSfxCount = FileIO.ReadByte();
                for (int i = 0; i < Audio.stageSfxCount; i++)
                {
                    Audio.SetSfxName(FileIO.ReadLengthPrefixedString(), i + Engine.globalSfxCount);
                }

                for (int i = 0; i < Audio.stageSfxCount; i++)
                {
                    var sfxPath = FileIO.ReadLengthPrefixedString();
                    FileIO.GetFileInfo(out infoStore);
                    FileIO.CloseFile();
                    Audio.LoadSfx(sfxPath, (byte)(i + Engine.globalSfxCount));
                    FileIO.SetFileInfo(infoStore);
                }

                var stageObjectCount = FileIO.ReadByte();
                for (int i = 0; i < stageObjectCount; i++)
                    Objects.SetObjectTypeName(FileIO.ReadLengthPrefixedString(), scriptId + i);


                for (int i = 0; i < stageObjectCount; i++)
                    FileIO.ReadLengthPrefixedString();

                FileIO.GetFileInfo(out infoStore);
                FileIO.CloseFile();
                Script.LoadBytecode(activeStageList, scriptId);
                FileIO.SetFileInfo(infoStore);

                FileIO.CloseFile();
            }

            Drawing.LoadStageGIFFile(stageListPosition);
            LoadStageCollisions();
            LoadStageBackground();
        }

        LoadStageChunks();

        for (int i = 0; i < Audio.TRACK_COUNT; ++i)
            Audio.SetMusicTrack("", (byte)i, false, 0);

        Helpers.Memset(
            Objects.objectEntityList,
            () => new Entity() { drawOrder = 3, scale = 512, objectInteractions = true, visible = true, tileCollisions = true });

        LoadActLayout();
        Objects.ProcessStartupObjects();
    }

    public static void LoadStageBackground()
    {
        for (int i = 0; i < LAYER_COUNT; ++i)
        {
            stageLayouts[i].type = LAYER.NOSCROLL;
            stageLayouts[i].deformationOffset = 0;
            stageLayouts[i].deformationOffsetW = 0;
        }
        for (int i = 0; i < PARALLAX_COUNT; ++i)
        {
            hParallax.scrollPos[i] = 0;
            vParallax.scrollPos[i] = 0;
        }

        FileInfo info;
        if (LoadStageFile("Backgrounds.bin", stageListPosition, out info))
        {
            byte layerCount = FileIO.ReadByte();
            hParallax.entryCount = FileIO.ReadByte();
            for (byte i = 0; i < hParallax.entryCount; ++i)
            {
                hParallax.parallaxFactor[i] = FileIO.ReadUInt16();
                hParallax.scrollSpeed[i] = FileIO.ReadByte() << 10;
                hParallax.scrollPos[i] = 0;
                hParallax.deform[i] = FileIO.ReadByte();
            }

            vParallax.entryCount = FileIO.ReadByte();
            for (byte i = 0; i < vParallax.entryCount; ++i)
            {
                vParallax.parallaxFactor[i] = FileIO.ReadUInt16();
                vParallax.scrollSpeed[i] = FileIO.ReadByte() << 10;
                vParallax.scrollPos[i] = 0;
                vParallax.deform[i] = FileIO.ReadByte();
            }

            for (byte i = 1; i < layerCount + 1; ++i)
            {
                stageLayouts[i].xsize = FileIO.ReadByte();
                FileIO.ReadByte(); // Unused (???)
                stageLayouts[i].ysize = FileIO.ReadByte();
                FileIO.ReadByte(); // Unused (???)
                stageLayouts[i].type = FileIO.ReadByte();
                stageLayouts[i].parallaxFactor = FileIO.ReadUInt16();
                stageLayouts[i].scrollSpeed = FileIO.ReadByte() << 10;
                stageLayouts[i].scrollPos = 0;

                Helpers.Memset<ushort>(stageLayouts[i].tiles, 0);
                Helpers.Memset<byte>(stageLayouts[i].lineScroll, 0);

                // Read Line Scroll
                byte[] buf = new byte[3];
                int pos = 0;
                while (true)
                {
                    FileIO.ReadFile(buf, 0, 1);
                    if (buf[0] == 0xFF)
                    {
                        FileIO.ReadFile(buf, 1, 1);
                        if (buf[1] == 0xFF)
                        {
                            break;
                        }
                        else
                        {
                            FileIO.ReadFile(buf, 2, 1);
                            int val = buf[1];
                            int cnt = buf[2] - 1;
                            for (int c = 0; c < cnt; ++c)
                            {
                                stageLayouts[i].lineScroll[pos] = (byte)val;
                                ++pos;
                            }
                        }
                    }
                    else
                    {
                        stageLayouts[i].lineScroll[pos] = buf[0];
                        ++pos;
                    }
                }

                // Read Layout
                for (int y = 0; y < stageLayouts[i].ysize; ++y)
                {
                    //ushort* chunks = &stageLayouts[i].tiles[];

                    var tileOffset = y * TILELAYER_CHUNK_H;
                    for (int x = 0; x < stageLayouts[i].xsize; ++x)
                    {
                        stageLayouts[i].tiles[tileOffset + x] = FileIO.ReadUInt16();
                    }
                }
            }

            FileIO.CloseFile();
        }
    }

    public static void LoadStageCollisions()
    {
        if (LoadStageFile("CollisionMasks.bin", stageListPosition, out var info))
        {
            int tileIndex = 0;
            for (int t = 0; t < TILE_COUNT; ++t)
            {
                for (int p = 0; p < CPATH_COUNT; ++p)
                {
                    byte fileBuffer = FileIO.ReadByte();
                    bool isCeiling = (fileBuffer >> 4) != 0;
                    collisionMasks[p].flags[t] = (byte)(fileBuffer & 0xF);
                    collisionMasks[p].angles[t] = FileIO.ReadUInt32();

                    if (isCeiling) // Ceiling Tile
                    {
                        for (int c = 0; c < TILE_SIZE; c += 2)
                        {
                            fileBuffer = FileIO.ReadByte();
                            collisionMasks[p].roofMasks[c + tileIndex] = (sbyte)(fileBuffer >> 4);
                            collisionMasks[p].roofMasks[c + tileIndex + 1] = (sbyte)(fileBuffer & 0xF);
                        }

                        // Has Collision (Pt 1)
                        fileBuffer = FileIO.ReadByte();
                        int id = 1;
                        for (int c = 0; c < TILE_SIZE / 2; ++c)
                        {
                            if ((fileBuffer & id) != 0)
                            {
                                collisionMasks[p].floorMasks[c + tileIndex + 8] = 0;
                            }
                            else
                            {
                                collisionMasks[p].floorMasks[c + tileIndex + 8] = 0x40;
                                collisionMasks[p].roofMasks[c + tileIndex + 8] = -0x40;
                            }
                            id <<= 1;
                        }

                        // Has Collision (Pt 2)
                        fileBuffer = FileIO.ReadByte();
                        id = 1;
                        for (int c = 0; c < TILE_SIZE / 2; ++c)
                        {
                            if ((fileBuffer & id) != 0)
                            {
                                collisionMasks[p].floorMasks[c + tileIndex] = 0;
                            }
                            else
                            {
                                collisionMasks[p].floorMasks[c + tileIndex] = 0x40;
                                collisionMasks[p].roofMasks[c + tileIndex] = -0x40;
                            }
                            id <<= 1;
                        }

                        // LWall rotations
                        for (int c = 0; c < TILE_SIZE; ++c)
                        {
                            int h = 0;
                            while (h > -1)
                            {
                                if (h == TILE_SIZE)
                                {
                                    collisionMasks[p].lWallMasks[c + tileIndex] = 0x40;
                                    h = -1;
                                }
                                else if (c > collisionMasks[p].roofMasks[h + tileIndex])
                                {
                                    ++h;
                                }
                                else
                                {
                                    collisionMasks[p].lWallMasks[c + tileIndex] = (sbyte)h;
                                    h = -1;
                                }
                            }
                        }

                        // RWall rotations
                        for (int c = 0; c < TILE_SIZE; ++c)
                        {
                            int h = TILE_SIZE - 1;
                            while (h < TILE_SIZE)
                            {
                                if (h == -1)
                                {
                                    collisionMasks[p].rWallMasks[c + tileIndex] = -0x40;
                                    h = TILE_SIZE;
                                }
                                else if (c > collisionMasks[p].roofMasks[h + tileIndex])
                                {
                                    --h;
                                }
                                else
                                {
                                    collisionMasks[p].rWallMasks[c + tileIndex] = (sbyte)h;
                                    h = TILE_SIZE;
                                }
                            }
                        }
                    }
                    else // Regular Tile
                    {
                        for (int c = 0; c < TILE_SIZE; c += 2)
                        {
                            fileBuffer = FileIO.ReadByte();
                            collisionMasks[p].floorMasks[c + tileIndex] = (sbyte)(fileBuffer >> 4);
                            collisionMasks[p].floorMasks[c + tileIndex + 1] = (sbyte)(fileBuffer & 0xF);
                        }
                        fileBuffer = FileIO.ReadByte();
                        int id = 1;
                        for (int c = 0; c < TILE_SIZE / 2; ++c) // HasCollision
                        {
                            if ((fileBuffer & id) != 0)
                            {
                                collisionMasks[p].roofMasks[c + tileIndex + 8] = 0xF;
                            }
                            else
                            {
                                collisionMasks[p].floorMasks[c + tileIndex + 8] = 0x40;
                                collisionMasks[p].roofMasks[c + tileIndex + 8] = -0x40;
                            }
                            id <<= 1;
                        }

                        fileBuffer = FileIO.ReadByte();
                        id = 1;
                        for (int c = 0; c < TILE_SIZE / 2; ++c) // HasCollision (pt 2)
                        {
                            if ((fileBuffer & id) != 0)
                            {
                                collisionMasks[p].roofMasks[c + tileIndex] = 0xF;
                            }
                            else
                            {
                                collisionMasks[p].floorMasks[c + tileIndex] = 0x40;
                                collisionMasks[p].roofMasks[c + tileIndex] = -0x40;
                            }
                            id <<= 1;
                        }

                        // LWall rotations
                        for (int c = 0; c < TILE_SIZE; ++c)
                        {
                            int h = 0;
                            while (h > -1)
                            {
                                if (h == TILE_SIZE)
                                {
                                    collisionMasks[p].lWallMasks[c + tileIndex] = 0x40;
                                    h = -1;
                                }
                                else if (c < collisionMasks[p].floorMasks[h + tileIndex])
                                {
                                    ++h;
                                }
                                else
                                {
                                    collisionMasks[p].lWallMasks[c + tileIndex] = (sbyte)h;
                                    h = -1;
                                }
                            }
                        }

                        // RWall rotations
                        for (int c = 0; c < TILE_SIZE; ++c)
                        {
                            int h = TILE_SIZE - 1;
                            while (h < TILE_SIZE)
                            {
                                if (h == -1)
                                {
                                    collisionMasks[p].rWallMasks[c + tileIndex] = -0x40;
                                    h = TILE_SIZE;
                                }
                                else if (c < collisionMasks[p].floorMasks[h + tileIndex])
                                {
                                    --h;
                                }
                                else
                                {
                                    collisionMasks[p].rWallMasks[c + tileIndex] = (sbyte)h;
                                    h = TILE_SIZE;
                                }
                            }
                        }
                    }
                }
                tileIndex += 16;
            }
            FileIO.CloseFile();
        }
    }

    public static void LoadStageChunks()
    {
        FileInfo info;
        byte[] entry = new byte[3];

        if (LoadStageFile("128x128Tiles.bin", stageListPosition, out info))
        {
            for (int i = 0; i < CHUNKTILE_COUNT; ++i)
            {
                FileIO.ReadFile(entry, 0, 3);
                entry[0] -= (byte)((entry[0] >> 6) << 6);

                tiles128x128.visualPlane[i] = (byte)(entry[0] >> 4);
                entry[0] = (byte)(entry[0] - 16 * (entry[0] >> 4));

                tiles128x128.direction[i] = (byte)(entry[0] >> 2);
                entry[0] = (byte)(entry[0] - 4 * (entry[0] >> 2));

                tiles128x128.tileIndex[i] = (ushort)(entry[1] + (entry[0] << 8));
#if RETRO_SOFTWARE_RENDER
                tiles128x128.gfxDataPos[i] = tiles128x128.tileIndex[i] << 8;
#endif
                tiles128x128.gfxDataPos[i] = tiles128x128.tileIndex[i] << 2;

                tiles128x128.collisionFlags[0][i] = (byte)(entry[2] >> 4);
                tiles128x128.collisionFlags[1][i] = (byte)(entry[2] - ((entry[2] >> 4) << 4));
            }
            FileIO.CloseFile();
        }
    }

    public static void LoadActLayout()
    {
        FileInfo info;

        for (int a = 0; a < 4; ++a) activeTileLayers[a] = 9; // disables missing scenes from rendering

        if (LoadActFile(".bin", stageListPosition, out info))
        {
            byte[] fileBuffer = new byte[4];

            byte length = FileIO.ReadByte();
            titleCardWord2 = (byte)length;

            char[] titleCardBuffer = new char[0x80];
            for (int i = 0; i < length; ++i)
            {
                titleCardBuffer[i] = (char)FileIO.ReadByte();
                if (titleCardBuffer[i] == '-')
                    titleCardWord2 = (byte)(i + 1);
            }
            titleCardBuffer[length] = '\0';

            titleCardText = new string(titleCardBuffer, 0, length);

            // READ TILELAYER
            FileIO.ReadFile(activeTileLayers, 0, 4);
            tLayerMidPoint = FileIO.ReadByte();

            stageLayouts[0].xsize = FileIO.ReadByte();
            FileIO.ReadByte();

            stageLayouts[0].ysize = FileIO.ReadByte();
            FileIO.ReadByte();

            curXBoundary1 = 0;
            newXBoundary1 = 0;
            curYBoundary1 = 0;
            newYBoundary1 = 0;
            curXBoundary2 = stageLayouts[0].xsize << 7;
            curYBoundary2 = stageLayouts[0].ysize << 7;
            waterLevel = curYBoundary2 + 128;
            newXBoundary2 = stageLayouts[0].xsize << 7;
            newYBoundary2 = stageLayouts[0].ysize << 7;

            Helpers.Memset<ushort>(stageLayouts[0].tiles, 0);
            Helpers.Memset<byte>(stageLayouts[0].lineScroll, 0);

            for (int y = 0; y < stageLayouts[0].ysize; ++y)
            {
                var tileOffset = y * TILELAYER_CHUNK_H;
                for (int x = 0; x < stageLayouts[0].xsize; ++x)
                {
                    stageLayouts[0].tiles[tileOffset + x] = FileIO.ReadUInt16();
                }
            }

            // READ OBJECTS
            int objectCount = FileIO.ReadUInt16();
            if (objectCount > 0x400)
                Debug.WriteLine("WARNING: object count {0} exceeds the object limit", objectCount);

            for (int i = 0; i < objectCount; ++i)
            {
                var obj = Objects.objectEntityList[32 + i];

                ushort attribs = FileIO.ReadUInt16();
                obj.type = FileIO.ReadByte();
                obj.propertyValue = FileIO.ReadByte();

                obj.xpos = FileIO.ReadInt32();
                obj.ypos = FileIO.ReadInt32();

                if ((attribs & 0x1) != 0)
                {
                    obj.state = FileIO.ReadInt32();
                }
                if ((attribs & 0x2) != 0)
                {
                    obj.direction = FileIO.ReadByte();
                }
                if ((attribs & 0x4) != 0)
                {
                    obj.scale = FileIO.ReadInt32();
                }
                if ((attribs & 0x8) != 0)
                {
                    obj.rotation = FileIO.ReadInt32();
                }
                if ((attribs & 0x10) != 0)
                {
                    obj.drawOrder = (sbyte)FileIO.ReadByte();
                }
                if ((attribs & 0x20) != 0)
                {
                    obj.priority = FileIO.ReadByte();
                }
                if ((attribs & 0x40) != 0)
                {
                    obj.alpha = FileIO.ReadByte();
                }
                if ((attribs & 0x80) != 0)
                {
                    obj.animation = FileIO.ReadByte();
                }
                if ((attribs & 0x100) != 0)
                {
                    obj.animationSpeed = FileIO.ReadInt32();
                }
                if ((attribs & 0x200) != 0)
                {
                    obj.frame = FileIO.ReadByte();
                }
                if ((attribs & 0x400) != 0)
                {
                    obj.inkEffect = FileIO.ReadByte();
                }
                if ((attribs & 0x800) != 0)
                {
                    obj.values[0] = FileIO.ReadInt32();
                }
                if ((attribs & 0x1000) != 0)
                {
                    obj.values[1] = FileIO.ReadInt32();
                }
                if ((attribs & 0x2000) != 0)
                {
                    obj.values[2] = FileIO.ReadInt32();
                }
                if ((attribs & 0x4000) != 0)
                {
                    obj.values[3] = FileIO.ReadInt32();
                }

                //++obj;
            }
        }
        stageLayouts[0].type = LAYER.HSCROLL;
        FileIO.CloseFile();
    }

    public static bool LoadStageFile(string path, int stageId, out FileInfo fileInfo)
    {
        var fileName = $"Data/Stages/{Engine.stageList[activeStageList][stageId].folder}/{path}";
        return FileIO.LoadFile(fileName, out fileInfo);
    }

    public static bool LoadActFile(string ext, int stageId, out FileInfo fileInfo)
    {
        var fileName = $"Data/Stages/{Engine.stageList[activeStageList][stageId].folder}/Act{Engine.stageList[activeStageList][stageId].id}{ext}";

        actId = int.Parse(Engine.stageList[activeStageList][stageId].id, NumberStyles.HexNumber);

        return FileIO.LoadFile(fileName, out fileInfo);
    }

    public static bool CheckCurrentStageFolder(int stage)
    {
        if (currentStageFolder == Engine.stageList[activeStageList][stage].folder)
        {
            return true;
        }
        else
        {
            currentStageFolder = Engine.stageList[activeStageList][stage].folder;
            return false;
        }
    }

    public static void ResetBackgroundSettings()
    {
        for (int i = 0; i < LAYER_COUNT; ++i)
        {
            stageLayouts[i].deformationOffset = 0;
            stageLayouts[i].deformationOffsetW = 0;
            stageLayouts[i].scrollPos = 0;
        }

        for (int i = 0; i < PARALLAX_COUNT; ++i)
        {
            hParallax.scrollPos[i] = 0;
            vParallax.scrollPos[i] = 0;
        }

        for (int i = 0; i < DEFORM_COUNT; ++i)
        {
            bgDeformationData0[i] = 0;
            bgDeformationData1[i] = 0;
            bgDeformationData2[i] = 0;
            bgDeformationData3[i] = 0;
        }
    }

    public static void SetPlayerScreenPosition(Entity target)
    {
        int targetX = target.xpos >> 16;
        int targetY = cameraAdjustY + (target.ypos >> 16);
        if (newYBoundary1 > curYBoundary1)
        {
            if (newYBoundary1 >= yScrollOffset)
                curYBoundary1 = yScrollOffset;
            else
                curYBoundary1 = newYBoundary1;
        }
        if (newYBoundary1 < curYBoundary1)
        {
            if (curYBoundary1 >= yScrollOffset)
                --curYBoundary1;
            else
                curYBoundary1 = newYBoundary1;
        }
        if (newYBoundary2 < curYBoundary2)
        {
            if (curYBoundary2 <= yScrollOffset + Drawing.SCREEN_YSIZE || newYBoundary2 >= yScrollOffset + Drawing.SCREEN_YSIZE)
                --curYBoundary2;
            else
                curYBoundary2 = yScrollOffset + Drawing.SCREEN_YSIZE;
        }
        if (newYBoundary2 > curYBoundary2)
        {
            if (yScrollOffset + Drawing.SCREEN_YSIZE >= curYBoundary2)
            {
                ++curYBoundary2;
                if (target.yvel > 0)
                {
                    int buf = curYBoundary2 + (target.yvel >> 16);
                    if (newYBoundary2 < buf)
                    {
                        curYBoundary2 = newYBoundary2;
                    }
                    else
                    {
                        curYBoundary2 += target.yvel >> 16;
                    }
                }
            }
            else
                curYBoundary2 = newYBoundary2;
        }
        if (newXBoundary1 > curXBoundary1)
        {
            if (xScrollOffset <= newXBoundary1)
                curXBoundary1 = xScrollOffset;
            else
                curXBoundary1 = newXBoundary1;
        }
        if (newXBoundary1 < curXBoundary1)
        {
            if (xScrollOffset <= curXBoundary1)
            {
                --curXBoundary1;
                if (target.xvel < 0)
                {
                    curXBoundary1 += target.xvel >> 16;
                    if (curXBoundary1 < newXBoundary1)
                        curXBoundary1 = newXBoundary1;
                }
            }
            else
            {
                curXBoundary1 = newXBoundary1;
            }
        }
        if (newXBoundary2 < curXBoundary2)
        {
            if (newXBoundary2 > Drawing.SCREEN_XSIZE + xScrollOffset)
                curXBoundary2 = newXBoundary2;
            else
                curXBoundary2 = Drawing.SCREEN_XSIZE + xScrollOffset;
        }
        if (newXBoundary2 > curXBoundary2)
        {
            if (Drawing.SCREEN_XSIZE + xScrollOffset >= curXBoundary2)
            {
                ++curXBoundary2;
                if (target.xvel > 0)
                {
                    curXBoundary2 += target.xvel >> 16;
                    if (curXBoundary2 > newXBoundary2)
                        curXBoundary2 = newXBoundary2;
                }
            }
            else
            {
                curXBoundary2 = newXBoundary2;
            }
        }

        int xPosDif = targetX - cameraXPos;
        if (targetX > cameraXPos)
        {
            xPosDif -= 8;
            if (xPosDif >= 0)
            {
                if (xPosDif >= 17)
                    xPosDif = 16;
            }
            else
            {
                xPosDif = 0;
            }
        }
        else
        {
            xPosDif += 8;
            if (xPosDif > 0)
            {
                xPosDif = 0;
            }
            else if (xPosDif <= -17)
            {
                xPosDif = -16;
            }
        }

        int centeredXBound1 = cameraXPos + xPosDif;
        cameraXPos = centeredXBound1;
        if (centeredXBound1 < Drawing.SCREEN_CENTERX + curXBoundary1)
        {
            cameraXPos = Drawing.SCREEN_CENTERX + curXBoundary1;
            centeredXBound1 = Drawing.SCREEN_CENTERX + curXBoundary1;
        }

        int centeredXBound2 = curXBoundary2 - Drawing.SCREEN_CENTERX;
        if (centeredXBound2 < centeredXBound1)
        {
            cameraXPos = centeredXBound2;
            centeredXBound1 = centeredXBound2;
        }

        int yPosDif = 0;
        if (target.scrollTracking)
        {
            if (targetY <= cameraYPos)
            {
                yPosDif = targetY - cameraYPos + 32;
                if (yPosDif <= 0)
                {
                    if (yPosDif <= -17)
                        yPosDif = -16;
                }
                else
                    yPosDif = 0;
            }
            else
            {
                yPosDif = targetY - cameraYPos - 32;
                if (yPosDif >= 0)
                {
                    if (yPosDif >= 17)
                        yPosDif = 16;
                }
                else
                    yPosDif = 0;
            }
            cameraLockedY = 1;
        }
        else if (targetY <= cameraYPos)
        {
            yPosDif = targetY - cameraYPos;
            if (targetY - cameraYPos <= 0)
            {
                if (yPosDif >= -32 && Math.Abs(target.yvel) <= 0x60000)
                {
                    if (yPosDif < -6)
                    {
                        yPosDif = -6;
                    }
                }
                else if (yPosDif < -16)
                {
                    yPosDif = -16;
                }
            }
            else
            {
                yPosDif = 0;
                cameraLockedY = 1;
            }
        }
        else
        {
            yPosDif = targetY - cameraYPos;
            if (targetY - cameraYPos < 0)
            {
                yPosDif = 0;
                cameraLockedY = 1;
            }
            else if (yPosDif > 32 || Math.Abs(target.yvel) > 0x60000)
            {
                if (yPosDif > 16)
                {
                    yPosDif = 16;
                }
                else
                {
                    cameraLockedY = 1;
                }
            }
            else
            {
                if (yPosDif <= 6)
                {
                    cameraLockedY = 1;
                }
                else
                {
                    yPosDif = 6;
                }
            }
        }

        int newCamY = cameraYPos + yPosDif;
        if (newCamY <= curYBoundary1 + (SCREEN_SCROLL_UP - 1))
            newCamY = curYBoundary1 + SCREEN_SCROLL_UP;
        cameraYPos = newCamY;
        if (curYBoundary2 - (SCREEN_SCROLL_DOWN - 1) <= newCamY)
        {
            cameraYPos = curYBoundary2 - SCREEN_SCROLL_DOWN;
        }

        xScrollOffset = cameraShakeX + centeredXBound1 - Drawing.SCREEN_CENTERX;

        int pos = cameraYPos + target.lookPosY - SCREEN_SCROLL_UP;
        if (pos < curYBoundary1)
        {
            yScrollOffset = curYBoundary1;
        }
        else
        {
            yScrollOffset = cameraYPos + target.lookPosY - SCREEN_SCROLL_UP;
        }

        int y = curYBoundary2 - Drawing.SCREEN_YSIZE;
        if (curYBoundary2 - (Drawing.SCREEN_YSIZE - 1) > yScrollOffset)
            y = yScrollOffset;
        yScrollOffset = cameraShakeY + y;

        if (cameraShakeX != 0)
        {
            if (cameraShakeX <= 0)
            {
                cameraShakeX = ~cameraShakeX;
            }
            else
            {
                cameraShakeX = -cameraShakeX;
            }
        }

        if (cameraShakeY != 0)
        {
            if (cameraShakeY <= 0)
            {
                cameraShakeY = ~cameraShakeY;
            }
            else
            {
                cameraShakeY = -cameraShakeY;
            }
        }
    }

    public static void SetPlayerLockedScreenPosition(Entity target)
    {
        if (newYBoundary1 > curYBoundary1)
        {
            if (yScrollOffset <= newYBoundary1)
                curYBoundary1 = yScrollOffset;
            else
                curYBoundary1 = newYBoundary1;
        }
        if (newYBoundary1 < curYBoundary1)
        {
            if (yScrollOffset <= curYBoundary1)
                --curYBoundary1;
            else
                curYBoundary1 = newYBoundary1;
        }
        if (newYBoundary2 < curYBoundary2)
        {
            if (curYBoundary2 <= yScrollOffset + Drawing.SCREEN_YSIZE || newYBoundary2 >= yScrollOffset + Drawing.SCREEN_YSIZE)
                --curYBoundary2;
            else
                curYBoundary2 = yScrollOffset + Drawing.SCREEN_YSIZE;
        }
        if (newYBoundary2 > curYBoundary2)
        {
            if (yScrollOffset + Drawing.SCREEN_YSIZE >= curYBoundary2)
                ++curYBoundary2;
            else
                curYBoundary2 = newYBoundary2;
        }
        if (newXBoundary1 > curXBoundary1)
        {
            if (xScrollOffset <= newXBoundary1)
                curXBoundary1 = xScrollOffset;
            else
                curXBoundary1 = newXBoundary1;
        }
        if (newXBoundary1 < curXBoundary1)
        {
            if (xScrollOffset <= curXBoundary1)
            {
                --curXBoundary1;
                if (target.xvel < 0)
                {
                    curXBoundary1 += target.xvel >> 16;
                    if (curXBoundary1 < newXBoundary1)
                        curXBoundary1 = newXBoundary1;
                }
            }
            else
            {
                curXBoundary1 = newXBoundary1;
            }
        }
        if (newXBoundary2 < curXBoundary2)
        {
            if (newXBoundary2 > Drawing.SCREEN_XSIZE + xScrollOffset)
                curXBoundary2 = newXBoundary2;
            else
                curXBoundary2 = Drawing.SCREEN_XSIZE + xScrollOffset;
        }
        if (newXBoundary2 > curXBoundary2)
        {
            if (Drawing.SCREEN_XSIZE + xScrollOffset >= curXBoundary2)
            {
                ++curXBoundary2;
                if (target.xvel > 0)
                {
                    curXBoundary2 += target.xvel >> 16;
                    if (curXBoundary2 > newXBoundary2)
                        curXBoundary2 = newXBoundary2;
                }
            }
            else
            {
                curXBoundary2 = newXBoundary2;
            }
        }

        if (cameraShakeX != 0)
        {
            if (cameraShakeX <= 0)
            {
                cameraShakeX = ~cameraShakeX;
            }
            else
            {
                cameraShakeX = -cameraShakeX;
            }
        }

        if (cameraShakeY != 0)
        {
            if (cameraShakeY <= 0)
            {
                cameraShakeY = ~cameraShakeY;
            }
            else
            {
                cameraShakeY = -cameraShakeY;
            }
        }
    }

    public static void SetLayerDeformation(
          int selectedDef,
          int waveLength,
          int waveWidth,
          int wType,
          int yPos,
          int wSize)
    {
        int index1 = 0;
        switch (selectedDef)
        {
            case 0:
                switch (wType)
                {
                    case 0:
                        for (int index2 = 0; index2 < 131072; index2 += 512)
                        {
                            bgDeformationData0[index1] = FastMath.sinVal512[index2 / waveLength & 511] * waveWidth >> 5;
                            ++index1;
                        }
                        break;
                    case 1:
                        int index3 = index1 + yPos;
                        for (int index2 = 0; index2 < wSize; ++index2)
                        {
                            bgDeformationData0[index3] = FastMath.sinVal512[(index2 << 9) / waveLength & 511] * waveWidth >> 5;
                            ++index3;
                        }
                        break;
                }
                for (int index2 = 256; index2 < 576; ++index2)
                    bgDeformationData0[index2] = bgDeformationData0[index2 - 256];
                break;
            case 1:
                switch (wType)
                {
                    case 0:
                        for (int index2 = 0; index2 < 131072; index2 += 512)
                        {
                            bgDeformationData1[index1] = FastMath.sinVal512[index2 / waveLength & 511] * waveWidth >> 5;
                            ++index1;
                        }
                        break;
                    case 1:
                        int index4 = index1 + yPos;
                        for (int index2 = 0; index2 < wSize; ++index2)
                        {
                            bgDeformationData1[index4] = FastMath.sinVal512[(index2 << 9) / waveLength & 511] * waveWidth >> 5;
                            ++index4;
                        }
                        break;
                }
                for (int index2 = 256; index2 < 576; ++index2)
                    bgDeformationData1[index2] = bgDeformationData1[index2 - 256];
                break;
            case 2:
                switch (wType)
                {
                    case 0:
                        for (int index2 = 0; index2 < 131072; index2 += 512)
                        {
                            bgDeformationData2[index1] = FastMath.sinVal512[index2 / waveLength & 511] * waveWidth >> 5;
                            ++index1;
                        }
                        break;
                    case 1:
                        int index5 = index1 + yPos;
                        for (int index2 = 0; index2 < wSize; ++index2)
                        {
                            bgDeformationData2[index5] = FastMath.sinVal512[(index2 << 9) / waveLength & 511] * waveWidth >> 5;
                            ++index5;
                        }
                        break;
                }
                for (int index2 = 256; index2 < 576; ++index2)
                    bgDeformationData2[index2] = bgDeformationData2[index2 - 256];
                break;
            case 3:
                switch (wType)
                {
                    case 0:
                        for (int index2 = 0; index2 < 131072; index2 += 512)
                        {
                            bgDeformationData3[index1] = FastMath.sinVal512[index2 / waveLength & 511] * waveWidth >> 5;
                            ++index1;
                        }
                        break;
                    case 1:
                        int index6 = index1 + yPos;
                        for (int index2 = 0; index2 < wSize; ++index2)
                        {
                            bgDeformationData3[index6] = FastMath.sinVal512[(index2 << 9) / waveLength & 511] * waveWidth >> 5;
                            ++index6;
                        }
                        break;
                }
                for (int index2 = 256; index2 < 576; ++index2)
                    bgDeformationData3[index2] = bgDeformationData3[index2 - 256];
                break;
        }
    }
}
