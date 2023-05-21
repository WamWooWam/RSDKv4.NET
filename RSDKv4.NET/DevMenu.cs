using RSDKv4.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using static RSDKv4.Input;
using static RSDKv4.Drawing;
using static RSDKv4.Text;
using RSDKv4.Render;

namespace RSDKv4;

public static class DevMenu
{
    static bool endLine = true;
    static int touchFlags = 0;
    static int taListStore = 0;

    public static void InitDevMenu()
    {
#if RETRO_USE_MOD_LOADER
    for (int m = 0; m < modList.size(); ++m) ScanModFolder(&modList[m]);
#endif
        // DrawStageGFXHQ = 0;
        Scene.xScrollOffset = 0;
        Scene.yScrollOffset = 0;
        Audio.StopMusic(true);
        Audio.StopAllSfx();
        Audio.ReleaseStageSfx();
        Palette.fadeMode = 0;
        Objects.playerListPos = 0;
        Engine.engineState = ENGINE_STATE.DEVMENU;
        Drawing.ClearGraphicsData();
        Animation.ClearAnimationData();
        Palette.SetActivePalette(0, 0, 256);
        //textMenuSurfaceNo = SURFACE_COUNT - 1;
        Drawing.LoadGIFFile("Data/Game/SystemText.gif", Drawing.TEXTMENU_SURFACE);
        Palette.SetPaletteEntry(0xFF, 1, 0x00, 0x00, 0x00);
        Palette.SetPaletteEntry(0xFF, 8, 0x80, 0x80, 0x80);
        Palette.SetPaletteEntry(0xFF, 0xF0, 0x00, 0x00, 0x00);
        Palette.SetPaletteEntry(0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        SetTextMenu(DEVMENU.MAIN);
        //drawStageGFXHQ = false;
#if !RETRO_USE_ORIGINAL_CODE
        Objects.RemoveNativeObjects<PauseMenu>();
#endif
#if RETRO_HARDWARE_RENDER
    render3DEnabled = false;
    UpdateHardwareTextures();
#endif
    }

    public static void InitErrorMessage()
    {
        Scene.xScrollOffset = 0;
        Scene.yScrollOffset = 0;
        Audio.StopMusic(true);
        Audio.StopAllSfx();
        Audio.ReleaseStageSfx();
        Palette.fadeMode = 0;
        Engine.engineState = ENGINE_STATE.DEVMENU;
        Drawing.ClearGraphicsData();
        Animation.ClearAnimationData();
        Palette.SetActivePalette(0, 0, 256);
        Drawing.LoadGIFFile("Data/Game/SystemText.gif", Drawing.TEXTMENU_SURFACE);
        Palette.SetPaletteEntry(0xFF, 1, 0x00, 0x00, 0x00);
        Palette.SetPaletteEntry(0xFF, 8, 0x80, 0x80, 0x80);
        Palette.SetPaletteEntry(0xFF, 0xF0, 0x00, 0x00, 0x00);
        Palette.SetPaletteEntry(0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        Text.gameMenu[0].alignment = 2;
        Text.gameMenu[0].selectionCount = 1;
        Text.gameMenu[0].selection1 = 0;
        Text.gameMenu[1].visibleRowCount = 0;
        Text.gameMenu[1].visibleRowOffset = 0;
        Scene.stageMode = DEVMENU.SCRIPTERROR;
        //drawStageGFXHQ = false;
#if !RETRO_USE_ORIGINAL_CODE
        Objects.RemoveNativeObjects<PauseMenu>();
#endif
        // TODO: ?
        Drawing.surfaceDirty = true;
    }

    public static void ProcessStageSelect()
    {
        Drawing.BeignDraw();
        Drawing.EnsureBlendMode(BlendMode.None);

        ClearScreen(0xF0);

        Input.CheckKeyDown(ref Input.keyDown);
        Input.CheckKeyPress(ref Input.keyPress);

        //#if defined RETRO_USING_MOUSE || defined RETRO_USING_TOUCH
        DrawSprite(32, 0x42, 16, 16, 78, 240, TEXTMENU_SURFACE);
        DrawSprite(32, 0xB2, 16, 16, 95, 240, TEXTMENU_SURFACE);
        DrawSprite(SCREEN_XSIZE - 32, SCREEN_YSIZE - 32, 16, 16, 112, 240, TEXTMENU_SURFACE);
        //#endif

        if (!keyDown.start && !keyDown.up && !keyDown.down)
        {
            int tFlags = touchFlags;
            touchFlags = 0;

            for (int t = 0; t < touches; ++t)
            {
                if (touchDown[t] != 0)
                {
                    if (touchX[t] < SCREEN_CENTERX)
                    {
                        if (touchY[t] >= SCREEN_CENTERY)
                        {
                            if ((tFlags & 2) == 0)
                                keyPress.down = true;
                            else
                                touchFlags |= 1 << 1;
                        }
                        else
                        {
                            if ((tFlags & 1) == 0)
                                keyPress.up = true;
                            else
                                touchFlags |= 1 << 0;
                        }
                    }
                    else if (touchX[t] > SCREEN_CENTERX)
                    {
                        if (touchY[t] > SCREEN_CENTERY)
                        {
                            if ((tFlags & 4) == 0)
                                keyPress.start = true;
                            else
                                touchFlags |= 1 << 2;
                        }
                        else
                        {
                            if ((tFlags & 8) == 0)
                                keyPress.B = true;
                            else
                                touchFlags |= 1 << 3;
                        }
                    }
                }
            }

            touchFlags |= (int)(keyPress.up ? 1 : 0) << 0;
            touchFlags |= (int)(keyPress.down ? 1 : 0) << 1;
            touchFlags |= (int)(keyPress.start ? 1 : 0) << 2;
            touchFlags |= (int)(keyPress.B ? 1 : 0) << 3;
        }

        switch (Scene.stageMode)
        {
            case DEVMENU.MAIN: // Main Menu
                {
                    if (keyPress.down)
                        gameMenu[0].selection2 += 2;

                    if (keyPress.up)
                        gameMenu[0].selection2 -= 2;

                    int count = 15;
#if RETRO_USE_MOD_LOADER
            count += 2;
#endif

                    if (gameMenu[0].selection2 > count)
                        gameMenu[0].selection2 = 9;
                    if (gameMenu[0].selection2 < 9)
                        gameMenu[0].selection2 = count;

                    DrawTextMenu(gameMenu[0], SCREEN_CENTERX, 72);
                    if (keyPress.start || keyPress.A)
                    {
                        if (gameMenu[0].selection2 == 9)
                        {
                            ClearGraphicsData();
                            Animation.ClearAnimationData();
                            Scene.activeStageList = 0;
                            Scene.stageMode = STAGEMODE.LOAD;
                            Engine.engineState = ENGINE_STATE.MAINGAME;
                            Scene.stageListPosition = 0;
                        }
                        else if (gameMenu[0].selection2 == 11)
                        {
                            SetupTextMenu(gameMenu[0], 0);
                            AddTextMenuEntry(gameMenu[0], "SELECT A PLAYER");
                            SetupTextMenu(gameMenu[1], 0);
                            LoadConfigListText(gameMenu[1], 0);
                            gameMenu[1].alignment = 0;
                            gameMenu[1].selectionCount = 1;
                            gameMenu[1].selection1 = 0;
                            Scene.stageMode = DEVMENU.PLAYERSEL;
                        }
                        else if (gameMenu[0].selection2 == 13)
                        {
                            Objects.ClearNativeObjects();
                            Engine.engineState = ENGINE_STATE.WAIT;
                            Engine.nativeMenuFadeIn = false;
                            if (Engine.skipStartMenu)
                            {
                                ClearGraphicsData();
                                Animation.ClearAnimationData();
                                Scene.activeStageList = 0;
                                Scene.stageMode = STAGEMODE.LOAD;
                                Engine.engineState = ENGINE_STATE.MAINGAME;
                                Scene.stageListPosition = 0;
                                Objects.CreateNativeObject(() => new RetroGameLoop());
                                if (Engine.deviceType == DEVICE.MOBILE)
                                    Objects.CreateNativeObject(() => new VirtualDPad());
                            }
                            else
                            {
                                Objects.CreateNativeObject(() => new SegaSplash());
                            }
                        }
#if RETRO_USE_MOD_LOADER
                        else if (gameMenu[0].selection2 == 15) {
                            InitMods(); // reload mods
                            SetTextMenu(DEVMENU_MODMENU);
                        }
#endif
                        else
                        {
#if RETRO_USE_MOD_LOADER
                            ExitGame();
#else
                            //Engine.running = false;
#endif
                        }
                    }
                    else if (keyPress.B)
                    {
                        ClearGraphicsData();
                        Animation.ClearAnimationData();
                        Scene.activeStageList = 0;
                        Scene.stageMode = STAGEMODE.LOAD;
                        Engine.engineState = ENGINE_STATE.MAINGAME;
                        Scene.stageListPosition = 0;
                    }
                    break;
                }
            case DEVMENU.PLAYERSEL: // Selecting Player
                {
                    if (keyPress.down)
                        ++gameMenu[1].selection1;
                    if (keyPress.up)
                        --gameMenu[1].selection1;
                    if (gameMenu[1].selection1 == gameMenu[1].rowCount)
                        gameMenu[1].selection1 = 0;

                    if (gameMenu[1].selection1 < 0)
                        gameMenu[1].selection1 = gameMenu[1].rowCount - 1;

                    DrawTextMenu(gameMenu[0], SCREEN_CENTERX - 4, 72);
                    DrawTextMenu(gameMenu[1], SCREEN_CENTERX - 40, 96);
                    if (keyPress.start || keyPress.A)
                    {
                        Objects.playerListPos = gameMenu[1].selection1;
                        SetTextMenu(DEVMENU.STAGELISTSEL);
                    }
                    else if (keyPress.B)
                    {
                        SetTextMenu(DEVMENU.MAIN);
                    }
                    break;
                }
            case DEVMENU.STAGELISTSEL: // Selecting Category
                {
                    if (keyPress.down)
                        gameMenu[0].selection2 += 2;
                    if (keyPress.up)
                        gameMenu[0].selection2 -= 2;

                    if (gameMenu[0].selection2 > 9)
                        gameMenu[0].selection2 = 3;

                    if (gameMenu[0].selection2 < 3)
                        gameMenu[0].selection2 = 9;

                    DrawTextMenu(gameMenu[0], SCREEN_CENTERX - 80, 72);
                    bool nextMenu = false;
                    switch (gameMenu[0].selection2)
                    {
                        case 3: // Presentation
                            if (Engine.stageListCount[0] > 0)
                                nextMenu = true;
                            Scene.activeStageList = 0;
                            break;
                        case 5: // Regular
                            if (Engine.stageListCount[1] > 0)
                                nextMenu = true;
                            Scene.activeStageList = 1;
                            break;
                        case 7: // Special
                            if (Engine.stageListCount[3] > 0)
                                nextMenu = true;
                            Scene.activeStageList = 3;
                            break;
                        case 9: // Bonus
                            if (Engine.stageListCount[2] > 0)
                                nextMenu = true;
                            Scene.activeStageList = 2;
                            break;
                        default: break;
                    }

                    if ((keyPress.start || keyPress.A) && nextMenu)
                    {
                        SetupTextMenu(gameMenu[0], 0);
                        AddTextMenuEntry(gameMenu[0], "SELECT A STAGE");
                        SetupTextMenu(gameMenu[1], 0);
                        Text.LoadConfigListText(gameMenu[1], ((gameMenu[0].selection2 - 3) >> 1) + 1);
                        gameMenu[1].alignment = 1;
                        gameMenu[1].selectionCount = 3;
                        gameMenu[1].selection1 = 0;
                        if (gameMenu[1].rowCount > 18)
                            gameMenu[1].visibleRowCount = 18;
                        else
                            gameMenu[1].visibleRowCount = 0;

                        gameMenu[0].alignment = 2;
                        gameMenu[0].selectionCount = 1;
                        gameMenu[1].timer = 0;
                        Scene.stageMode = DEVMENU.STAGESEL;
                    }
                    else if (keyPress.B)
                    {
                        SetupTextMenu(gameMenu[0], 0);
                        AddTextMenuEntry(gameMenu[0], "SELECT A PLAYER");
                        SetupTextMenu(gameMenu[1], 0);
                        Text.LoadConfigListText(gameMenu[1], 0);
                        gameMenu[0].alignment = 2;
                        gameMenu[1].alignment = 0;
                        gameMenu[1].selectionCount = 1;
                        gameMenu[1].visibleRowCount = 0;
                        gameMenu[1].visibleRowOffset = 0;
                        gameMenu[1].selection1 = Objects.playerListPos;
                        Scene.stageMode = DEVMENU.PLAYERSEL;
                    }
                    break;
                }
            case DEVMENU.STAGESEL: // Selecting Stage
                {
                    if (keyDown.down)
                    {
                        gameMenu[1].timer += 1;
                        if (gameMenu[1].timer > 8)
                        {
                            gameMenu[1].timer = 0;
                            keyPress.down = true;
                        }
                    }
                    else
                    {
                        if (keyDown.up)
                        {
                            gameMenu[1].timer -= 1;
                            if (gameMenu[1].timer < -8)
                            {
                                gameMenu[1].timer = 0;
                                keyPress.up = true;
                            }
                        }
                        else
                        {
                            gameMenu[1].timer = 0;
                        }
                    }
                    if (keyPress.down)
                    {
                        gameMenu[1].selection1++;
                        if (gameMenu[1].selection1 - gameMenu[1].visibleRowOffset >= gameMenu[1].visibleRowCount)
                        {
                            gameMenu[1].visibleRowOffset += 1;
                        }
                    }
                    if (keyPress.up)
                    {
                        gameMenu[1].selection1--;
                        if (gameMenu[1].selection1 - gameMenu[1].visibleRowOffset < 0)
                        {
                            gameMenu[1].visibleRowOffset -= 1;
                        }
                    }
                    if (gameMenu[1].selection1 == gameMenu[1].rowCount)
                    {
                        gameMenu[1].selection1 = 0;
                        gameMenu[1].visibleRowOffset = 0;
                    }
                    if (gameMenu[1].selection1 < 0)
                    {
                        gameMenu[1].selection1 = gameMenu[1].rowCount - 1;
                        gameMenu[1].visibleRowOffset = (ushort)(gameMenu[1].rowCount - gameMenu[1].visibleRowCount);
                    }

                    DrawTextMenu(gameMenu[0], SCREEN_CENTERX - 4, 40);
                    DrawTextMenu(gameMenu[1], SCREEN_CENTERX + 100, 64);
                    if (keyPress.start || keyPress.A)
                    {
                        Scene.debugMode = keyDown.A;
                        Scene.stageMode = STAGEMODE.LOAD;
                        Engine.engineState = ENGINE_STATE.MAINGAME;
                        Scene.stageListPosition = gameMenu[1].selection1;
                        Engine.SetGlobalVariableByName("options.gameMode", 0);
                        Engine.SetGlobalVariableByName("lampPostID", 0); // For S1
                        Engine.SetGlobalVariableByName("starPostID", 0); // For S2
                    }
                    else if (keyPress.B)
                    {
                        SetTextMenu(DEVMENU.STAGELISTSEL);
                    }
                    break;
                }
            case DEVMENU.SCRIPTERROR: // Script Error
                {
                    DrawTextMenu(gameMenu[0], SCREEN_CENTERX, 72);
                    if (keyPress.start || keyPress.A)
                    {
                        SetTextMenu(DEVMENU.MAIN);
                    }
                    else if (keyPress.B)
                    {
                        Drawing.ClearGraphicsData();
                        Animation.ClearAnimationData();
                        Scene.activeStageList = 0;
                        Scene.stageMode = DEVMENU.STAGESEL;
                        Engine.engineState = ENGINE_STATE.MAINGAME;
                        Scene.stageListPosition = 0;
                    }
                    else if (keyPress.C)
                    {
                        Drawing.ClearGraphicsData();
                        Animation.ClearAnimationData();
                        Scene.stageMode = STAGEMODE.LOAD;
                        Engine.engineState = ENGINE_STATE.MAINGAME;
                    }
                    break;
                }
#if RETRO_USE_MOD_LOADER
        case DEVMENU.MODMENU: // Mod Menu
        {
            int preOption = gameMenu[1].selection1;
            if (keyDown.down) {
                gameMenu[1].timer++;
                if (gameMenu[1].timer > 8) {
                    gameMenu[1].timer = 0;
                    keyPress.down   = true;
                }
            }
            else {
                if (keyDown.up) {
                    gameMenu[1].timer--;
                    if (gameMenu[1].timer < -8) {
                        gameMenu[1].timer = 0;
                        keyPress.up     = true;
                    }
                }
                else {
                    gameMenu[1].timer = 0;
                }
            }

            if (keyPress.down) {
                gameMenu[1].selection1++;
                if (gameMenu[1].selection1 - gameMenu[1].visibleRowOffset >= gameMenu[1].visibleRowCount) {
                    gameMenu[1].visibleRowOffset++;
                }
            }

            if (keyPress.up) {
                gameMenu[1].selection1--;
                if (gameMenu[1].selection1 - gameMenu[1].visibleRowOffset < 0 && gameMenu[1].visibleRowOffset > 0) {
                    gameMenu[1].visibleRowOffset--;
                }
            }

            if (gameMenu[1].selection1 >= gameMenu[1].rowCount) {
                if (keyDown.C) {
                    gameMenu[1].selection1--;
                    gameMenu[1].visibleRowOffset--;
                }
                else {
                    gameMenu[1].selection1       = 0;
                    gameMenu[1].visibleRowOffset = 0;
                }
            }

            if (gameMenu[1].selection1 < 0) {
                if (keyDown.C) {
                    gameMenu[1].selection1++;
                }
                else {
                    gameMenu[1].selection1       = gameMenu[1].rowCount - 1;
                    gameMenu[1].visibleRowOffset = gameMenu[1].rowCount - gameMenu[1].visibleRowCount;
                }
            }
            gameMenu[1].selection2 = gameMenu[1].selection1; // its a bug fix LOL

            char buffer[0x100];
            if (gameMenu[1].selection1 < modList.size() && (keyPress.A || keyPress.start || keyPress.left || keyPress.right)) {
                modList[gameMenu[1].selection1].active ^= 1;
                StrCopy(buffer, modList[gameMenu[1].selection1].name.c_str());
                StrAdd(buffer, ": ");
                StrAdd(buffer, (modList[gameMenu[1].selection1].active ? "  Active" : "Inactive"));
                EditTextMenuEntry(gameMenu[1], buffer, gameMenu[1].selection1);
            }
            else if (keyDown.C && gameMenu[1].selection1 != preOption) {
                int visibleOffset  = gameMenu[1].visibleRowOffset;
                int option         = gameMenu[1].selection1;
                ModInfo swap       = modList[preOption];
                modList[preOption] = modList[option];
                modList[option]    = swap;
                SetTextMenu(DEVMENU_MODMENU);
                gameMenu[1].selection1       = option;
                gameMenu[1].visibleRowOffset = visibleOffset;
            }
            else if (keyPress.B) {
                RefreshEngine();

                if (Engine.modMenuCalled) {
                    stageMode            = STAGEMODE_LOAD;
                    Engine.gameMode      = ENGINE_MAINGAME;
                    Engine.modMenuCalled = false;

                    if (stageListPosition >= stageListCount[activeStageList]) {
                        activeStageList   = 0;
                        stageListPosition = 0;
                    }
                }
                else {
                    SetTextMenu(DEVMENU_MAIN);

                    SetPaletteEntry(-1, 1, 0x00, 0x00, 0x00);
                    SetPaletteEntry(-1, 8, 0x80, 0x80, 0x80);
                    SetPaletteEntry(-1, 0xF0, 0x00, 0x00, 0x00);
                    SetPaletteEntry(-1, 0xFF, 0xFF, 0xFF, 0xFF);
                }
            }

            DrawTextMenu(gameMenu[0], SCREEN_CENTERX - 4, 40);
            DrawTextMenu(gameMenu[1], SCREEN_CENTERX + 100, 64);
            break;
        }
#endif
            default: break;
        }

        Drawing.EndDraw();
    }

    public static void SetTextMenu(int sm)
    {
        Scene.stageMode = sm;
        SetupTextMenu(gameMenu[0], 0);
        SetupTextMenu(gameMenu[1], 0);
        switch (sm)
        {
            case DEVMENU.MAIN:
                {
                    AddTextMenuEntry(gameMenu[0], "RETRO ENGINE DEV MENU");
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], Engine.gameWindowText + " Version");
                    AddTextMenuEntry(gameMenu[0], Engine.gameVersion);
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], "START GAME");
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], "STAGE SELECT");
#if !RETRO_USE_ORIGINAL_CODE
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], "START MENU");
#if RETRO_USE_MOD_LOADER
            AddTextMenuEntry(gameMenu[0], " ");
            AddTextMenuEntry(gameMenu[0], "MODS");
#endif
                    AddTextMenuEntry(gameMenu[0], " ");
                    AddTextMenuEntry(gameMenu[0], "EXIT GAME");
#endif
                    gameMenu[0].alignment = 2;
                    gameMenu[0].selectionCount = 2;
                    gameMenu[0].selection1 = 0;
                    gameMenu[0].selection2 = 9;
                    gameMenu[1].visibleRowCount = 0;
                    gameMenu[1].visibleRowOffset = 0;
                    break;
                }
            case DEVMENU.STAGELISTSEL:
                AddTextMenuEntry(gameMenu[0], "SELECT A STAGE LIST");
                AddTextMenuEntry(gameMenu[0], " ");
                AddTextMenuEntry(gameMenu[0], " ");
                AddTextMenuEntry(gameMenu[0], "   PRESENTATION");
                AddTextMenuEntry(gameMenu[0], " ");
                AddTextMenuEntry(gameMenu[0], "   REGULAR");
                AddTextMenuEntry(gameMenu[0], " ");
                AddTextMenuEntry(gameMenu[0], "   SPECIAL");
                AddTextMenuEntry(gameMenu[0], " ");
                AddTextMenuEntry(gameMenu[0], "   BONUS");
                gameMenu[0].alignment = 0;
                gameMenu[0].selection2 = 3;
                gameMenu[0].selectionCount = 2;
                break;
#if RETRO_USE_MOD_LOADER
        case DEVMENU_MODMENU:
            SetupTextMenu(gameMenu[0], 0);
            AddTextMenuEntry(gameMenu[0], "MOD LIST");
            SetupTextMenu(gameMenu[1], 0);

            char buffer[0x100];
            for (int m = 0; m < modList.size(); ++m) {
                StrCopy(buffer, modList[m].name.c_str());
                StrAdd(buffer, ": ");
                StrAdd(buffer, modList[m].active ? "  Active" : "Inactive");
                AddTextMenuEntry(gameMenu[1], buffer);
            }

            gameMenu[1].alignment      = 1;
            gameMenu[1].selectionCount = 3;
            gameMenu[1].selection1     = 0;
            if (gameMenu[1].rowCount > 18)
                gameMenu[1].visibleRowCount = 18;
            else
                gameMenu[1].visibleRowCount = 0;

            gameMenu[0].alignment        = 2;
            gameMenu[0].selectionCount   = 1;
            gameMenu[1].timer            = 0;
            gameMenu[1].visibleRowOffset = 0;
            break;
#endif
        }
    }
}
