using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using RSDKv4.Utility;

using static RSDKv4.Native.NativeGlobals;
using static RSDKv4.Native.NativeRenderer;
using static RSDKv4.Font;

namespace RSDKv4.Native;

public class SaveSelect : NativeEntity
{
    public enum SAVESEL { NONE, SONIC, ST, TAILS, KNUX };
    public enum STATE { SETUP, ENTER, MAIN, EXIT, LOADSAVE, ENTERSUBMENU, SUBMENU, EXITSUBMENU, MAIN_DELETING, DELSETUP, DIALOGWAIT };
    public static class BUTTON { public const int NOSAVE = 0, SAVE1 = 1, SAVE2 = 2, SAVE3 = 3, SAVE4 = 4, COUNT = 5; };

    public STATE state;
    public float timer;
    public int unused1;
    public MenuControl menuControl;
    public object playerSelect;
    public TextLabel labelPtr;
    public float deleteRotateY;
    public float targetDeleteRotateY;
    public float deleteRotateYVelocity;
    public Matrix matrix1;
    public SubMenuButton[] saveButtons = new SubMenuButton[(int)BUTTON.COUNT];
    public PushButton delButton;
    public DialogPanel dialog;
    public bool deleteEnabled;
    public int selectedButton;
    public float[] rotateY = new float[(int)BUTTON.COUNT];
    public float[] targetRotateY = new float[(int)BUTTON.COUNT];
    public float[] rotateYVelocity = new float[(int)BUTTON.COUNT];

    public override void Create()
    {
        var saveGame = SaveData.saveGame;
        this.menuControl = (MenuControl)Objects.GetNativeObject(0);
        this.labelPtr = Objects.CreateNativeObject(() => new TextLabel());
        this.labelPtr.fontId = FONT.HEADING;
        if (Engine.language != LANGUAGE.EN)
            this.labelPtr.scale = 0.12f;
        else
            this.labelPtr.scale = 0.2f;
        this.labelPtr.alpha = 0;
        this.labelPtr.z = 0;
        this.labelPtr.state = TextLabel.STATE.IDLE;
        this.labelPtr.text = Font.GetCharactersForString(Strings.strSaveSelect, FONT.HEADING);
        this.labelPtr.alignOffset = 512.0f;

        this.deleteRotateY = MathHelper.ToRadians(22.5f);
        this.labelPtr.renderMatrix = Helpers.CreateRotationY(this.deleteRotateY)
            * Matrix.CreateTranslation(-128.0f, 80.0f, 160.0f);

        this.labelPtr.useRenderMatrix = true;

        this.delButton = Objects.CreateNativeObject(() => new PushButton());
        this.delButton.x = 384.0f;
        this.delButton.y = -16.0f;
        if (Engine.language == LANGUAGE.FR)
            this.delButton.scale = 0.15f;
        else
            this.delButton.scale = 0.2f;

        this.delButton.bgColour = 0x00A048;
        this.delButton.text = Font.GetCharactersForString(Strings.strDelete, FONT.LABEL);

        this.saveButtons[BUTTON.NOSAVE] = Objects.CreateNativeObject(() => new SubMenuButton());
        this.saveButtons[BUTTON.NOSAVE].text = Font.GetCharactersForString(Strings.strNoSave, FONT.LABEL);
        this.saveButtons[BUTTON.NOSAVE].matXOff = 512.0f;
        this.saveButtons[BUTTON.NOSAVE].textY = -4.0f;
        this.saveButtons[BUTTON.NOSAVE].matZ = 0.0f;
        this.saveButtons[BUTTON.NOSAVE].scale = 0.1f;

        this.rotateY[BUTTON.NOSAVE] = MathHelper.ToRadians(16.0f);
        this.saveButtons[BUTTON.NOSAVE].matrix = Helpers.CreateRotationY(this.rotateY[0])
            * Matrix.CreateTranslation(-128.0f, 48.0f, 160.0f);
        this.saveButtons[BUTTON.NOSAVE].useMatrix = true;
        SaveData.ReadSaveRAMData();

        float y = 18.0f;
        for (int i = BUTTON.SAVE1; i < BUTTON.COUNT; ++i)
        {
            this.saveButtons[i] = Objects.CreateNativeObject(() => new SubMenuButton());

            int stagePos = saveGame.files[i - 1].stageId;
            if (stagePos >= 0x80)
            {
                this.saveButtons[i].text = Font.GetCharactersForString(Strings.strSaveStageList[saveGame.files[i - 1].specialStageId + 19], FONT.LABEL);
                this.saveButtons[i].state = SubMenuButton.STATE.SAVEBUTTON_SELECTED;
                this.saveButtons[i].textY = 2.0f;
                this.saveButtons[i].scale = 0.08f;
                this.deleteEnabled = true;
            }
            else if (stagePos > 0)
            {
                if (stagePos - 1 > 18 && Engine.gameType == GAME.SONIC1)
                    this.saveButtons[i].text = Font.GetCharactersForString(Strings.strSaveStageList[25], FONT.LABEL);
                else
                    this.saveButtons[i].text = Font.GetCharactersForString(Strings.strSaveStageList[stagePos - 1], FONT.LABEL);
                this.saveButtons[i].state = SubMenuButton.STATE.SAVEBUTTON_SELECTED;
                this.saveButtons[i].textY = 2.0f;
                this.saveButtons[i].scale = 0.08f;
                this.deleteEnabled = true;
            }
            else
            {
                this.saveButtons[i].text = Font.GetCharactersForString(Strings.strNewGame, FONT.LABEL);
                this.saveButtons[i].textY = -4.0f;
                this.saveButtons[i].scale = 0.1f;
            }

            this.saveButtons[i].matXOff = 512.0f;
            this.saveButtons[i].matZ = 0.0f;
            this.saveButtons[i].symbol = (byte)saveGame.files[i - 1].characterId;
            this.saveButtons[i].flags = (byte)saveGame.files[i - 1].emeralds;
            this.rotateY[i] = MathHelper.ToRadians(16.0f);
            this.saveButtons[i].matrix = Helpers.CreateRotationY(this.rotateY[i])
                * Matrix.CreateTranslation(-128.0f, y, 160.0f);
            this.saveButtons[i].useMatrix = true;
            y -= 30.0f;
        }
    }

    public override void Main()
    {
        var saveGame = SaveData.saveGame;

        switch (this.state)
        {
            case STATE.SETUP:
                {
                    this.timer += Engine.deltaTime;
                    if (this.timer > 1.0)
                    {
                        this.timer = 0.0f;
                        this.state = STATE.ENTER;
                    }
                    break;
                }
            case STATE.ENTER:
                {
                    this.labelPtr.alignOffset = this.labelPtr.alignOffset / (1.125f * (60.0f * Engine.deltaTime));
                    if (this.deleteEnabled)
                        this.delButton.x = ((92.0f - this.delButton.x) / (8.0f * (60.0f * Engine.deltaTime))) + this.delButton.x;

                    float div = 60.0f * Engine.deltaTime * 16.0f;
                    for (int i = 0; i < BUTTON.COUNT; ++i)
                        this.saveButtons[i].matXOff += (-176.0f - this.saveButtons[i].matXOff) / div;

                    this.timer += Engine.deltaTime + Engine.deltaTime;
                    this.labelPtr.alpha = (int)(256.0f * this.timer);
                    if (this.timer > 1.0)
                    {
                        this.timer = 0.0f;
                        this.state = STATE.MAIN;
                        Input.keyPress.start = false;
                        Input.keyPress.A = false;
                    }
                    break;
                }
            case STATE.MAIN:
            case STATE.MAIN_DELETING:
                {
                    if (this.state != STATE.MAIN_DELETING)
                    {
                        if (!this.deleteEnabled)
                            this.delButton.x += (512.0f - this.delButton.x) / (60.0f * Engine.deltaTime * 16.0f);
                        else
                            this.delButton.x += (92.0f - this.delButton.x) / (60.0f * Engine.deltaTime * 16.0f);
                    }

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
                                this.selectedButton--;
                                if (this.deleteEnabled && this.selectedButton < BUTTON.NOSAVE)
                                {
                                    this.selectedButton = BUTTON.COUNT;
                                }
                                else if (this.selectedButton < BUTTON.NOSAVE)
                                {
                                    this.selectedButton = BUTTON.COUNT - 1;
                                }
                            }
                            else if (Input.keyPress.down)
                            {
                                Audio.PlaySfxByName("Menu Move", false);
                                this.selectedButton++;
                                if (this.deleteEnabled && this.selectedButton > BUTTON.COUNT)
                                {
                                    this.selectedButton = BUTTON.NOSAVE;
                                }
                                else if (this.selectedButton >= BUTTON.COUNT)
                                {
                                    this.selectedButton = BUTTON.NOSAVE;
                                }
                            }

                            for (int i = 0; i < BUTTON.COUNT; ++i) this.saveButtons[i].b = 0xFF;

                            if (this.deleteEnabled && (Input.keyPress.left || Input.keyPress.right))
                            {
                                if (this.selectedButton < BUTTON.COUNT)
                                {
                                    this.selectedButton = BUTTON.COUNT;
                                    this.delButton.state = PushButton.STATE.SELECTED;
                                }
                                else
                                {
                                    this.selectedButton = BUTTON.NOSAVE;
                                    this.saveButtons[this.selectedButton].b = 0x00;
                                    this.delButton.state = PushButton.STATE.UNSELECTED;
                                }
                            }
                            else
                            {
                                if (this.selectedButton >= BUTTON.COUNT)
                                {
                                    this.delButton.state = PushButton.STATE.SELECTED;
                                }
                                else
                                {
                                    this.saveButtons[this.selectedButton].b = 0x00;
                                    this.delButton.state = PushButton.STATE.UNSELECTED;
                                }
                            }

                            if (Input.keyPress.start || Input.keyPress.A)
                            {
                                if (this.selectedButton < BUTTON.COUNT)
                                {
                                    if (this.state == STATE.MAIN_DELETING)
                                    {
                                        if (this.selectedButton > BUTTON.NOSAVE && saveGame.files[this.selectedButton - 1].stageId > 0)
                                        {
                                            Audio.PlaySfxByName("Menu Select", false);
                                            this.state = STATE.DELSETUP;
                                            this.saveButtons[this.selectedButton].b = 0xFF;
                                            this.saveButtons[this.selectedButton].state = SubMenuButton.STATE.SAVEBUTTON_UNSELECTED;
                                        }
                                    }
                                    else
                                    {
                                        Audio.PlaySfxByName("Menu Select", false);
                                        this.saveButtons[this.selectedButton].state = SubMenuButton.STATE.FLASHING2;
                                        if (this.selectedButton > BUTTON.NOSAVE && saveGame.files[this.selectedButton - 1].stageId > 0)
                                        {
                                            Audio.StopMusic(true);
                                            this.saveButtons[this.selectedButton].state = SubMenuButton.STATE.SAVEBUTTON_UNSELECTED;
                                        }
                                        this.saveButtons[this.selectedButton].b = 0xFF;
                                        this.state = STATE.LOADSAVE;
                                    }
                                }
                                else
                                {
                                    if (Engine.gameType == GAME.SONIC1)
                                        Audio.PlaySfxByName("Lamp Post", false);
                                    else
                                        Audio.PlaySfxByName("Star Post", false);
                                    this.delButton.state = PushButton.STATE.FLASHING;
                                    if (this.state == STATE.MAIN_DELETING)
                                    {
                                        this.state = STATE.MAIN;
                                        Audio.PlaySfxByName("Menu Back", false);
                                        for (int i = BUTTON.SAVE1; i < BUTTON.COUNT; ++i)
                                        {
                                            if (this.saveButtons[i].state == SubMenuButton.STATE.SAVEBUTTON_SELECTED)
                                                this.saveButtons[i].useMeshH = false;
                                        }
                                        this.delButton.state = PushButton.STATE.UNSELECTED;
                                    }
                                    else
                                        this.state = STATE.LOADSAVE;
                                }
                            }
                        }
                    }
                    else
                    {
                        float y = 48.0f;
                        for (int i = 0; i < BUTTON.COUNT; ++i)
                        {
                            if (Input.touches > 0)
                            {
                                if (Input.CheckTouchRect(-64.0f, y, 96.0f, 12.0f) < 0)
                                    this.saveButtons[i].b = 0xFF;
                                else
                                    this.saveButtons[i].b = 0x00;
                            }
                            else if (this.saveButtons[i].b == 0)
                            {
                                this.selectedButton = i;
                                if (this.state == STATE.MAIN_DELETING)
                                {
                                    if (this.selectedButton > BUTTON.NOSAVE && saveGame.files[this.selectedButton - 1].stageId > 0)
                                    {
                                        Audio.PlaySfxByName("Menu Select", false);
                                        this.state = STATE.DELSETUP;
                                        this.saveButtons[this.selectedButton].b = 0xFF;
                                        this.saveButtons[this.selectedButton].state = SubMenuButton.STATE.SAVEBUTTON_UNSELECTED;
                                    }
                                }
                                else
                                {
                                    Audio.PlaySfxByName("Menu Select", false);
                                    this.saveButtons[this.selectedButton].state = SubMenuButton.STATE.FLASHING2;
                                    if (this.selectedButton > BUTTON.NOSAVE && saveGame.files[this.selectedButton - 1].stageId > 0)
                                    {
                                        Audio.StopMusic(true);
                                        this.saveButtons[this.selectedButton].state = SubMenuButton.STATE.SAVEBUTTON_UNSELECTED;
                                    }
                                    this.saveButtons[this.selectedButton].b = 0xFF;
                                    this.state = STATE.LOADSAVE;
                                }

                                break;
                            }
                            y -= 30.0f;
                        }

                        if (this.state == STATE.MAIN)
                        {
                            if (!this.deleteEnabled)
                            {
                                if (Input.keyDown.up || Input.keyDown.down || Input.keyDown.left || Input.keyDown.right)
                                {
                                    this.selectedButton = BUTTON.NOSAVE;
                                    usePhysicalControls = true;
                                }
                            }
                            else
                            {
                                if (Input.touches <= 0)
                                {
                                    if (this.delButton.state == PushButton.STATE.SELECTED)
                                    {
                                        this.selectedButton = BUTTON.COUNT;
                                        if (Engine.gameType == GAME.SONIC1)
                                            Audio.PlaySfxByName("Lamp Post", false);
                                        else
                                            Audio.PlaySfxByName("Star Post", false);
                                        this.delButton.state = PushButton.STATE.FLASHING;
                                        this.state = STATE.LOADSAVE;
                                    }
                                    else
                                    {
                                        if (Input.keyDown.up || Input.keyDown.down || Input.keyDown.left || Input.keyDown.right)
                                        {
                                            this.selectedButton = BUTTON.NOSAVE;
                                            usePhysicalControls = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (Input.CheckTouchRect(this.delButton.x, this.delButton.y, (64.0f * this.delButton.scale) + this.delButton.textWidth, 12.0f) >= 0)
                                    {
                                        this.delButton.state = PushButton.STATE.SELECTED;
                                    }
                                    else
                                    {
                                        this.delButton.state = PushButton.STATE.UNSELECTED;
                                    }
                                    if (this.state == STATE.MAIN)
                                    {
                                        if (Input.keyDown.up || Input.keyDown.down || Input.keyDown.left || Input.keyDown.right)
                                        {
                                            this.selectedButton = BUTTON.NOSAVE;
                                            usePhysicalControls = true;
                                        }
                                    }
                                }
                            }
                        }
                        else if (this.state == STATE.MAIN_DELETING)
                        {
                            if (Input.touches > 0)
                            {
                                if (Input.CheckTouchRect(this.delButton.x, this.delButton.y, (64.0f * this.delButton.scale) + this.delButton.textWidth, 12.0f) >= 0)
                                {
                                    this.delButton.state = PushButton.STATE.SELECTED;
                                }
                                else
                                {
                                    this.delButton.state = PushButton.STATE.UNSELECTED;
                                }
                            }
                            else if (this.delButton.state == PushButton.STATE.SELECTED)
                            {
                                this.state = STATE.MAIN;
                                Audio.PlaySfxByName("Menu Back", false);
                                for (int i = BUTTON.SAVE1; i < BUTTON.COUNT; ++i)
                                {
                                    if (this.saveButtons[i].state == SubMenuButton.STATE.SAVEBUTTON_SELECTED)
                                        this.saveButtons[i].useMeshH = false;
                                }
                                this.delButton.state = PushButton.STATE.UNSELECTED;
                            }
                        }
                    }

                    if (this.menuControl.state == MenuControl.STATE.EXITSUBMENU)
                        this.state = STATE.EXIT;
                    break;
                }
            case STATE.EXIT:
                {
                    this.labelPtr.alignOffset += 10.0f * (60.0f * Engine.deltaTime);
                    this.delButton.x += 10.0f * (60.0f * Engine.deltaTime);
                    for (int i = 0; i < BUTTON.COUNT; ++i) this.saveButtons[i].matXOff += 11.0f * (60.0f * Engine.deltaTime);
                    this.timer += Engine.deltaTime + Engine.deltaTime;
                    if (this.timer > 1.0f)
                    {
                        this.timer = 0.0f;
                        Objects.RemoveNativeObject(this.labelPtr);
                        Objects.RemoveNativeObject(this.delButton);
                        for (int i = 0; i < BUTTON.COUNT; ++i)
                            Objects.RemoveNativeObject(this.saveButtons[i]);
                        Objects.RemoveNativeObject(this);
                    }
                    break;
                }
            case STATE.LOADSAVE:
                {
                    this.menuControl.state = MenuControl.STATE.NONE;
                    if ((this.saveButtons[this.selectedButton].state & ~SubMenuButton.STATE.SAVEBUTTON_SELECTED) == 0)
                    {
                        if (this.selectedButton == BUTTON.COUNT)
                        {
                            this.menuControl.state = MenuControl.STATE.SUBMENU;
                            this.state = STATE.MAIN_DELETING;
                            if (usePhysicalControls)
                                this.selectedButton = BUTTON.SAVE1;
                            for (int i = BUTTON.SAVE1; i < BUTTON.COUNT; ++i)
                            {
                                if (this.saveButtons[i].state == SubMenuButton.STATE.SAVEBUTTON_SELECTED)
                                    this.saveButtons[i].useMeshH = true;
                            }
                        }
                        else if (this.selectedButton != 0)
                        {
                            int saveSlot = this.selectedButton - 1;
                            if (saveGame.files[saveSlot].stageId != 0)
                            {
                                this.state = STATE.SUBMENU;
                                Engine.SetGlobalVariableByName("options.saveSlot", saveSlot);
                                Engine.SetGlobalVariableByName("options.gameMode", 1);
                                Engine.SetGlobalVariableByName("options.stageSelectFlag", 0);
                                Engine.SetGlobalVariableByName("player.lives", saveGame.files[saveSlot].lives);
                                Engine.SetGlobalVariableByName("player.score", saveGame.files[saveSlot].score);
                                Engine.SetGlobalVariableByName("player.scoreBonus", saveGame.files[saveSlot].scoreBonus);
                                Engine.SetGlobalVariableByName("specialStage.listPos", saveGame.files[saveSlot].specialStageId);
                                Engine.SetGlobalVariableByName("specialStage.emeralds", saveGame.files[saveSlot].emeralds);
                                Engine.SetGlobalVariableByName("lampPostID", 0);
                                Engine.SetGlobalVariableByName("starPostID", 0);
                                //Engine.debugMode = false;
                                if (saveGame.files[saveSlot].stageId >= 0x80)
                                {
                                    Engine.SetGlobalVariableByName("specialStage.nextZone", saveGame.files[saveSlot].stageId - 0x81);
                                    Scene.InitStartingStage(STAGELIST.SPECIAL, saveGame.files[saveSlot].specialStageId, saveGame.files[saveSlot].characterId);
                                }
                                else
                                {
                                    Engine.SetGlobalVariableByName("specialStage.nextZone", saveGame.files[saveSlot].stageId - 1);
                                    Scene.InitStartingStage(STAGELIST.REGULAR, saveGame.files[saveSlot].stageId - 1, saveGame.files[saveSlot].characterId);
                                }
                                Objects.CreateNativeObject(() => new FadeScreen());
                            }
                            else
                            {
                                this.state = STATE.ENTERSUBMENU;
                                this.deleteRotateYVelocity = 0.0f;
                                this.targetDeleteRotateY = MathHelper.ToRadians(-90.0f);
                                for (int i = 0; i < BUTTON.COUNT; ++i)
                                    this.targetRotateY[i] = MathHelper.ToRadians(-90.0f);

                                float val = 0.02f;
                                for (int i = 0; i < BUTTON.COUNT; ++i)
                                {
                                    this.rotateYVelocity[i] = val;
                                    val += 0.02f;
                                }
                            }
                        }
                        else
                        {
                            this.state = STATE.ENTERSUBMENU;
                            this.deleteRotateYVelocity = 0.0f;
                            this.targetDeleteRotateY = MathHelper.ToRadians(-90.0f);
                            for (int i = 0; i < BUTTON.COUNT; ++i)
                                this.targetRotateY[i] = MathHelper.ToRadians(-90.0f);

                            float val = 0.02f;
                            for (int i = 0; i < BUTTON.COUNT; ++i)
                            {
                                this.rotateYVelocity[i] = val;
                                val += 0.02f;
                            }
                        }
                    }
                    break;
                }
            case STATE.ENTERSUBMENU:
                {
                    if (this.deleteRotateY > this.targetDeleteRotateY)
                    {
                        this.deleteRotateYVelocity -= 0.0025f * (Engine.deltaTime * 60.0f);
                        this.deleteRotateY += Engine.deltaTime * 60.0f * this.deleteRotateYVelocity;
                        this.deleteRotateYVelocity -= 0.0025f * (Engine.deltaTime * 60.0f);
                        this.labelPtr.renderMatrix = Helpers.CreateRotationY(this.deleteRotateY)
                            * Matrix.CreateTranslation(-128.0f, 80.0f, 160.0f);
                    }

                    float y = 48.0f;
                    for (int i = 0; i < BUTTON.COUNT; ++i)
                    {
                        if (this.rotateY[i] > this.targetRotateY[i])
                        {
                            this.rotateYVelocity[i] -= 0.0025f * (60.0f * Engine.deltaTime);
                            if (this.rotateYVelocity[i] < 0.0f)
                                this.rotateY[i] += 60.0f * Engine.deltaTime * this.rotateYVelocity[i];
                            this.rotateYVelocity[i] -= 0.0025f * (60.0f * Engine.deltaTime);
                            this.saveButtons[i].matrix = Helpers.CreateRotationY(this.rotateY[i])
                                * Matrix.CreateTranslation(-128.0f, y, 160.0f);
                        }
                        y -= 30.0f;
                    }

                    if (this.targetRotateY[BUTTON.COUNT - 1] >= this.rotateY[BUTTON.COUNT - 1])
                    {
                        this.state = STATE.SUBMENU;
                        this.deleteRotateYVelocity = 0.0f;
                        this.targetDeleteRotateY = MathHelper.ToRadians(22.5f);
                        for (int i = 0; i < BUTTON.COUNT; ++i)
                            this.targetRotateY[i] = MathHelper.ToRadians(16.0f);

                        float val = -0.02f;
                        for (int i = 0; i < BUTTON.COUNT; ++i)
                        {
                            this.rotateYVelocity[i] = val;
                            val -= 0.02f;
                        }

                        //this.playerSelect = CREATE_ENTITY(PlayerSelectScreen);
                        //((NativeEntity_PlayerSelectScreen*)this.playerSelect).saveSel = this;
                    }

                    float div = 60.0f * Engine.deltaTime * 16.0f;
                    MenuButton button = this.menuControl.buttons[this.menuControl.buttonID];
                    BackButton backButton = this.menuControl.backButton;
                    button.x += (512.0f - button.x) / div;
                    backButton.x += (1024.0f - backButton.x) / div;
                    this.delButton.x += (512.0f - this.delButton.x) / div;
                    break;
                }
            case STATE.SUBMENU: // player select idle
                break;
            case STATE.EXITSUBMENU:
                {
                    if (this.targetDeleteRotateY > this.deleteRotateY)
                    {
                        this.deleteRotateYVelocity += 0.0025f * (Engine.deltaTime * 60.0f);
                        this.deleteRotateY += Engine.deltaTime * 60.0f * this.deleteRotateYVelocity;
                        this.deleteRotateYVelocity += 0.0025f * (Engine.deltaTime * 60.0f);
                        if (this.deleteRotateY > this.targetDeleteRotateY)
                            this.deleteRotateY = this.targetDeleteRotateY;
                        this.labelPtr.renderMatrix = Helpers.CreateRotationY(this.deleteRotateY)
                            * Matrix.CreateTranslation(-128.0f, 80.0f, 160.0f);
                    }

                    float y = 48.0f;
                    for (int i = 0; i < BUTTON.COUNT; ++i)
                    {
                        if (this.targetRotateY[i] > this.rotateY[i])
                        {
                            this.rotateYVelocity[i] += 0.0025f * (60.0f * Engine.deltaTime);
                            if (this.rotateYVelocity[i] > 0.0)
                                this.rotateY[i] += 60.0f * Engine.deltaTime * this.rotateYVelocity[i];

                            this.rotateYVelocity[i] += 0.0025f * (60.0f * Engine.deltaTime);
                            if (this.rotateY[i] > this.targetRotateY[i])
                                this.rotateY[i] = this.targetRotateY[i];
                            this.saveButtons[i].matrix = Helpers.CreateRotationY(this.rotateY[i])
                                * Matrix.CreateTranslation(-128.0f, y, 160.0f);
                        }
                        y -= 30.0f;
                    }

                    float div = 60.0f * Engine.deltaTime * 16.0f;
                    MenuButton button = this.menuControl.buttons[this.menuControl.buttonID];
                    BackButton backButton = this.menuControl.backButton;
                    button.x += (112.0f - button.x) / div;
                    backButton.x += (230.0f - backButton.x) / div;
                    if (this.deleteEnabled)
                        this.delButton.x += (92.0f - this.delButton.x) / div;

                    if (backButton.x < Drawing.SCREEN_YSIZE)
                    {
                        backButton.x = Drawing.SCREEN_YSIZE;
                        this.state = STATE.MAIN;
                        this.menuControl.state = MenuControl.STATE.SUBMENU;
                    }
                    break;
                }
            case STATE.DELSETUP:
                {
                    this.menuControl.state = MenuControl.STATE.NONE;
                    if (this.saveButtons[this.selectedButton].state == SubMenuButton.STATE.SAVEBUTTON_SELECTED)
                    {
                        this.dialog = Objects.CreateNativeObject(() => new DialogPanel());
                        this.dialog.text = Font.GetCharactersForString(Strings.strDeleteMessage, FONT.TEXT);
                        this.state = STATE.DIALOGWAIT;
                    }
                    break;
                }
            case STATE.DIALOGWAIT:
                {
                    if (this.dialog.selection == DialogPanel.SELECTION.YES)
                    {
                        Audio.PlaySfxByName("Event", false);
                        for (int i = BUTTON.SAVE1; i < BUTTON.COUNT; ++i)
                        {
                            if (this.saveButtons[i].state == SubMenuButton.STATE.SAVEBUTTON_SELECTED)
                                this.saveButtons[i].useMeshH = false;
                        }
                        this.state = STATE.MAIN;
                        this.menuControl.state = MenuControl.STATE.SUBMENU;
                        this.saveButtons[this.selectedButton].text = Font.GetCharactersForString(Strings.strNewGame, FONT.LABEL);

                        this.saveButtons[this.selectedButton].state = SubMenuButton.STATE.IDLE;
                        this.saveButtons[this.selectedButton].textY = -4.0f;
                        this.saveButtons[this.selectedButton].scale = 0.1f;

                        saveGame.files[this.selectedButton - 1].characterId = 0;
                        saveGame.files[this.selectedButton - 1].lives = 3;
                        saveGame.files[this.selectedButton - 1].score = 0;
                        saveGame.files[this.selectedButton - 1].scoreBonus = 500000;
                        saveGame.files[this.selectedButton - 1].stageId = 0;
                        saveGame.files[this.selectedButton - 1].emeralds = 0;
                        saveGame.files[this.selectedButton - 1].specialStageId = 0;
                        SaveData.WriteSaveRAMData();

                        this.deleteEnabled = false;
                        for (int i = BUTTON.SAVE1; i < BUTTON.COUNT; ++i)
                        {
                            if (this.saveButtons[i].state == SubMenuButton.STATE.SAVEBUTTON_SELECTED)
                                this.deleteEnabled = true;
                        }
                    }
                    else if (this.dialog.selection == DialogPanel.SELECTION.NO)
                    {
                        for (int i = BUTTON.SAVE1; i < BUTTON.COUNT; ++i)
                        {
                            if (this.saveButtons[i].state == SubMenuButton.STATE.SAVEBUTTON_SELECTED)
                                this.saveButtons[i].useMeshH = false;
                        }
                        this.state = STATE.MAIN;
                        this.menuControl.state = MenuControl.STATE.SUBMENU;
                    }
                    break;
                }
            default: break;
        }
    }
}
