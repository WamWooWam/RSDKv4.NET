﻿using Microsoft.Xna.Framework;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class SegaIDButton : NativeEntity
{
    public enum STATE { IDLE, PRESSED }

    public float x;
    public float y;
    public float z;
    public float texX;
    public STATE state;
    public int alpha;
    public int field_28;
    public int field_2C;
    public int textureID;
    public bool useRenderMatrix;
    public Matrix renderMatrix;

    public override void Create()
    {
        this.z = 160.0f;
        this.state = STATE.IDLE;
        this.textureID = Renderer.LoadTexture("Data/Game/Menu/SegaID.png", TEXFMT.RGBA8888);
    }

    public override void Main()
    {
        if (this.useRenderMatrix)
            Renderer.SetRenderMatrix(this.renderMatrix);
        Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);

        switch (this.state)
        {
            case STATE.IDLE:
                Renderer.RenderImage(this.x, this.y, this.z, 0.25f, 0.25f, 64.0f, 64.0f, 128.0f, 128.0f, this.texX, 0.0f, this.alpha, this.textureID);
                break;
            case STATE.PRESSED:
                Renderer.RenderImage(this.x, this.y, this.z, 0.3f, 0.3f, 64.0f, 64.0f, 128.0f, 128.0f, this.texX, 0.0f, this.alpha, this.textureID);
                break;
        }
        Renderer.SetRenderVertexColor(0xFF, 0xFF, 0xFF);
        Renderer.NewRenderState();
        if (this.useRenderMatrix)
            Renderer.SetRenderMatrix(null);
    }
}
