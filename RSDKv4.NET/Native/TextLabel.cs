using Microsoft.Xna.Framework;
using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class TextLabel : NativeEntity
{
    public enum STATE { NONE = -1, IDLE, BLINK, BLINK_FAST };

    public float x;
    public float y;
    public float z;
    public float alignOffset;
    public float timer;
    public float scale;
    public int alpha;
    public int fontID;
    public ushort[] text;
    public STATE state;
    public bool useRenderMatrix;
    public Matrix renderMatrix;

    public bool useColours;
    public byte r;
    public byte g;
    public byte b;

    public override void Create()
    {
        z = 160.0f;
        alpha = 0xFF;
        state = STATE.IDLE;
    }

    public override void Main()
    {
        if (useRenderMatrix)
        {
            NewRenderState();
            SetRenderMatrix(renderMatrix);
        }

#if !RETRO_USE_ORIGINAL_CODE
        if (useColours)
            SetRenderVertexColor(r, g, b);
#endif

        switch (state)
        {
            default: break;
            case STATE.NONE: break;
            case STATE.IDLE:
                SetRenderBlendMode(RENDER_BLEND.ALPHA);
                RenderText(text, fontID, x - alignOffset, y, (int)z, scale, alpha);
                break;
            case STATE.BLINK:
                timer += Engine.deltaTime;
                if (timer > 1.0f)
                    timer -= 1.0f;

                if (timer > 0.5)
                {
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    
                    RenderText(text, fontID, x - alignOffset, y, (int)z, scale, alpha);
                }
                break;
            case STATE.BLINK_FAST:
                timer += Engine.deltaTime;
                if (timer > 0.1f)
                    timer -= 0.1f;

                if (timer > 0.05)
                {
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    RenderText(text, fontID, x - alignOffset, y, (int)z, scale, alpha);
                }
                break;
        }

#if !RETRO_USE_ORIGINAL_CODE
        if (useColours)
            SetRenderVertexColor(0xFF, 0xFF, 0xFF);
#endif

        if (useRenderMatrix)
        {
            NewRenderState();
            SetRenderMatrix(null);
        }
    }

    public void SetAlignment(int align)
    {
        switch (align)
        {
            default:
            case ALIGN.LEFT: alignOffset = 0.0f; break;
            case ALIGN.CENTER: alignOffset = Font.GetTextWidth(text, fontID, scale) * 0.5f; break;
            case ALIGN.RIGHT: alignOffset = Font.GetTextWidth(text, fontID, scale); break;
        }
    }
}
