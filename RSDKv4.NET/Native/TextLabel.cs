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
    public int fontId;
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
            Renderer.NewRenderState();
            Renderer.SetRenderMatrix(renderMatrix);
        }

#if !RETRO_USE_ORIGINAL_CODE
        if (useColours)
            Renderer.SetRenderVertexColor(r, g, b);
#endif

        switch (state)
        {
            default: break;
            case STATE.NONE: break;
            case STATE.IDLE:
                Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                Renderer.RenderText(text, fontId, x - alignOffset, y, (int)z, scale, alpha);
                break;
            case STATE.BLINK:
                timer += Engine.deltaTime;
                if (timer > 1.0f)
                    timer -= 1.0f;

                if (timer > 0.5)
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderText(text, fontId, x - alignOffset, y, (int)z, scale, alpha);
                }
                break;
            case STATE.BLINK_FAST:
                timer += Engine.deltaTime;
                if (timer > 0.1f)
                    timer -= 0.1f;

                if (timer > 0.05)
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderText(text, fontId, x - alignOffset, y, (int)z, scale, alpha);
                }
                break;
        }

#if !RETRO_USE_ORIGINAL_CODE
        if (useColours)
            Renderer.SetRenderVertexColor(0xFF, 0xFF, 0xFF);
#endif

        if (useRenderMatrix)
        {
            Renderer.NewRenderState();
            Renderer.SetRenderMatrix(null);
        }
    }

    public void SetAlignment(int align)
    {
        alignOffset = align switch
        {
            ALIGN.CENTER => Font.GetTextWidth(text, fontId, scale) * 0.5f,
            ALIGN.RIGHT => Font.GetTextWidth(text, fontId, scale),
            _ => 0.0f,
        };
    }
}
