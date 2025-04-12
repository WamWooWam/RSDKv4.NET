using RSDKv4.Native;
using RSDKv4.Render;

using static RSDKv4.Drawing;

namespace RSDKv4;

public class DevMenu
{
    private bool endLine = true;
    private int touchFlags = 0;
    private int taListStore = 0;

    private Audio Audio;
    private Palette Palette;
    private Drawing Drawing;
    private Animation Animation;
    private Engine Engine;
    private Objects Objects;
    private Input Input;
    private Text Text;
    private Scene Scene;

    public void Initialize(Engine engine)
    {
        Audio = engine.Audio;
        Palette = engine.Palette; 
        Drawing = engine.Drawing;
        Animation = engine.Animation;
        Engine = engine;
        Objects = engine.Objects;
        Input = engine.Input;
        Text = engine.Text;
        Scene = engine.Scene;        
    }

    public void InitDevMenu()
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
//#if RETRO_HARDWARE_RENDER
        //Drawing.render3DEnabled = false;
        (Drawing as HardwareDrawing).UpdateSurfaces();
//#endif
    }

    public void InitErrorMessage()
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

    public void ProcessStageSelect()
    {
        Drawing.BeginDraw();
        Drawing.EnsureBlendMode(BlendMode.None);

        Drawing.ClearScreen(0xF0);

        Input.CheckKeyDown(ref Input.keyDown);
        Input.CheckKeyPress(ref Input.keyPress);

        //#if defined RETRO_USING_MOUSE || defined RETRO_USING_TOUCH
        Drawing.DrawSprite(32, 0x42, 16, 16, 78, 240, TEXTMENU_SURFACE);
        Drawing.DrawSprite(32, 0xB2, 16, 16, 95, 240, TEXTMENU_SURFACE);
        Drawing.DrawSprite(SCREEN_XSIZE - 32, SCREEN_YSIZE - 32, 16, 16, 112, 240, TEXTMENU_SURFACE);
        //#endif

        if (!Input.keyDown.start && !Input.keyDown.up && !Input.keyDown.down)
        {
            int tFlags = touchFlags;
            touchFlags = 0;

            for (int t = 0; t < Input.touches; ++t)
            {
                if (Input.touchDown[t] != 0)
                {
                    if (Input.touchX[t] < SCREEN_CENTERX)
                    {
                        if (Input.touchY[t] >= SCREEN_CENTERY)
                        {
                            if ((tFlags & 2) == 0)
                                Input.keyPress.down = true;
                            else
                                touchFlags |= 1 << 1;
                        }
                        else
                        {
                            if ((tFlags & 1) == 0)
                                Input.keyPress.up = true;
                            else
                                touchFlags |= 1 << 0;
                        }
                    }
                    else if (Input.touchX[t] > SCREEN_CENTERX)
                    {
                        if (Input.touchY[t] > SCREEN_CENTERY)
                        {
                            if ((tFlags & 4) == 0)
                                Input.keyPress.start = true;
                            else
                                touchFlags |= 1 << 2;
                        }
                        else
                        {
                            if ((tFlags & 8) == 0)
                                Input.keyPress.B = true;
                            else
                                touchFlags |= 1 << 3;
                        }
                    }
                }
            }

            touchFlags |= (int)(Input.keyPress.up ? 1 : 0) << 0;
            touchFlags |= (int)(Input.keyPress.down ? 1 : 0) << 1;
            touchFlags |= (int)(Input.keyPress.start ? 1 : 0) << 2;
            touchFlags |= (int)(Input.keyPress.B ? 1 : 0) << 3;
        }

        switch (Scene.stageMode)
        {
            case DEVMENU.MAIN: // Main Menu
                {
                    if (Input.keyPress.down)
                        Text.gameMenu[0].selection2 += 2;

                    if (Input.keyPress.up)
                        Text.gameMenu[0].selection2 -= 2;

                    int count = 15;
#if RETRO_USE_MOD_LOADER
            count += 2;
#endif

                    if (Text.gameMenu[0].selection2 > count)
                        Text.gameMenu[0].selection2 = 9;
                    if (Text.gameMenu[0].selection2 < 9)
                        Text.gameMenu[0].selection2 = count;

                    Drawing.DrawTextMenu(Text.gameMenu[0], SCREEN_CENTERX, 72);
                    if (Input.keyPress.start || Input.keyPress.A)
                    {
                        if (Text.gameMenu[0].selection2 == 9)
                        {
                            Drawing.ClearGraphicsData();
                            Animation.ClearAnimationData();
                            Scene.activeStageList = 0;
                            Scene.stageMode = STAGEMODE.LOAD;
                            Engine.engineState = ENGINE_STATE.MAINGAME;
                            Scene.stageListPosition = 0;
                        }
                        else if (Text.gameMenu[0].selection2 == 11)
                        {
                            Text.SetupTextMenu(Text.gameMenu[0], 0);
                            Text.AddTextMenuEntry(Text.gameMenu[0], "SELECT A PLAYER");
                            Text.SetupTextMenu(Text.gameMenu[1], 0);
                            Text.LoadConfigListText(Text.gameMenu[1], 0);
                            Text.gameMenu[1].alignment = 0;
                            Text.gameMenu[1].selectionCount = 1;
                            Text.gameMenu[1].selection1 = 0;
                            Scene.stageMode = DEVMENU.PLAYERSEL;
                        }
                        else if (Text.gameMenu[0].selection2 == 13)
                        {
                            Objects.ClearNativeObjects();
                            Engine.engineState = ENGINE_STATE.WAIT;
                            Engine.nativeMenuFadeIn = false;
                            if (Engine.skipStartMenu)
                            {
                                Drawing.ClearGraphicsData();
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
                        else if (Text.gameMenu[0].selection2 == 15) {
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
                    else if (Input.keyPress.B)
                    {
                        Drawing.ClearGraphicsData();
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
                    if (Input.keyPress.down)
                        ++Text.gameMenu[1].selection1;
                    if (Input.keyPress.up)
                        --Text.gameMenu[1].selection1;
                    if (Text.gameMenu[1].selection1 == Text.gameMenu[1].rowCount)
                        Text.gameMenu[1].selection1 = 0;

                    if (Text.gameMenu[1].selection1 < 0)
                        Text.gameMenu[1].selection1 = Text.gameMenu[1].rowCount - 1;

                    Drawing.DrawTextMenu(Text.gameMenu[0], SCREEN_CENTERX - 4, 72);
                    Drawing.DrawTextMenu(Text.gameMenu[1], SCREEN_CENTERX - 40, 96);
                    if (Input.keyPress.start || Input.keyPress.A)
                    {
                        Objects.playerListPos = Text.gameMenu[1].selection1;
                        SetTextMenu(DEVMENU.STAGELISTSEL);
                    }
                    else if (Input.keyPress.B)
                    {
                        SetTextMenu(DEVMENU.MAIN);
                    }
                    break;
                }
            case DEVMENU.STAGELISTSEL: // Selecting Category
                {
                    if (Input.keyPress.down)
                        Text.gameMenu[0].selection2 += 2;
                    if (Input.keyPress.up)
                        Text.gameMenu[0].selection2 -= 2;

                    if (Text.gameMenu[0].selection2 > 9)
                        Text.gameMenu[0].selection2 = 3;

                    if (Text.gameMenu[0].selection2 < 3)
                        Text.gameMenu[0].selection2 = 9;

                    Drawing.DrawTextMenu(Text.gameMenu[0], SCREEN_CENTERX - 80, 72);
                    bool nextMenu = false;
                    switch (Text.gameMenu[0].selection2)
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

                    if ((Input.keyPress.start || Input.keyPress.A) && nextMenu)
                    {
                        Text.SetupTextMenu(Text.gameMenu[0], 0);
                        Text.AddTextMenuEntry(Text.gameMenu[0], "SELECT A STAGE");
                        Text.SetupTextMenu(Text.gameMenu[1], 0);
                        Text.LoadConfigListText(Text.gameMenu[1], ((Text.gameMenu[0].selection2 - 3) >> 1) + 1);
                        Text.gameMenu[1].alignment = 1;
                        Text.gameMenu[1].selectionCount = 3;
                        Text.gameMenu[1].selection1 = 0;
                        if (Text.gameMenu[1].rowCount > 18)
                            Text.gameMenu[1].visibleRowCount = 18;
                        else
                            Text.gameMenu[1].visibleRowCount = 0;

                        Text.gameMenu[0].alignment = 2;
                        Text.gameMenu[0].selectionCount = 1;
                        Text.gameMenu[1].timer = 0;
                        Scene.stageMode = DEVMENU.STAGESEL;
                    }
                    else if (Input.keyPress.B)
                    {
                        Text.SetupTextMenu(Text.gameMenu[0], 0);
                        Text.AddTextMenuEntry(Text.gameMenu[0], "SELECT A PLAYER");
                        Text.SetupTextMenu(Text.gameMenu[1], 0);
                        Text.LoadConfigListText(Text.gameMenu[1], 0);
                        Text.gameMenu[0].alignment = 2;
                        Text.gameMenu[1].alignment = 0;
                        Text.gameMenu[1].selectionCount = 1;
                        Text.gameMenu[1].visibleRowCount = 0;
                        Text.gameMenu[1].visibleRowOffset = 0;
                        Text.gameMenu[1].selection1 = Objects.playerListPos;
                        Scene.stageMode = DEVMENU.PLAYERSEL;
                    }
                    break;
                }
            case DEVMENU.STAGESEL: // Selecting Stage
                {
                    if (Input.keyDown.down)
                    {
                        Text.gameMenu[1].timer += 1;
                        if (Text.gameMenu[1].timer > 8)
                        {
                            Text.gameMenu[1].timer = 0;
                            Input.keyPress.down = true;
                        }
                    }
                    else
                    {
                        if (Input.keyDown.up)
                        {
                            Text.gameMenu[1].timer -= 1;
                            if (Text.gameMenu[1].timer < -8)
                            {
                                Text.gameMenu[1].timer = 0;
                                Input.keyPress.up = true;
                            }
                        }
                        else
                        {
                            Text.gameMenu[1].timer = 0;
                        }
                    }
                    if (Input.keyPress.down)
                    {
                        Text.gameMenu[1].selection1++;
                        if (Text.gameMenu[1].selection1 - Text.gameMenu[1].visibleRowOffset >= Text.gameMenu[1].visibleRowCount)
                        {
                            Text.gameMenu[1].visibleRowOffset += 1;
                        }
                    }
                    if (Input.keyPress.up)
                    {
                        Text.gameMenu[1].selection1--;
                        if (Text.gameMenu[1].selection1 - Text.gameMenu[1].visibleRowOffset < 0)
                        {
                            Text.gameMenu[1].visibleRowOffset -= 1;
                        }
                    }
                    if (Text.gameMenu[1].selection1 == Text.gameMenu[1].rowCount)
                    {
                        Text.gameMenu[1].selection1 = 0;
                        Text.gameMenu[1].visibleRowOffset = 0;
                    }
                    if (Text.gameMenu[1].selection1 < 0)
                    {
                        Text.gameMenu[1].selection1 = Text.gameMenu[1].rowCount - 1;
                        Text.gameMenu[1].visibleRowOffset = (ushort)(Text.gameMenu[1].rowCount - Text.gameMenu[1].visibleRowCount);
                    }

                    Drawing.DrawTextMenu(Text.gameMenu[0], SCREEN_CENTERX - 4, 40);
                    Drawing.DrawTextMenu(Text.gameMenu[1], SCREEN_CENTERX + 100, 64);
                    if (Input.keyPress.start || Input.keyPress.A)
                    {
                        Scene.debugMode = Input.keyDown.A;
                        Scene.stageMode = STAGEMODE.LOAD;
                        Engine.engineState = ENGINE_STATE.MAINGAME;
                        Scene.stageListPosition = Text.gameMenu[1].selection1;
                        Engine.SetGlobalVariableByName("options.gameMode", 0);
                        Engine.SetGlobalVariableByName("lampPostID", 0); // For S1
                        Engine.SetGlobalVariableByName("starPostID", 0); // For S2
                    }
                    else if (Input.keyPress.B)
                    {
                        SetTextMenu(DEVMENU.STAGELISTSEL);
                    }
                    break;
                }
            case DEVMENU.SCRIPTERROR: // Script Error
                {
                    Drawing.DrawTextMenu(Text.gameMenu[0], SCREEN_CENTERX, 72);
                    if (Input.keyPress.start || Input.keyPress.A)
                    {
                        SetTextMenu(DEVMENU.MAIN);
                    }
                    else if (Input.keyPress.B)
                    {
                        Drawing.ClearGraphicsData();
                        Animation.ClearAnimationData();
                        Scene.activeStageList = 0;
                        Scene.stageMode = DEVMENU.STAGESEL;
                        Engine.engineState = ENGINE_STATE.MAINGAME;
                        Scene.stageListPosition = 0;
                    }
                    else if (Input.keyPress.C)
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
            int preOption = Text.gameMenu[1].selection1;
            if (Input.keyDown.down) {
                Text.gameMenu[1].timer++;
                if (Text.gameMenu[1].timer > 8) {
                    Text.gameMenu[1].timer = 0;
                    Input.keyPress.down   = true;
                }
            }
            else {
                if (Input.keyDown.up) {
                    Text.gameMenu[1].timer--;
                    if (Text.gameMenu[1].timer < -8) {
                        Text.gameMenu[1].timer = 0;
                        Input.keyPress.up     = true;
                    }
                }
                else {
                    Text.gameMenu[1].timer = 0;
                }
            }

            if (Input.keyPress.down) {
                Text.gameMenu[1].selection1++;
                if (Text.gameMenu[1].selection1 - Text.gameMenu[1].visibleRowOffset >= Text.gameMenu[1].visibleRowCount) {
                    Text.gameMenu[1].visibleRowOffset++;
                }
            }

            if (Input.keyPress.up) {
                Text.gameMenu[1].selection1--;
                if (Text.gameMenu[1].selection1 - Text.gameMenu[1].visibleRowOffset < 0 && Text.gameMenu[1].visibleRowOffset > 0) {
                    Text.gameMenu[1].visibleRowOffset--;
                }
            }

            if (Text.gameMenu[1].selection1 >= Text.gameMenu[1].rowCount) {
                if (Input.keyDown.C) {
                    Text.gameMenu[1].selection1--;
                    Text.gameMenu[1].visibleRowOffset--;
                }
                else {
                    Text.gameMenu[1].selection1       = 0;
                    Text.gameMenu[1].visibleRowOffset = 0;
                }
            }

            if (Text.gameMenu[1].selection1 < 0) {
                if (Input.keyDown.C) {
                    Text.gameMenu[1].selection1++;
                }
                else {
                    Text.gameMenu[1].selection1       = Text.gameMenu[1].rowCount - 1;
                    Text.gameMenu[1].visibleRowOffset = Text.gameMenu[1].rowCount - Text.gameMenu[1].visibleRowCount;
                }
            }
            Text.gameMenu[1].selection2 = Text.gameMenu[1].selection1; // its a bug fix LOL

            char buffer[0x100];
            if (Text.gameMenu[1].selection1 < modList.size() && (Input.keyPress.A || Input.keyPress.start || Input.keyPress.left || Input.keyPress.right)) {
                modList[Text.gameMenu[1].selection1].active ^= 1;
                StrCopy(buffer, modList[Text.gameMenu[1].selection1].name.c_str());
                StrAdd(buffer, ": ");
                StrAdd(buffer, (modList[Text.gameMenu[1].selection1].active ? "  Active" : "Inactive"));
                EditTextMenuEntry(Text.gameMenu[1], buffer, Text.gameMenu[1].selection1);
            }
            else if (Input.keyDown.C && Text.gameMenu[1].selection1 != preOption) {
                int visibleOffset  = Text.gameMenu[1].visibleRowOffset;
                int option         = Text.gameMenu[1].selection1;
                ModInfo swap       = modList[preOption];
                modList[preOption] = modList[option];
                modList[option]    = swap;
                SetTextMenu(DEVMENU_MODMENU);
                Text.gameMenu[1].selection1       = option;
                Text.gameMenu[1].visibleRowOffset = visibleOffset;
            }
            else if (Input.keyPress.B) {
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

            DrawTextMenu(Text.gameMenu[0], SCREEN_CENTERX - 4, 40);
            DrawTextMenu(Text.gameMenu[1], SCREEN_CENTERX + 100, 64);
            break;
        }
#endif
            default: break;
        }

        Drawing.EndDraw();
    }

    public void SetTextMenu(int sm)
    {
        Scene.stageMode = sm;
        Text.SetupTextMenu(Text.gameMenu[0], 0);
        Text.SetupTextMenu(Text.gameMenu[1], 0);
        switch (sm)
        {
            case DEVMENU.MAIN:
                {
                    Text.AddTextMenuEntry(Text.gameMenu[0], "RETRO ENGINE DEV MENU");
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], Engine.gameWindowText + " Version");
                    Text.AddTextMenuEntry(Text.gameMenu[0], Engine.gameVersion);
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], "START GAME");
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], "STAGE SELECT");
#if !RETRO_USE_ORIGINAL_CODE
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], "START MENU");
#if RETRO_USE_MOD_LOADER
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], "MODS");
#endif
                    Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                    Text.AddTextMenuEntry(Text.gameMenu[0], "EXIT GAME");
#endif
                    Text.gameMenu[0].alignment = 2;
                    Text.gameMenu[0].selectionCount = 2;
                    Text.gameMenu[0].selection1 = 0;
                    Text.gameMenu[0].selection2 = 9;
                    Text.gameMenu[1].visibleRowCount = 0;
                    Text.gameMenu[1].visibleRowOffset = 0;
                    break;
                }
            case DEVMENU.STAGELISTSEL:
                Text.AddTextMenuEntry(Text.gameMenu[0], "SELECT A STAGE LIST");
                Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                Text.AddTextMenuEntry(Text.gameMenu[0], "   PRESENTATION");
                Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                Text.AddTextMenuEntry(Text.gameMenu[0], "   REGULAR");
                Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                Text.AddTextMenuEntry(Text.gameMenu[0], "   SPECIAL");
                Text.AddTextMenuEntry(Text.gameMenu[0], " ");
                Text.AddTextMenuEntry(Text.gameMenu[0], "   BONUS");
                Text.gameMenu[0].alignment = 0;
                Text.gameMenu[0].selection2 = 3;
                Text.gameMenu[0].selectionCount = 2;
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
