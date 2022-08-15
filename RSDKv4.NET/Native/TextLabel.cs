using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public static class ALIGN
{
    public const int LEFT = 0, CENTER = 1, RIGHT = 2;
}

internal class TextLabel : NativeEntity
{
    public enum TEXTLABEL_STATE { NONE = -1, IDLE, BLINK, BLINK_FAST };

    public float x;
    public float y;
    public float z;
    public float alignOffset;
    public float timer;
    public float scale;
    public int alpha;
    public int fontID;
    public ushort[] text;
    public TEXTLABEL_STATE state;
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
        state = TEXTLABEL_STATE.IDLE;
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
            case TEXTLABEL_STATE.NONE: break;
            case TEXTLABEL_STATE.IDLE:
                SetRenderBlendMode(RENDER_BLEND.ALPHA);
                RenderText(text, fontID, x - alignOffset, y, (int)z, scale, alpha);
                break;
            case TEXTLABEL_STATE.BLINK:
                timer += Engine.deltaTime;
                if (timer > 1.0f)
                    timer -= 1.0f;

                if (timer > 0.5)
                {
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    
                    RenderText(text, fontID, x - alignOffset, y, (int)z, scale, alpha);
                }
                break;
            case TEXTLABEL_STATE.BLINK_FAST:
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

    public void Align(int align)
    {
        switch (align)
        {
            default:
            case ALIGN.LEFT: alignOffset = 0.0f; break;
            case ALIGN.CENTER: alignOffset = Text.GetTextWidth(text, fontID, scale) * 0.5f; break;
            case ALIGN.RIGHT: alignOffset = Text.GetTextWidth(text, fontID, scale); break;
        }
    }
}
