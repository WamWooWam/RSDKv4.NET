using Microsoft.Xna.Framework;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class DialogPanel : NativeEntity
{
    public enum TYPE { OK, YESNO }
    public enum SELECTION { YES, NO, OK }
    public enum STATE { SETUP, ENTER, MAIN, ACTION, EXIT, IDLE }

    public STATE state;
    public int buttonCount;
    public float stateTimer;
    public int field_1C; //??
    public float buttonScale;
    public MeshInfo panelMesh;
    public Matrix buttonMatrix;
    public Matrix buttonMult;
    public PushButton[] buttons = new PushButton[2];
    public int buttonSelected;
    public SELECTION selection;
    public ushort[] text = new ushort[128];
    public float textX;
    public float textY;
    public float scale;

    public override void Create()
    {
        panelMesh = LoadMesh("Data/Game/Models/Panel.bin", -1);
        SetMeshVertexColors(panelMesh, 0x28, 0x5C, 0xB0, 0xFF);
        buttonCount = (int)TYPE.YESNO;
    }

    public override void Main()
    {
        NewRenderState();
        SetRenderBlendMode(RENDER_BLEND.ALPHA);

        switch (this.state)
        {
            case STATE.SETUP:
                {
                    PushButton confirmButton = Objects.CreateNativeObject(() => new PushButton());
                    this.buttons[0] = confirmButton;
                    if (this.buttonCount == (int)TYPE.OK)
                    {
                        confirmButton.x = 0.0f;
                        confirmButton.y = -40.0f;
                        confirmButton.z = 0.0f;
                        confirmButton.scale = 0.25f;
                        confirmButton.bgColour = 0x00A048;
                        confirmButton.bgColourSelected = 0x00C060;
                        confirmButton.useRenderMatrix = true;
                        confirmButton.text = Font.GetCharactersForString(" OK ", FONT.LABEL);
                    }
                    else
                    {
                        confirmButton.x = -48.0f;
                        confirmButton.y = -40.0f;
                        confirmButton.z = 0.0f;
                        confirmButton.scale = 0.25f;
                        confirmButton.bgColour = 0x00A048;
                        confirmButton.bgColourSelected = 0x00C060;
                        confirmButton.useRenderMatrix = true;
                        confirmButton.text = Font.GetCharactersForString(Strings.strYes, FONT.LABEL);

                        PushButton noButton = Objects.CreateNativeObject(() => new PushButton());
                        this.buttons[1] = noButton;
                        noButton.useRenderMatrix = true;
                        noButton.scale = 0.25f;
                        noButton.x = 48.0f;
                        noButton.y = -40.0f;
                        noButton.z = 0.0f;
                        noButton.text = Font.GetCharactersForString(Strings.strNo, FONT.LABEL);
                    }
                    this.scale = 224.0f / (Font.GetTextWidth(this.text, FONT.TEXT, 1.0f) + 1.0f);
                    if (this.scale > 0.4f)
                        this.scale = 0.4f;
                    this.textX = Font.GetTextWidth(this.text, FONT.TEXT, this.scale) * -0.5f;
                    this.textY = Font.GetTextHeight(this.text, FONT.TEXT, this.scale) * 0.5f;
                    this.state = STATE.ENTER;

                    // FallThrough
                    goto case STATE.ENTER;
                }
            case STATE.ENTER:
                {
                    this.buttonScale += (0.77f - this.buttonScale) / (Engine.deltaTime * 60.0f * 8.0f);
                    if (this.buttonScale > 0.75f)
                        this.buttonScale = 0.75f;

                    NewRenderState();

                    this.buttonMatrix =
                        Matrix.CreateScale(this.buttonScale, this.buttonScale, 1.0f) *
                        Matrix.CreateTranslation(0.0f, 0.0f, 160.0f);

                    SetRenderMatrix(this.buttonMatrix);
                    for (int i = 0; i < this.buttonCount; ++i)
                        this.buttons[i].renderMatrix = this.buttonMatrix;

                    this.stateTimer += Engine.deltaTime;
                    if (this.stateTimer > 0.5f)
                    {
                        this.state = STATE.MAIN;
                        this.stateTimer = 0.0f;
                    }
                    break;
                }
            case STATE.MAIN:
                {
                    Input.CheckKeyDown(ref Input.keyDown);
                    Input.CheckKeyPress(ref Input.keyPress);
                    SetRenderMatrix(this.buttonMatrix);
                    if (!NativeGlobals.usePhysicalControls)
                    {
                        if (Input.touches < 1)
                        {
                            if (this.buttons[0].state == PushButton.STATE.SELECTED)
                            {
                                this.buttonSelected = 0;
                                this.state = STATE.ACTION;
                                Audio.PlaySfxByName("Menu Select", false);
                                this.buttons[0].state = PushButton.STATE.FLASHING;
                            }
                            if (this.buttonCount == (int)TYPE.YESNO && this.buttons[1].state == PushButton.STATE.SELECTED)
                            {
                                this.buttonSelected = 1;
                                this.state = STATE.ACTION;
                                Audio.PlaySfxByName("Menu Select", false);
                                this.buttons[1].state = PushButton.STATE.FLASHING;
                            }
                        }
                        else
                        {
                            if (this.buttonCount == (int)TYPE.OK)
                            {
                                this.buttons[0].state =
                                    Input.CheckTouchRect(0.0f, -30.0f, (this.buttons[0].textWidth + (this.buttons[0].scale * 64.0f)) * 0.75f, 12.0f) >= 0
                                    ? PushButton.STATE.SELECTED : PushButton.STATE.UNSELECTED;
                            }
                            else
                            {
                                this.buttons[0].state =
                                    Input.CheckTouchRect(-36.0f, -30.0f, (this.buttons[0].textWidth + (this.buttons[0].scale * 64.0f)) * 0.75f, 12.0f) >= 0
                                    ? PushButton.STATE.SELECTED : PushButton.STATE.UNSELECTED;
                                this.buttons[1].state =
                                    Input.CheckTouchRect(36.0f, -30.0f, (this.buttons[1].textWidth + (this.buttons[1].scale * 64.0f)) * 0.75f, 12.0f) >= 0
                                    ? PushButton.STATE.SELECTED : PushButton.STATE.UNSELECTED;
                            }
                        }
                        if (Input.keyDown.left)
                        {
                            NativeGlobals.usePhysicalControls = true;
                            this.buttonSelected = 1;
                        }
                        else if (Input.keyDown.right)
                        {
                            NativeGlobals.usePhysicalControls = true;
                            this.buttonSelected = 0;
                        }
                    }
                    else if (Input.touches > 0)
                    {
                        NativeGlobals.usePhysicalControls = false;
                    }
                    else if (this.buttonCount == (int)TYPE.OK)
                    {
                        this.buttonSelected = 0;
                        if (Input.keyPress.start || Input.keyPress.A)
                        {
                            this.state = STATE.ACTION;
                            Audio.PlaySfxByName("Menu Select", false);
                            this.buttons[this.buttonSelected].state = PushButton.STATE.FLASHING;
                        }
                    }
                    else
                    {
                        if (Input.keyPress.left)
                        {
                            Audio.PlaySfxByName("Menu Move", false);
                            if (--this.buttonSelected < 0)
                                this.buttonSelected = 1;
                        }
                        if (Input.keyPress.right)
                        {
                            Audio.PlaySfxByName("Menu Move", false);
                            if (++this.buttonSelected > 1)
                                this.buttonSelected = 0;
                        }
                        this.buttons[0].state = 0;
                        this.buttons[1].state = 0;
                        this.buttons[this.buttonSelected].state = PushButton.STATE.SELECTED;

                        if (Input.keyPress.start || Input.keyPress.A)
                        {
                            this.state = STATE.ACTION;
                            Audio.PlaySfxByName("Menu Select", false);
                            this.buttons[this.buttonSelected].state = PushButton.STATE.FLASHING;
                        }
                    }
                    if (this.state == STATE.MAIN && Input.keyPress.B)
                    {
                        Audio.PlaySfxByName("Menu Back", false);
                        this.selection = SELECTION.NO;
                        this.state = STATE.EXIT;
                    }
                    break;
                }
            case STATE.ACTION:
                SetRenderMatrix(this.buttonMatrix);
                if (this.buttons[this.buttonSelected].state != PushButton.STATE.UNSELECTED)
                {
                    this.state = STATE.EXIT;

                    this.selection = (SELECTION)(this.buttonSelected + 1);
                    if (this.buttonCount == (int)TYPE.OK)
                        this.selection = SELECTION.OK;
                }
                break;
            case STATE.EXIT:
                this.buttonScale =
                    this.buttonScale + ((((this.stateTimer < 0.2f) ? 1 : -1) - this.buttonScale) / (Engine.deltaTime * 60.0f * 8.0f));
                if (this.buttonScale < 0.0)
                    this.buttonScale = 0.0f;
                NewRenderState();

                this.buttonMatrix =
                    Matrix.CreateScale(this.buttonScale, this.buttonScale, 1.0f) *
                    Matrix.CreateTranslation(0.0f, 0.0f, 160.0f);

                SetRenderMatrix(this.buttonMatrix);

                for (int i = 0; i < this.buttonCount; ++i)
                    this.buttons[i].renderMatrix = this.buttonMatrix;

                this.stateTimer += Engine.deltaTime;
                if (this.stateTimer > 0.5)
                {
                    for (int i = 0; i < this.buttonCount; ++i) Objects.RemoveNativeObject(this.buttons[i]);
                    Objects.RemoveNativeObject(this);
                    return;
                }
                break;
            case STATE.IDLE: SetRenderMatrix(this.buttonMatrix); break;
            default: break;
        }
        RenderMesh(this.panelMesh, MESH.COLOURS, false);
        RenderText(this.text, FONT.TEXT, this.textX, this.textY, 0, this.scale, 255);

    }
}
