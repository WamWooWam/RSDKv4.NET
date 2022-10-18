using System;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class MenuControl : NativeEntity
{
    public enum STATE { MAIN, ACTION, NONE, ENTERSUBMENU, SUBMENU, EXITSUBMENU, DIALOGWAIT }

    public enum STATEINPUT { CHECKTOUCH, HANDLEDRAG, HANDLEMOVEMENT, MOVE, HANDLERELEASE }

    public enum BUTTON { STARTGAME = 1, TIMEATTACK, MULTIPLAYER, ACHIEVEMENTS, LEADERBOARDS, OPTIONS }

    public STATE state;
    public float timer;
    public float float18;
    public float float1C;
    public float float20;
    public float float24;
    public float float28;
    public float float2C;
    public float float30;
    public int buttonCount;
    public MenuButton[] buttons = new MenuButton[8];
    public BackButton backButton;
    public BUTTON[] buttonFlags = new BUTTON[8];
    public byte buttonID;
    public STATEINPUT stateInput;
    public float field_6C;
    public float field_70;
    public float field_74;
    public float field_78;
    public float touchX2;
    public SegaIDButton segaIDButton;
    public int field_84;
    public DialogPanel dialog;
    public int dialogTimer;

    public override void Create()
    {
        Audio.SetMusicTrack("MainMenu.ogg", 0, true, 106596);
        Objects.CreateNativeObject(() => new MenuBG());

        this.buttons[this.buttonCount] = Objects.CreateNativeObject(() => new StartGameButton());
        this.buttonFlags[this.buttonCount] = BUTTON.STARTGAME;
        this.buttonCount++;

        this.buttons[this.buttonCount] = Objects.CreateNativeObject(() => new TimeAttackButton());
        this.buttonFlags[this.buttonCount] = BUTTON.TIMEATTACK;
        this.buttonCount++;

#if RETRO_USE_MOD_LOADER
    int vsID = GetSceneID(STAGELIST_PRESENTATION, "2P VS");
    if (vsID != -1) {
#else
        if (Engine.gameType == GAME.SONIC2)
        {
#endif
            this.buttons[this.buttonCount] = Objects.CreateNativeObject(() => new MultiplayerButton());
            this.buttonFlags[this.buttonCount] = BUTTON.MULTIPLAYER;
            this.buttonCount++;
        }

        if (Engine.onlineActive)
        {
            this.buttons[this.buttonCount] = Objects.CreateNativeObject(() => new AchievementsButton());
            this.buttonFlags[this.buttonCount] = BUTTON.ACHIEVEMENTS;
            this.buttonCount++;

            this.buttons[this.buttonCount] = Objects.CreateNativeObject(() => new LeaderboardsButton());
            this.buttonFlags[this.buttonCount] = BUTTON.LEADERBOARDS;
            this.buttonCount++;
        }

        this.buttons[this.buttonCount] = Objects.CreateNativeObject(() => new OptionsButton());
        this.buttonFlags[this.buttonCount] = BUTTON.OPTIONS;
        this.buttonCount++;

        this.backButton = Objects.CreateNativeObject(() => new BackButton());
        this.backButton.visible = false;
        this.backButton.x = 240.0f;
        this.backButton.y = -160.0f;
        this.backButton.z = 0.0f;

        this.segaIDButton = Objects.CreateNativeObject(() => new SegaIDButton());
        this.segaIDButton.y = -92.0f;
        this.segaIDButton.texX = 0.0f;
        this.segaIDButton.x = SCREEN_CENTERX_F - 32.0f;

        this.float28 = 0.15707964f;  // this but less precise --. M_PI / 2
        this.float2C = 0.078539819f; // this but less precise --. M_PI / 4
        this.float30 = (this.buttonCount * this.float28) * 0.5f;

        float offset = 0.0f;
        for (int b = 0; b < this.buttonCount; ++b)
        {
            MenuButton button = this.buttons[b];
            float sin = (float)Math.Sin(this.float18 + offset);
            float cos = (float)Math.Cos(this.float18 + offset);
            button.x = 1024.0f * sin;
            button.z = (cos * -512.0f) + 672.0f;
            button.y = (128.0f * sin) + 16.0f;
            button.visible = button.z <= 288.0f;
            offset += this.float28;
        }

        Audio.PlayMusic(0, 0);
        if (Engine.deviceType == DEVICE.STANDARD)
            NativeGlobals.usePhysicalControls = true;
        // Objects.BackupNativeObjects();
    }

    public override void Main()
    {
        SegaIDButton segaIDButton = this.segaIDButton;
        BackButton backButton = this.backButton;

        switch (this.state)
        {
            case STATE.MAIN:
                {
                    Input.CheckKeyDown(ref Input.inputDown);
                    Input.CheckKeyPress(ref Input.inputPress);

                    if (segaIDButton.alpha < 0x100 && Engine.language != LANGUAGE.JP && !(Engine.language == LANGUAGE.ZH || Engine.language == LANGUAGE.ZS)
                        && Engine.deviceType == DEVICE.MOBILE)
                        segaIDButton.alpha += 8;

                    if (!NativeGlobals.usePhysicalControls)
                    {
                        switch (this.stateInput)
                        {
                            case STATEINPUT.CHECKTOUCH:
                                {
                                    if (Input.touches > 0)
                                    {
                                        if (Input.inputDown.left == 0 && Input.inputDown.right == 0)
                                        {
                                            segaIDButton.state = SegaIDButton.STATE.IDLE;
                                            if (Input.CheckTouchRect(0.0f, 16.0f, 56.0f, 56.0f) >= 0)
                                            {
                                                //BackupNativeObjects();
                                                this.touchX2 = Input.touchXF[0];
                                                this.stateInput = STATEINPUT.HANDLERELEASE;
                                                this.buttonID = (byte)Math.Ceiling(this.float18 / -this.float2C);
                                                this.buttons[this.buttonID].g = 0xC0;
                                            }
                                            else
                                            {
                                                if (Input.CheckTouchRect(this.segaIDButton.x, this.segaIDButton.y, 20.0f, 20.0f) >= 0
                                                    && segaIDButton.alpha > 64)
                                                {
                                                    segaIDButton.state = SegaIDButton.STATE.PRESSED;
                                                }
                                                else
                                                {
                                                    this.stateInput = STATEINPUT.HANDLEDRAG;
                                                    this.field_74 = 0.0f;
                                                    this.field_6C = Input.touchXF[0];
                                                    this.float20 = this.float18;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            segaIDButton.state = SegaIDButton.STATE.IDLE;
                                            NativeGlobals.usePhysicalControls = true;
                                            this.buttonID = (byte)Math.Ceiling(this.float18 / -this.float2C);
                                            this.buttons[this.buttonID].g = 0xC0;
                                        }
                                    }
                                    else if (segaIDButton.state == SegaIDButton.STATE.PRESSED)
                                    {
                                        segaIDButton.state = SegaIDButton.STATE.IDLE;
                                        Audio.PlaySfxByName("Menu Select", false);
                                        //ShowPromoPopup(0, "MoreGames");
                                    }
                                    else if (Input.inputDown.left != 0 || Input.inputDown.right != 0)
                                    {
                                        segaIDButton.state = SegaIDButton.STATE.IDLE;
                                        NativeGlobals.usePhysicalControls = true;
                                        this.buttonID = (byte)Math.Ceiling(this.float18 / -this.float2C);
                                        this.buttons[this.buttonID].g = 0xC0;
                                    }
                                    break;
                                }
                            case STATEINPUT.HANDLEDRAG:
                                {
                                    if (Input.touches <= 0)
                                    {
                                        this.stateInput = STATEINPUT.HANDLEMOVEMENT;
                                    }
                                    else
                                    {
                                        this.field_70 = 0.0f;
                                        this.field_78 = (this.field_6C - Input.touchXF[0]) * -0.0007f;
                                        if (Math.Abs(this.field_74) > 0.0f)
                                        {
                                            this.field_70 = this.field_78 - this.field_74;
                                            this.float18 += this.field_70;
                                        }
                                        this.field_74 = this.field_78;
                                    }
                                    break;
                                }
                            case STATEINPUT.HANDLEMOVEMENT:
                                {
                                    this.field_70 /= (1.125f * (60.0f * Engine.deltaTime));
                                    this.float18 += this.field_70;
                                    float max = -(this.float30 - this.float2C);

                                    if (max - 0.05f > this.float18 || this.float18 > 0.05f)
                                    {
                                        this.field_70 = 0.0f;
                                    }

                                    if (Math.Abs(this.field_70) < 0.0025f)
                                    {
                                        if (this.float18 == this.float20 && this.field_6C < 0.0f)
                                        {
                                            this.float18 += 0.00001f;
                                        }

                                        if (this.float18 <= this.float20)
                                        {
                                            this.float1C = (float)Math.Floor(this.float18 / this.float2C) * this.float2C;

                                            if (this.float1C > this.float20 - this.float2C)
                                                this.float1C = this.float20 - this.float2C;

                                            if (this.float1C < max)
                                                this.float1C = max;
                                        }
                                        else
                                        {
                                            this.float1C = (float)Math.Ceiling(this.float18 / this.float2C) * this.float2C;

                                            if (this.float1C < this.float2C + this.float20)
                                                this.float1C = this.float2C + this.float20;

                                            if (this.float1C > 0.0)
                                                this.float1C = 0.0f;
                                        }

                                        this.stateInput = STATEINPUT.MOVE;
                                        this.float18 += (this.float1C - this.float18) / ((60.0f * Engine.deltaTime) * 8.0f);
                                    }
                                    break;
                                }
                            case STATEINPUT.MOVE:
                                {
                                    if (Input.touches > 0)
                                    {
                                        this.stateInput = STATEINPUT.HANDLEDRAG;
                                        this.field_74 = 0.0f;
                                        this.field_6C = Input.touchXF[0];
                                    }
                                    else
                                    {
                                        this.float18 += (this.float1C - this.float18) / ((60.0f * Engine.deltaTime) * 6.0f);
                                        if (Math.Abs(this.float1C - this.float18) < 0.00025f)
                                        {
                                            this.float18 = this.float1C;
                                            this.stateInput = STATEINPUT.CHECKTOUCH;
                                        }
                                    }
                                    break;
                                }
                            case STATEINPUT.HANDLERELEASE:
                                {
                                    if (Input.touches > 0)
                                    {
                                        if (Input.CheckTouchRect(0.0f, 16.0f, 56.0f, 56.0f) < 0)
                                        {
                                            this.buttons[this.buttonID].g = 0xFF;
                                        }
                                        else
                                        {
                                            this.buttons[this.buttonID].g = 0xC0;
                                            if (Math.Abs(this.touchX2 - Input.touchXF[0]) > 8.0f)
                                            {
                                                this.stateInput = STATEINPUT.HANDLEDRAG;
                                                this.field_6C = this.buttonID;
                                                this.field_74 = 0.0f;
                                                this.float20 = this.float18;
                                                this.buttons[this.buttonID].g = 0xFF;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (this.buttons[this.buttonID].g == 0xC0)
                                        {
                                            this.buttons[this.buttonID].label.state = TextLabel.STATE.BLINK_FAST;
                                            this.timer = 0.0f;
                                            this.state = STATE.ACTION;
                                            Audio.PlaySfxByName("Menu Select", false);
                                        }
                                        this.buttons[this.buttonID].g = 0xFF;
                                        this.stateInput = STATEINPUT.CHECKTOUCH;
                                    }
                                    break;
                                }
                            default: break;
                        }
                    }
                    else
                    {
                        if (this.stateInput == STATEINPUT.HANDLEDRAG)
                        {
                            this.float18 += (((this.float24 + this.float1C) - this.float18) / ((60.0f * Engine.deltaTime) * 8.0f));

                            if (Math.Abs(this.float1C - this.float18) < 0.001f)
                            {
                                this.float18 = this.float1C;
                                this.stateInput = STATEINPUT.CHECKTOUCH;
                            }
                        }
                        else
                        {
                            if (Input.touches <= 0)
                            {
                                if (Input.inputPress.right != 0 && this.float18 > -(this.float30 - this.float2C))
                                {
                                    this.stateInput = STATEINPUT.HANDLEDRAG;
                                    this.float1C -= this.float2C;
                                    Audio.PlaySfxByName("Menu Move", false);
                                    this.float24 = -0.01f;
                                    this.buttonID++;
                                    if (this.buttonID >= this.buttonCount)
                                        this.buttonID = (byte)(this.buttonCount - 1);
                                }
                                else if (Input.inputPress.left != 0 && this.float18 < 0.0)
                                {
                                    this.stateInput = STATEINPUT.HANDLEDRAG;
                                    this.float1C += this.float2C;
                                    Audio.PlaySfxByName("Menu Move", false);
                                    this.float24 = 0.01f;
                                    this.buttonID--;
                                    if (this.buttonID > this.buttonCount)
                                        this.buttonID = 0;
                                }
                                else if ((Input.inputPress.start != 0 || Input.inputPress.A != 0) && !NativeGlobals.nativeMenuFadeIn)
                                {
                                    //BackupNativeObjects();
                                    this.buttons[this.buttonID].label.state = TextLabel.STATE.BLINK_FAST;
                                    this.timer = 0.0f;
                                    this.state = STATE.ACTION;
                                    Audio.PlaySfxByName("Menu Select", false);
                                }

                                for (int i = 0; i < this.buttonCount; ++i)
                                {
                                    this.buttons[i].g = 0xFF;
                                }
                                this.buttons[this.buttonID].g = 0xC0;
                            }
                            else
                            {
                                NativeGlobals.usePhysicalControls = false;
                                for (int i = 0; i < this.buttonCount; ++i)
                                {
                                    this.buttons[i].g = 0xFF;
                                }
                            }
                        }
                    }

                    float offset = this.float18;
                    for (int i = 0; i < this.buttonCount; ++i)
                    {
                        MenuButton button = this.buttons[i];
                        button.x = 1024.0f * (float)Math.Sin(this.float18 + offset);
                        button.y = ((float)Math.Sin(this.float18 + offset) * 128.0f) + 16.0f;
                        button.z = ((float)Math.Cos(this.float18 + offset) * -512.0f) + 672.0f;
                        button.visible = button.z <= 288.0f;
                        offset += this.float28;
                    }

                    if (this.stateInput != STATEINPUT.CHECKTOUCH)
                    {
                        if (this.dialogTimer != 0)
                        {
                            this.dialogTimer--;
                        }
                        else if (Input.inputPress.B != 0 && !NativeGlobals.nativeMenuFadeIn)
                        {
                            this.dialog = Objects.CreateNativeObject(() => new DialogPanel());
                            this.dialog.text = Font.GetCharactersForString(Strings.strExitGame, FONT.TEXT);
                            this.state = STATE.DIALOGWAIT;
                            Audio.PlaySfxByName("Resume", false);
                        }
                    }
                    break;
                }
            case STATE.ACTION:
                {
                    this.timer += Engine.deltaTime;
                    if (this.timer > 0.5f)
                    {
                        this.timer = 0.0f;
                        MenuButton button = this.buttons[this.buttonID];
                        switch (this.buttonFlags[this.buttonID])
                        {
                            case BUTTON.STARTGAME:
                                this.state = STATE.ENTERSUBMENU;
                                this.field_70 = 0.0f;
                                button.g = 0xFF;
                                this.buttons[this.buttonID].label.state = TextLabel.STATE.NONE;
                                this.backButton.visible = true;
                                Engine.SetGlobalVariableByName("options.vsMode", 0);
                                //CREATE_ENTITY(SaveSelect);
                                break;
                            case BUTTON.TIMEATTACK:
                                this.state = STATE.ENTERSUBMENU;
                                this.field_70 = 0.0f;
                                button.g = 0xFF;
                                button.label.state = TextLabel.STATE.NONE;
                                this.backButton.visible = true;
                                //CREATE_ENTITY(TimeAttack);
                                break;
                            case BUTTON.MULTIPLAYER:
                                this.state = STATE.MAIN;
                                button.label.state = TextLabel.STATE.IDLE;
                                Engine.SetGlobalVariableByName("options.saveSlot", 0);
                                Engine.SetGlobalVariableByName("options.gameMode", 0);
                                Engine.SetGlobalVariableByName("options.vsMode", 0);
                                Engine.SetGlobalVariableByName("player.lives", 3);
                                Engine.SetGlobalVariableByName("player.score", 0);
                                Engine.SetGlobalVariableByName("player.scoreBonus", 50000);
                                Engine.SetGlobalVariableByName("specialStage.listPos", 0);
                                Engine.SetGlobalVariableByName("specialStage.emeralds", 0);
                                Engine.SetGlobalVariableByName("specialStage.nextZone", 0);
                                Engine.SetGlobalVariableByName("timeAttack.result", 0);
                                Engine.SetGlobalVariableByName("lampPostID", 0);
                                Engine.SetGlobalVariableByName("starPostID", 0);
                                if (Engine.onlineActive)
                                {
                                    Scene.InitStartingStage(STAGELIST.PRESENTATION, 3, 0);
                                    //CREATE_ENTITY(FadeScreen);
                                }
                                else
                                {
                                    this.dialog = Objects.CreateNativeObject(() => new DialogPanel());
                                    this.dialog.buttonCount = (int)DialogPanel.TYPE.OK;
                                    this.dialog.text = Font.GetCharactersForString(Strings.strNetworkMessage, FONT.TEXT);
                                    this.state = STATE.DIALOGWAIT;
                                }
                                break;
                            case BUTTON.ACHIEVEMENTS:
                                if (Engine.onlineActive && false)
                                {
                                    //ShowAchievementsScreen();
                                }
                                else
                                {
                                    this.state = STATE.MAIN;
                                    this.dialog = Objects.CreateNativeObject(() => new DialogPanel());
                                    this.dialog.buttonCount = (int)DialogPanel.TYPE.OK;
                                    this.dialog.text = Font.GetCharactersForString(Strings.strNetworkMessage, FONT.TEXT);
                                    this.state = STATE.DIALOGWAIT;
                                }
                                button.label.state = TextLabel.STATE.IDLE;
                                break;
                            case BUTTON.LEADERBOARDS:
                                this.state = STATE.MAIN;
                                if (Engine.onlineActive && false)
                                {
                                    //ShowLeaderboardsScreen();
                                }
                                else
                                {
                                    this.dialog = Objects.CreateNativeObject(() => new DialogPanel());
                                    this.dialog.buttonCount = (int)DialogPanel.TYPE.OK;
                                    this.dialog.text = Font.GetCharactersForString(Strings.strNetworkMessage, FONT.TEXT);
                                    this.state = STATE.DIALOGWAIT;
                                }
                                button.label.state = TextLabel.STATE.IDLE;
                                break;
                            case BUTTON.OPTIONS:
                                this.state = STATE.ENTERSUBMENU;
                                this.field_70 = 0.0f;
                                button.g = 0xFF;
                                button.label.state = TextLabel.STATE.NONE;
                                this.backButton.visible = true;
                                //CREATE_ENTITY(OptionsMenu);
                                break;
                            default:
                                this.state = STATE.MAIN;
                                button.label.state = TextLabel.STATE.IDLE;
                                break;
                        }
                    }
                    break;
                }
            case STATE.NONE: break;
            case STATE.ENTERSUBMENU:
                {
                    if (segaIDButton.alpha > 0)
                        segaIDButton.alpha -= 8;

                    this.field_70 -= 0.125f * (60.0f * Engine.deltaTime);

                    for (int i = 0; i < this.buttonCount; ++i)
                    {
                        if (this.buttonID != i)
                        {
                            if (this.buttonID != i)
                                this.buttons[i].z += ((60.0f * Engine.deltaTime) * this.field_70);
                        }
                    }

                    this.timer += Engine.deltaTime;
                    this.field_70 -= 0.125f * (60.0f * Engine.deltaTime);

                    if (this.timer > 0.5)
                    {
                        var button = this.buttons[this.buttonID];
                        float div = (60.0f * Engine.deltaTime) * 16.0f;

                        button.x += ((112.0f - button.x) / div);
                        button.y += ((64.0f - button.y) / div);
                        button.z += ((200.0f - button.z) / div);
                        this.backButton.z += ((320.0f - this.backButton.z) / div);
                    }

                    if (this.timer > 1.5f)
                    {
                        this.timer = 0.0f;
                        this.state = STATE.SUBMENU;

                        for (int i = 0; i < this.buttonCount; ++i)
                        {
                            if (this.buttonID != i)
                            {
                                if (this.buttonID != i)
                                    this.buttons[i].visible = false;
                            }
                        }
                    }
                    break;
                }
            case STATE.SUBMENU:
                {
                    Input.CheckKeyDown(ref Input.inputDown);
                    Input.CheckKeyPress(ref Input.inputPress);
                    if (Input.touches <= 0)
                    {
                        if (this.backButton.g == 0xC0)
                        {
                            Audio.PlaySfxByName("Menu Back", false);
                            this.backButton.g = 0xFF;
                            this.state = STATE.EXITSUBMENU;
                        }
                    }
                    else
                    {
                        backButton = this.backButton;
                        if (Input.CheckTouchRect(122.0f, -80.0f, 32.0f, 32.0f) < 0)
                            backButton.g = 0xFF;
                        else
                            backButton.g = 0xC0;
                    }
                    if (Input.inputPress.B != 0)
                    {
                        Audio.PlaySfxByName("Menu Back", false);
                        this.backButton.g = 0xFF;
                        this.state = STATE.EXITSUBMENU;
                    }
                    break;
                }
            case STATE.EXITSUBMENU:
                {
                    this.backButton.z = ((0.0f - this.backButton.z) / (16.0f * (60.0f * Engine.deltaTime))) + this.backButton.z;
                    this.timer += Engine.deltaTime;
                    if (this.timer > 0.25f)
                    {
                        float offset = this.float18;
                        float div = (60.0f * Engine.deltaTime) * 8.0f;

                        for (int i = 0; i < this.buttonCount; ++i)
                        {
                            if (this.buttonID != i)
                            {
                                MenuButton button = this.buttons[i];
                                button.z = (((((float)Math.Cos(offset + this.float18) * -512.0f) + 672.0f) - button.z) / div) + button.z;
                                button.visible = true;
                            }
                            offset += this.float28;
                        }

                        MenuButton curButton = this.buttons[this.buttonID];
                        curButton.label.state = TextLabel.STATE.IDLE;
                        curButton.x += ((0.0f - curButton.x) / div);
                        curButton.y += ((16.0f - curButton.y) / div);
                        curButton.z += ((160.0f - curButton.z) / div);
                    }

                    if (this.timer > 1.0)
                    {
                        this.timer = 0.0f;
                        this.field_70 = 0.0f;
                        this.state = STATE.MAIN;
                    }
                    break;
                }
            case STATE.DIALOGWAIT:
                {
                    if (this.dialog.selection == DialogPanel.SELECTION.NO || this.dialog.selection == DialogPanel.SELECTION.OK)
                    {
                        this.state = STATE.MAIN;
                        this.dialogTimer = 50;
                    }
                    else if (this.dialog.selection == DialogPanel.SELECTION.YES)
                    {
                        //ExitGame();
                        this.dialogTimer = 50;
                        this.state = STATE.MAIN;
                    }
                    break;
                }
            default: break;
        }
    }
}
