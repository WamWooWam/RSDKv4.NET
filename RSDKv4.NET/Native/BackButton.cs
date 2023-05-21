using System;
using Microsoft.Xna.Framework;
using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class BackButton : NativeEntity
{
    public int field_10;
    public bool visible;
    public int field_18;
    public int field_1C;
    public float x;
    public float y;
    public float z;
    public MeshInfo meshBack;
    public float angle;
    public float scale;

    public int textureCircle;
    public byte r;
    public byte g;
    public byte b;
    public Matrix renderMatrix;
    public Matrix matrixTemp;

    public override void Create()
    {
        textureCircle = LoadTexture("Data/Game/Menu/Circle.png", TEXFMT.RGBA4444);

        int texture = LoadTexture("Data/Game/Menu/Intro.png", TEXFMT.RGBA4444);
        meshBack = LoadMesh("Data/Game/Models/BackArrow.bin", texture);
        x = 0.0f;
        y = 16.0f;
        z = 160.0f;
        r = 0xFF;
        g = 0xFF;
        b = 0x00;
    }

    public override void Main()
    {
        if (this.visible)
        {
            if (this.scale < 0.2f)
            {
                this.scale += (0.25f - this.scale) / (60.0f * Engine.deltaTime * 16.0f);
                if (this.scale > 0.2f)
                    this.scale = 0.2f;
            }
            SetRenderBlendMode(RENDER_BLEND.ALPHA);
            SetRenderVertexColor(this.r, this.g, this.b);
            RenderImage(this.x, this.y, this.z, this.scale, this.scale, 256.0f, 256.0f, 512.0f, 512.0f, 0.0f, 0.0f, 255, this.textureCircle);
            SetRenderVertexColor(0xFF, 0xFF, 0xFF);
            SetRenderBlendMode(RENDER_BLEND.NONE);

            this.angle -= Engine.deltaTime + Engine.deltaTime;
            if (this.angle < -MathHelper.TwoPi)
                this.angle += MathHelper.TwoPi;

            NewRenderState();
            this.renderMatrix =
                Matrix.CreateScale(((float)Math.Cos(angle) * 0.35f) + 1.25f, ((float)Math.Cos(this.angle) * 0.35f) + 1.25f, 1.0f)
                * Matrix.CreateTranslation(this.x, this.y, this.z - 8.0f);

            SetRenderMatrix(this.renderMatrix);
            RenderMesh(this.meshBack, MESH.NORMALS, true);
            SetRenderMatrix(null);
        }
    }
}
