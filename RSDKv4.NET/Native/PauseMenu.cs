using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RSDKv4.Utility;

using static RSDKv4.Native.NativeGlobals;
using static RSDKv4.Native.NativeRenderer;
using static RSDKv4.Font;

namespace RSDKv4.Native;

public class PauseMenu : NativeEntity
{
    public static class PMB { public const int CONTINUE = 0, RESTART = 1, SETTINGS = 2, EXIT = 3, DEVMENU = 4, COUNT = 5; };
    public enum STATE { SETUP, ENTER, MAIN, CONTINUE, ACTION, ENTERSUBMENU, SUBMENU, EXITSUBMENU, RESTART, EXIT, DEVMENU };

    public STATE state;
    public float timer;
    public float unused1;
    public RetroGameLoop retroGameLoop;
    public SettingsScreen settingsScreen;
    public TextLabel label;
    public DialogPanel dialog;
    public FadeScreen devMenuFade;
    public SubMenuButton[] buttons = new SubMenuButton[PMB.COUNT];
    public float renderRot;
    public float renderRotMax;
    public float rotInc;
    public Matrix matrixTemp;
    public Matrix matrix;
    public int buttonSelected;
    public float[] buttonRot = new float[PMB.COUNT];
    public float[] rotMax = new float[PMB.COUNT];
    public float[] buttonRotY = new float[PMB.COUNT];
    public int unused2;
    public float buttonX;
    public float matrixX;
    public float width;
    public float matrixY;
    public float matrixZ;
    public float rotationY;
    public float rotYOff;
    public int textureCircle;
    public int textureDPad;
    public float dpadX;
    public float dpadXSpecial;
    public float dpadY;
    public int unusedAlpha;
    public bool makeSound;
    public bool miniPauseDisabled;

    private static int pauseMenuButtonCount;

    public override void Create()
    {
        // code has been here from TitleScreen_Create due to the possibility of opening the dev menu before this loads :(
#if !RETRO_USE_ORIGINAL_CODE
        int heading = -1, labelTex = -1, textTex = -1;

        if (fontList[FONT.HEADING].count <= 2)
        {
            if (Engine.useHighResAssets)
                heading = LoadTexture("Data/Game/Menu/Heading_EN.png", TEXFMT.RGBA4444);
            else
                heading = LoadTexture("Data/Game/Menu/Heading_EN@1x.png", TEXFMT.RGBA4444);
            LoadBitmapFont("Data/Game/Menu/Heading_EN.fnt", FONT.HEADING, heading);
        }

        if (fontList[FONT.LABEL].count <= 2)
        {
            if (Engine.useHighResAssets)
                labelTex = LoadTexture("Data/Game/Menu/Label_EN.png", TEXFMT.RGBA4444);
            else
                labelTex = LoadTexture("Data/Game/Menu/Label_EN@1x.png", TEXFMT.RGBA4444);
            LoadBitmapFont("Data/Game/Menu/Label_EN.fnt", FONT.LABEL, labelTex);
        }

        if (fontList[FONT.TEXT].count <= 2)
        {
            textTex = LoadTexture("Data/Game/Menu/Text_EN.png", TEXFMT.RGBA4444);
            LoadBitmapFont("Data/Game/Menu/Text_EN.fnt", FONT.TEXT, textTex);
        }

        switch (Engine.language)
        {
            case LANGUAGE.JP:
                if (heading >= 0)
                {
                    heading = LoadTexture("Data/Game/Menu/Heading_JA@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Heading_JA.fnt", FONT.HEADING, heading);
                }

                if (labelTex >= 0)
                {
                    labelTex = LoadTexture("Data/Game/Menu/Label_JA@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Label_JA.fnt", FONT.LABEL, labelTex);
                }

                if (textTex >= 0)
                {
                    textTex = LoadTexture("Data/Game/Menu/Text_JA@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Text_JA.fnt", FONT.TEXT, textTex);
                }
                break;
            case LANGUAGE.RU:
                if (heading >= 0)
                {
                    if (Engine.useHighResAssets)
                        heading = LoadTexture("Data/Game/Menu/Heading_RU.png", TEXFMT.RGBA4444);
                    else
                        heading = LoadTexture("Data/Game/Menu/Heading_RU@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Heading_RU.fnt", FONT.HEADING, heading);
                }

                if (labelTex >= 0)
                {
                    if (Engine.useHighResAssets)
                        labelTex = LoadTexture("Data/Game/Menu/Label_RU.png", TEXFMT.RGBA4444);
                    else
                        labelTex = LoadTexture("Data/Game/Menu/Label_RU@1x.png", TEXFMT.RGBA4444);
                }
                break;
            case LANGUAGE.KO:
                if (heading >= 0)
                {
                    heading = LoadTexture("Data/Game/Menu/Heading_KO@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Heading_KO.fnt", FONT.HEADING, heading);
                }

                if (labelTex >= 0)
                {
                    labelTex = LoadTexture("Data/Game/Menu/Label_KO@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Label_KO.fnt", FONT.LABEL, labelTex);
                }

                if (textTex >= 0)
                {
                    textTex = LoadTexture("Data/Game/Menu/Text_KO.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Text_KO.fnt", FONT.TEXT, textTex);
                }
                break;
            case LANGUAGE.ZH:
                if (heading >= 0)
                {
                    heading = LoadTexture("Data/Game/Menu/Heading_ZH@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Heading_ZH.fnt", FONT.HEADING, heading);
                }

                if (labelTex >= 0)
                {
                    labelTex = LoadTexture("Data/Game/Menu/Label_ZH@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Label_ZH.fnt", FONT.LABEL, labelTex);
                }

                if (textTex >= 0)
                {
                    textTex = LoadTexture("Data/Game/Menu/Text_ZH@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Text_ZH.fnt", FONT.TEXT, textTex);
                }
                break;
            case LANGUAGE.ZS:
                if (heading >= 0)
                {
                    heading = LoadTexture("Data/Game/Menu/Heading_ZHS@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Heading_ZHS.fnt", FONT.HEADING, heading);
                }

                if (labelTex >= 0)
                {
                    labelTex = LoadTexture("Data/Game/Menu/Label_ZHS@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Label_ZHS.fnt", FONT.LABEL, labelTex);
                }

                if (textTex >= 0)
                {
                    textTex = LoadTexture("Data/Game/Menu/Text_ZHS@1x.png", TEXFMT.RGBA4444);
                    LoadBitmapFont("Data/Game/Menu/Text_ZHS.fnt", FONT.TEXT, textTex);
                }
                break;
            default: break;
        }
#endif
        pauseMenuButtonCount = PMB.COUNT;
        if (PMB.COUNT == 5 && !Engine.devMenu)
            pauseMenuButtonCount--;

        //this.retroGameLoop = (RetroGameLoop)Objects.GetNativeObject(0);
        this.label = Objects.CreateNativeObject(() => new TextLabel());
        this.label.state = TextLabel.STATE.IDLE;
        this.label.z = 0.0f;
        this.label.scale = 0.2f;
        this.label.alpha = 0;
        this.label.fontId = FONT.HEADING;
        this.label.text = GetCharactersForString(Strings.strPause, FONT.HEADING);
        this.label.alignOffset = 512.0f;
        this.renderRot = MathHelper.ToRadians(22.5f);
        this.label.renderMatrix = Helpers.CreateRotationY(MathHelper.ToRadians(22.5f))
            * Matrix.CreateTranslation(-128.0f, 80.0f, 160.0f);
        this.label.useRenderMatrix = true;

        this.buttonX = ((SCREEN_CENTERX_F + -160.0f) * -0.5f) + -128.0f;
        for (int i = 0; i < pauseMenuButtonCount; ++i)
        {
            SubMenuButton button = Objects.CreateNativeObject(() => new SubMenuButton());
            this.buttons[i] = button;
            this.buttonRot[i] = MathHelper.ToRadians(16.0f);
            button.scale = 0.1f;
            button.matZ = 0.0f;
            button.matXOff = 512.0f;
            button.textY = -4.0f;
            button.matrix = Helpers.CreateRotationY(MathHelper.ToRadians(16.0f))
                * Matrix.CreateTranslation(this.buttonX, 48.0f - i * 30, 160.0f);
            button.symbol = 1;
            button.useMatrix = true;
        }
        if ((Engine.GetGlobalVariableByName("player.lives") <= 1 && Engine.GetGlobalVariableByName("options.gameMode") <= 1) || Scene.activeStageList == 0
            || Engine.GetGlobalVariableByName("options.attractMode") == 1 || Engine.GetGlobalVariableByName("options.vsMode") == 1)
        {
            this.buttons[PMB.RESTART].r = 0x80;
            this.buttons[PMB.RESTART].g = 0x80;
            this.buttons[PMB.RESTART].b = 0x80;
        }
        this.buttons[PMB.CONTINUE].text = GetCharactersForString(Strings.strContinue, FONT.LABEL);
        this.buttons[PMB.RESTART].text = GetCharactersForString(Strings.strRestart, FONT.LABEL);
        this.buttons[PMB.SETTINGS].text = GetCharactersForString(Strings.strSettings, FONT.LABEL);
        this.buttons[PMB.EXIT].text = GetCharactersForString(Strings.strExit, FONT.LABEL);
        if (pauseMenuButtonCount == 5)
            this.buttons[PMB.DEVMENU].text = GetCharactersForString(Strings.strDevMenu, FONT.LABEL);
        this.textureCircle = LoadTexture("Data/Game/Menu/Circle.png", TEXFMT.RGBA4444);
        this.rotationY = 0.0f;
        this.rotYOff = MathHelper.ToRadians(-16.0f);
        this.matrixX = 0.0f;
        this.matrixY = 0.0f;
        this.matrixZ = 160.0f;
        this.width = (1.75f * SCREEN_CENTERX_F) - ((SCREEN_CENTERX_F - 160) * 2);
        if (Engine.deviceType == DEVICE.MOBILE)
            this.textureDPad = LoadTexture("Data/Game/Menu/VirtualDPad.png", TEXFMT.RGBA8888);
        this.dpadY = 104.0f;
        this.state = STATE.ENTER;
        this.miniPauseDisabled = true;
        this.dpadX = SCREEN_CENTERX_F - 76.0f;
        this.dpadXSpecial = SCREEN_CENTERX_F - 52.0f;
    }

    public override void Main()
    {
        switch (this.state)
        {
            case STATE.SETUP:
                {
                    this.timer += Engine.deltaTime;
                    if (this.timer > 1.0f)
                    {
                        this.timer = 0.0f;
                        this.state = STATE.ENTER;
                    }
                    break;
                }
            case STATE.ENTER:
                {
                    if (this.unusedAlpha < 0x100)
                        this.unusedAlpha += 8;
                    this.timer += Engine.deltaTime * 2;
                    this.label.alignOffset = this.label.alignOffset / (1.125f * (60.0f * Engine.deltaTime));
                    this.label.alpha = (int)(256.0f * this.timer);
                    for (int i = 0; i < pauseMenuButtonCount; ++i)
                        this.buttons[i].matXOff += (-176.0f - this.buttons[i].matXOff) / (16.0f * (60.0f * Engine.deltaTime));
                    this.matrixX += (this.width - this.matrixX) / (60.0f * Engine.deltaTime * 12.0f);
                    this.matrixZ += (512.0f - this.matrixZ) / (60.0f * Engine.deltaTime * 12.0f);
                    this.rotationY += (this.rotYOff - this.rotationY) / (16.0f * (60.0f * Engine.deltaTime));
                    if (this.timer > 1.0f)
                    {
                        this.timer = 0.0f;
                        this.state = STATE.MAIN;
                    }
                    break;
                }
            case STATE.MAIN:
                {
                    Input.CheckKeyDown(ref Input.keyDown);
                    Input.CheckKeyPress(ref Input.keyPress);
                    if (usePhysicalControls)
                    {
                        if (Input.touches > 0)
                        {
                            usePhysicalControls = false;
                        }
                        else
                        {
                            if (Input.keyPress.up)
                            {
                                Audio.PlaySfxByName("Menu Move", false);
                                this.buttonSelected--;
                                if (this.buttonSelected < PMB.CONTINUE)
                                    this.buttonSelected = pauseMenuButtonCount - 1;
                            }
                            else if (Input.keyPress.down)
                            {
                                Audio.PlaySfxByName("Menu Move", false);
                                this.buttonSelected++;
                                if (this.buttonSelected >= pauseMenuButtonCount)
                                    this.buttonSelected = PMB.CONTINUE;
                            }
                            for (int i = 0; i < pauseMenuButtonCount; ++i) this.buttons[i].b = this.buttons[i].r;
                            this.buttons[this.buttonSelected].b = 0;
                            if (this.buttons[this.buttonSelected].g > 0x80 && (Input.keyPress.start || Input.keyPress.A))
                            {
                                Audio.PlaySfxByName("Menu Select", false);
                                this.buttons[this.buttonSelected].state = SubMenuButton.STATE.FLASHING2;
                                this.buttons[this.buttonSelected].b = 0xFF;
                                this.state = STATE.ACTION;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < pauseMenuButtonCount; ++i)
                        {
                            if (Input.touches > 0)
                            {
                                if (this.buttons[i].g > 0x80)
                                    this.buttons[i].b = (byte)(((Input.CheckTouchRect(-80.0f, 48.0f - i * 30.0f, 112.0f, 12.0f) > 0) ? 1 : 0) * 0xFF);
                                else
                                    this.buttons[i].b = 0x80;
                            }
                            else if (this.buttons[i].b == 0)
                            {
                                this.buttonSelected = i;
                                Audio.PlaySfxByName("Menu Select", false);
                                this.buttons[i].state = SubMenuButton.STATE.FLASHING2;
                                this.buttons[i].b = 0xFF;
                                this.state = STATE.ACTION;
                                break;
                            }
                        }

                        if (this.state == STATE.MAIN && (Input.keyDown.up || Input.keyDown.down))
                        {
                            this.buttonSelected = PMB.CONTINUE;
                            usePhysicalControls = true;
                        }
                    }
                    if (Input.touches > 0)
                    {
                        if (!this.miniPauseDisabled && Input.CheckTouchRect(SCREEN_CENTERX_F, SCREEN_CENTERY_F, 112.0f, 24.0f) >= 0)
                        {
                            this.buttonSelected = PMB.CONTINUE;
                            Audio.PlaySfxByName("Resume", false);
                            this.state = STATE.ACTION;
                        }
                    }
                    else
                    {
                        this.miniPauseDisabled = false;
                        if (this.makeSound)
                        {
                            Audio.PlaySfxByName("Menu Select", false);
                            this.makeSound = false;
                        }
                    }
                    break;
                }
            case STATE.CONTINUE:
                {
                    this.label.alignOffset += 10.0f * (60.0f * Engine.deltaTime);
                    this.timer += Engine.deltaTime * 2;
                    for (int i = 0; i < pauseMenuButtonCount; ++i)
                        this.buttons[i].matXOff += (12.0f + i) * (60.0f * Engine.deltaTime);
                    this.matrixX += (-this.matrixX) / (5.0f * (60.0f * Engine.deltaTime));
                    this.matrixZ += (160.0f - this.matrixZ) / (5.0f * (60.0f * Engine.deltaTime));
                    this.rotationY += (this.rotYOff - this.rotationY) / (60.0f * Engine.deltaTime * 6.0f);

                    if (this.timer > 0.9)
                    {
                        //mixFiltersOnJekyll = true;
                        RenderRetroBuffer(64, 160.0f);
                        if (Engine.deviceType == DEVICE.MOBILE)
                        {
                            if (Scene.activeStageList == STAGELIST.SPECIAL)
                                RenderImage(this.dpadXSpecial, this.dpadY, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 160.0f, 258.0f, 255, this.textureDPad);
                            else
                                RenderImage(this.dpadX, this.dpadY, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 160.0f, 258.0f, 255, this.textureDPad);
                        }
                        this.timer = 0.0f;
                        Objects.ClearNativeObjects();
                        Objects.CreateNativeObject(() => new RetroGameLoop());
                        if (Engine.deviceType == DEVICE.MOBILE)
                            Objects.CreateNativeObject(() => new VirtualDPad()).pauseAlpha = 255;
                        Audio.ResumeSound();
                        Engine.engineState = ENGINE_STATE.MAINGAME;
                        return;
                    }
                    break;
                }
            case STATE.ACTION:
                {
                    if (this.buttons[this.buttonSelected].state == SubMenuButton.STATE.IDLE)
                    {
                        switch (this.buttonSelected)
                        {
                            case PMB.CONTINUE:
                                this.state = STATE.CONTINUE;
                                this.rotYOff = 0.0f;
                                break;
                            case PMB.RESTART:
                                this.dialog = Objects.CreateNativeObject(() => new DialogPanel());
                                this.dialog.text =
                                    GetCharactersForString(Engine.GetGlobalVariableByName("options.gameMode") != 0 ? Strings.strRestartMessage : Strings.strNSRestartMessage, FONT.TEXT);
                                this.state = STATE.RESTART;
                                break;
                            case PMB.SETTINGS:
                                this.state = STATE.ENTERSUBMENU;
                                this.rotInc = 0.0f;
                                this.renderRotMax = MathHelper.ToRadians(-90.0f);
                                for (int i = 0; i < pauseMenuButtonCount; ++i)
                                {
                                    this.rotMax[i] = MathHelper.ToRadians(-90.0f);
                                    this.buttonRotY[i] = 0.02f * (i + 1);
                                }

                                break;
                            case PMB.EXIT:
                                this.dialog = Objects.CreateNativeObject(() => new DialogPanel());
                                this.dialog.text =
                                    GetCharactersForString(Engine.GetGlobalVariableByName("options.gameMode") != 0 ? Strings.strExitMessage : Strings.strNSExitMessage, FONT.TEXT);
                                this.state = STATE.EXIT;
                                if (Engine.gameType == GAME.SONIC1)
                                    Engine.SetGlobalVariableByName("timeAttack.result", 1000000);
                                break;
#if !RETRO_USE_ORIGINAL_CODE
                            case PMB.DEVMENU:
                                this.state = STATE.DEVMENU;
                                this.timer = 0.0f;
                                break;
#endif
                            default: break;
                        }
                    }
                    break;
                }
            case STATE.ENTERSUBMENU:
                {
                    if (this.renderRot > this.renderRotMax)
                    {
                        this.rotInc -= 0.0025f * (Engine.deltaTime * 60.0f);
                        this.renderRot += Engine.deltaTime * 60.0f * this.rotInc;
                        this.rotInc -= 0.0025f * (Engine.deltaTime * 60.0f);
                        this.label.renderMatrix = Helpers.CreateRotationY(this.renderRot)
                            * Matrix.CreateTranslation(this.buttonX, 80.0f, 160.0f);
                    }
                    for (int i = 0; i < pauseMenuButtonCount; ++i)
                    {
                        if (this.buttonRot[i] > this.rotMax[i])
                        {
                            this.buttonRotY[i] -= 0.0025f * (60.0f * Engine.deltaTime);
                            if (this.buttonRotY[i] < 0.0)
                            {
                                this.buttonRot[i] += 60.0f * Engine.deltaTime * this.buttonRotY[i];
                            }
                            this.buttonRotY[i] -= 0.0025f * (60.0f * Engine.deltaTime); // do it again ????
                            this.label.renderMatrix = Helpers.CreateRotationY(this.buttonRot[i])
                                * Matrix.CreateTranslation(this.buttonX, 48.0f - i * 30, 160.0f);
                        }
                    }
                    if (this.rotMax[pauseMenuButtonCount - 1] >= this.buttonRot[pauseMenuButtonCount - 1])
                    {
                        this.state = STATE.SUBMENU;

                        this.rotInc = 0.0f;
                        this.renderRotMax = MathHelper.ToRadians(22.5f);
                        for (int i = 0; i < pauseMenuButtonCount; ++i)
                        {
                            this.rotMax[i] = MathHelper.ToRadians(16.0f);
                            this.buttonRotY[i] = -0.02f * (i + 1);
                        }
                        if (this.buttonSelected == 2)
                        {
                            //this.settingsScreen = Objects.CreateNativeObject(() => new SettingsScreen());
                            //this.settingsScreen.optionsMenu = (NativeEntity_OptionsMenu*)self;
                            //this.settingsScreen.isPauseMenu = 1;
                        }
                    }
                    this.matrixX += (1024.0f - this.matrixX) / (60.0f * Engine.deltaTime * 16.0f);
                    break;
                }
            case STATE.SUBMENU: break;
            case STATE.EXITSUBMENU:
                {
                    if (this.renderRotMax > this.renderRot)
                    {
                        this.rotInc += 0.0025f * (Engine.deltaTime * 60.0f);
                        this.renderRot += Engine.deltaTime * 60.0f * this.rotInc;
                        this.rotInc += 0.0025f * (Engine.deltaTime * 60.0f);
                        this.label.renderMatrix = Helpers.CreateRotationY(this.renderRot)
                            * Matrix.CreateTranslation(this.buttonX, 80.0f, 160.0f);
                    }

                    for (int i = 0; i < pauseMenuButtonCount; ++i)
                    {
                        if (this.rotMax[i] > this.buttonRot[i])
                        {
                            this.buttonRotY[i] += 0.0025f * (60.0f * Engine.deltaTime);
                            if (this.buttonRotY[i] > 0.0f)
                                this.buttonRot[i] += 60.0f * Engine.deltaTime * this.buttonRotY[i];
                            this.buttonRotY[i] += 0.0025f * (60.0f * Engine.deltaTime);
                            if (this.buttonRot[i] > this.rotMax[i])
                                this.buttonRot[i] = this.rotMax[i];

                            this.buttons[i].matrix = Helpers.CreateRotationY(this.buttonRot[i])
                                * Matrix.CreateTranslation(this.buttonX, 48.0f - i * 30, 160.0f);
                        }
                    }

                    float div = 60.0f * Engine.deltaTime * 16.0f;
                    this.matrixX += (this.width - 32.0f - this.matrixX) / div;
                    if (this.width - 16.0 > this.matrixX)
                    {
                        this.matrixX = this.width - 16.0f;
                        this.state = STATE.MAIN;
                    }
                    break;
                }
            case STATE.RESTART:
                {
                    if (this.dialog.selection == DialogPanel.SELECTION.YES)
                    {
                        this.state = STATE.SUBMENU;
                        Engine.engineState = ENGINE_STATE.EXITPAUSE;
                        Scene.stageMode = STAGEMODE.LOAD;
                        if (Engine.GetGlobalVariableByName("options.gameMode") <= 1)
                        {
                            Engine.SetGlobalVariableByName("player.lives", Engine.GetGlobalVariableByName("player.lives") - 1);
                        }
                        if (Scene.activeStageList != STAGELIST.SPECIAL)
                        {
                            Engine.SetGlobalVariableByName("lampPostID", 0);
                            Engine.SetGlobalVariableByName("starPostID", 0);
                        }
                        this.dialog.state = DialogPanel.STATE.IDLE;
                        Audio.StopMusic(true);
                        Objects.CreateNativeObject(() => new FadeScreen());
                        break;
                    }
                    if (this.dialog.selection == DialogPanel.SELECTION.NO)
                        this.state = STATE.MAIN;
                    break;
                }
            case STATE.EXIT:
                {
                    if (this.dialog.selection == DialogPanel.SELECTION.YES)
                    {
                        this.state = STATE.SUBMENU;
                        if (Engine.skipStartMenu)
                        {
                            Engine.engineState = ENGINE_STATE.MAINGAME;
                            this.dialog.state = DialogPanel.STATE.IDLE;
                            FadeScreen fadeout = Objects.CreateNativeObject(() => new FadeScreen());
                            fadeout.state = FadeScreen.STATE.GAMEFADEOUT;
                            Scene.activeStageList = STAGELIST.PRESENTATION;
                            Scene.stageListPosition = 0;
                            Scene.stageMode = STAGEMODE.LOAD;
                        }
                        else
                        {
                            Engine.engineState = ((Engine.GetGlobalVariableByName("options.gameMode") > 1) ? 1 : 0) + ENGINE_STATE.ENDGAME;
                            this.dialog.state = DialogPanel.STATE.IDLE;
                            //CREATE_ENTITY(FadeScreen);
                        }
                    }
                    else
                    {
                        if (this.dialog.selection == DialogPanel.SELECTION.YES || this.dialog.selection == DialogPanel.SELECTION.NO || this.dialog.selection == DialogPanel.SELECTION.OK)
                        {
                            this.state = STATE.MAIN;
                            this.unused2 = 50;
                        }
                    }
                    break;
                }
#if !RETRO_USE_ORIGINAL_CODE
            case STATE.DEVMENU:
                //this.timer += Engine.deltaTime;
                //if (this.timer > 0.5)
                //{
                //    if (!this.devMenuFade)
                //    {
                //        this.devMenuFade = CREATE_ENTITY(FadeScreen);
                //        this.devMenuFade.state = FADESCREEN_STATE_FADEOUT;
                //    }
                //    if (!this.devMenuFade.delay || this.devMenuFade.timer >= this.devMenuFade.delay)
                //    {
                //        ClearNativeObjects();
                //        RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, 255);
                //        CREATE_ENTITY(RetroGameLoop);
                //        if (Engine.gameDeviceType == RETRO_MOBILE)
                //            CREATE_ENTITY(VirtualDPad);
                //        Engine.gameMode = ENGINE_INITDEVMENU;
                //        return;
                //    }
                //}
                break;
#endif
            default: break;
        }

        SetRenderBlendMode(RENDER_BLEND.NONE);
        NewRenderState();

        this.matrix = Helpers.CreateRotationY(this.rotationY)
            * Matrix.CreateTranslation(this.matrixX, this.matrixY, this.matrixZ);

        SetRenderMatrix(this.matrix);
        RenderRect(-SCREEN_CENTERX_F - 4.0f, SCREEN_CENTERY_F + 4.0f, 0.0f, SCREEN_XSIZE_F + 8.0f, SCREEN_YSIZE_F + 8.0f, 0, 0, 0, 255);
        RenderRetroBuffer(64, 0.0f);
        NewRenderState();
        SetRenderMatrix(null);
        if (Engine.deviceType == DEVICE.MOBILE && this.state != STATE.SUBMENU)
        {
            if (Scene.activeStageList == STAGELIST.SPECIAL)
                RenderImage(this.dpadXSpecial, this.dpadY, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 160.0f, 258.0f, 255, this.textureDPad);
            else
                RenderImage(this.dpadX, this.dpadY, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 160.0f, 258.0f, 255, this.textureDPad);
        }
        SetRenderBlendMode(RENDER_BLEND.ALPHA);
        NewRenderState();
    }
}
