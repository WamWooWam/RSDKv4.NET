using Microsoft.Xna.Framework;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class PushButton : NativeEntity
{
    public enum STATE { UNSELECTED, SELECTED, FLASHING, SCALED }

    public float x;
    public float y;
    public float z;
    public STATE state;
    public float textWidth;
    public float xOff;
    public float yOff;
    public float stateTimer;
    public float flashTimer;
    public float scale;
    public float textScale;
    public int alpha;
    public int textColour;
    public int textColourSelected;
    public int bgColour;
    public int bgColourSelected;
    public int symbolsTex;
    public ushort[] text = new ushort[64];
    public bool useRenderMatrix;
    public Matrix renderMatrix;

    public override void Create()
    {
        this.z = 160.0f;
        this.alpha = 255;
        this.scale = 0.15f;
        this.state = STATE.SCALED;
        this.symbolsTex = Renderer.LoadTexture("Data/Game/Menu/Symbols.png", TEXFMT.RGBA4444);
        this.bgColour = 0xFF0000;
        this.bgColourSelected = 0xFF4000;
        this.textColour = 0xFFFFFF;
        this.textColourSelected = 0xFFFF00;
    }

    public override void Main()
    {
        Renderer.NewRenderState();
        if (this.useRenderMatrix)
            Renderer.SetRenderMatrix(this.renderMatrix);

        Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);

        switch (this.state)
        {
            case STATE.UNSELECTED:
                {
                    Renderer.SetRenderVertexColor((byte)((this.bgColour >> 16) & 0xFF), (byte)((this.bgColour >> 8) & 0xFF), (byte)(this.bgColour & 0xFF));
                    Renderer.RenderImage(this.x - this.textWidth, this.y, this.z, this.scale, this.scale, 64.0f, 64.0f, 64.0f, 128.0f, 0.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x, this.y, this.z, this.textWidth + this.textWidth, this.scale, 0.5f, 64.0f, 1.0f, 128.0f, 63.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x + this.textWidth, this.y, this.z, this.scale, this.scale, 0.0f, 64.0f, 64.0f, 128.0f, 64.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.SetRenderVertexColor((byte)(this.textColour >> 16), (byte)((this.textColour >> 8) & 0xFF), (byte)(this.textColour & 0xFF));
                    Renderer.RenderText(this.text, FONT.LABEL, this.x - this.xOff, this.y - this.yOff, (int)this.z, this.textScale, this.alpha);
                    break;
                }
            case STATE.SELECTED:
                {
                    if (NativeGlobals.usePhysicalControls)
                    {
                        Renderer.SetRenderVertexColor(0x00, 0x00, 0x00);
                        Renderer.RenderImage(this.x - this.textWidth, this.y, this.z, 1.1f * this.scale, 1.1f * this.scale, 64.0f, 64.0f, 64.0f, 128.0f,
                                    0.0f, 0.0f, this.alpha, this.symbolsTex);
                        Renderer.RenderImage(this.x, this.y, this.z, this.textWidth + this.textWidth, 1.1f * this.scale, 0.5f, 64.0f, 1.0f, 128.0f, 63.0f,
                                    0.0f, this.alpha, this.symbolsTex);
                        Renderer.RenderImage(this.x + this.textWidth, this.y, this.z, 1.1f * this.scale, 1.1f * this.scale, 0.0f, 64.0f, 64.0f, 128.0f,
                                    64.0f, 0.0f, this.alpha, this.symbolsTex);
                    }
                    Renderer.SetRenderVertexColor((byte)((this.bgColourSelected >> 16) & 0xFF), (byte)((this.bgColourSelected >> 8) & 0xFF), (byte)(this.bgColourSelected & 0xFF));
                    Renderer.RenderImage(this.x - this.textWidth, this.y, this.z, this.scale, this.scale, 64.0f, 64.0f, 64.0f, 128.0f, 0.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x, this.y, this.z, this.textWidth + this.textWidth, this.scale, 0.5f, 64.0f, 1.0f, 128.0f, 63.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x + this.textWidth, this.y, this.z, this.scale, this.scale, 0.0f, 64.0f, 64.0f, 128.0f, 64.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.SetRenderVertexColor((byte)(this.textColourSelected >> 16), (byte)((this.textColourSelected >> 8) & 0xFF), (byte)(this.textColourSelected & 0xFF));
                    Renderer.RenderText(this.text, FONT.LABEL, this.x - this.xOff, this.y - this.yOff, (int)this.z, this.textScale, this.alpha);
                    break;
                }
            case STATE.FLASHING:
                {
                    this.flashTimer += Engine.deltaTime;
                    if (this.flashTimer > 0.1f)
                        this.flashTimer -= 0.1f;
                    Renderer.SetRenderVertexColor((byte)((this.bgColourSelected >> 16) & 0xFF), (byte)((this.bgColourSelected >> 8) & 0xFF), (byte)(this.bgColourSelected & 0xFF));
                    Renderer.RenderImage(this.x - this.textWidth, this.y, this.z, this.scale, this.scale, 64.0f, 64.0f, 64.0f, 128.0f, 0.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x, this.y, this.z, this.textWidth + this.textWidth, this.scale, 0.5f, 64.0f, 1.0f, 128.0f, 63.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x + this.textWidth, this.y, this.z, this.scale, this.scale, 0.0f, 64.0f, 64.0f, 128.0f, 64.0f, 0.0f,
                                this.alpha, this.symbolsTex);

                    int colour = this.flashTimer > 0.05f ? this.textColourSelected : this.textColour;
                    Renderer.SetRenderVertexColor((byte)((colour >> 16) & 0xFF), (byte)((colour >> 8) & 0xFF), (byte)(colour & 0xFF));
                    Renderer.RenderText(this.text, FONT.LABEL, this.x - this.xOff, this.y - this.yOff, (int)this.z, this.textScale, this.alpha);
                    this.stateTimer += Engine.deltaTime;
                    if (this.stateTimer > 0.5)
                    {
                        this.stateTimer = 0.0f;
                        this.state = STATE.UNSELECTED;
                    }
                    break;
                }
            case STATE.SCALED:
                {
                    this.state = 0;
                    this.xOff = Font.GetTextWidth(this.text, FONT.LABEL, this.scale) * 0.375f;
                    this.textWidth = Font.GetTextWidth(this.text, FONT.LABEL, this.scale) * 0.375f;
                    this.yOff = 0.75f * this.scale * 32.0f;
                    this.textScale = 0.75f * this.scale;

                    Renderer.SetRenderVertexColor((byte)(this.bgColour >> 16), (byte)((this.bgColour >> 8) & 0xFF), (byte)(this.bgColour & 0xFF));
                    Renderer.RenderImage(this.x - this.textWidth, this.y, this.z, this.scale, this.scale, 64.0f, 64.0f, 64.0f, 128.0f, 0.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x, this.y, this.z, this.textWidth + this.textWidth, this.scale, 0.5f, 64.0f, 1.0f, 128.0f, 63.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.RenderImage(this.x + this.textWidth, this.y, this.z, this.scale, this.scale, 0.0f, 64.0f, 64.0f, 128.0f, 64.0f, 0.0f,
                                this.alpha, this.symbolsTex);
                    Renderer.SetRenderVertexColor((byte)((this.textColour >> 16) & 0xFF), (byte)((this.textColour >> 8) & 0xFF), (byte)(this.textColour & 0xFF));
                    Renderer.RenderText(this.text, FONT.LABEL, this.x - this.xOff, this.y - this.yOff, (int)this.z, this.textScale, this.alpha);
                    break;
                }
        }

        Renderer.SetRenderVertexColor(0xFF, 0xFF, 0xFF);
        if (this.useRenderMatrix)
        {
            Renderer.NewRenderState();
            Renderer.SetRenderMatrix(null);
        }
    }
}

