using Microsoft.Xna.Framework;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class MenuBG : NativeEntity
{
    public bool isFading;
    public float fadeTimer;
    public float circle1Rot;
    public float circle2Rot;
    public float Ztrans1;
    public float Ztrans2;
    public float ZtransRender;
    public MeshInfo bgCircle1;
    public MeshInfo bgCircle2;
    public MeshInfo bgLines;
    public MeshAnimator animator;
    public int textureID;
    public byte fadeR;
    public byte fadeG;
    public byte fadeB;
    public int alpha;
    public Matrix renderMatrix;
    public Matrix matrixTemp;
    public Matrix circle1;
    public Matrix circle2;

    public override void Create()
    {
        this.animator = new MeshAnimator();
        this.textureID = Renderer.LoadTexture("Data/Game/Menu/BG1.png", TEXFMT.RGBA5551);
        this.bgCircle1 = Renderer.LoadMesh("Data/Game/Models/BGCircle1.bin", -1);
        this.bgCircle2 = Renderer.LoadMesh("Data/Game/Models/BGCircle2.bin", -1);
        this.bgLines = Renderer.LoadMesh("Data/Game/Models/BGLines.bin", -1);
        Renderer.SetMeshAnimation(this.bgLines, this.animator, 0, 40, 0.0f);
        this.animator.loopAnimation = true;
        this.fadeR = 0xA0;
        this.fadeG = 0xC0;
        this.fadeB = 0xF8;
        this.isFading = true;
        Renderer.SetMeshVertexColors(this.bgCircle1, 0xE0, 0xD0, 0xC0, 0xFF);
        Renderer.SetMeshVertexColors(this.bgCircle2, 0xE0, 0xD0, 0xC0, 0xFF);
        Renderer.SetMeshVertexColors(this.bgLines, 0xE0, 0, 0, 0xFF);
        this.Ztrans1 = -32.0f;
        this.Ztrans2 = -64.0f;
        this.ZtransRender = -128.0f;
    }

    public override void Main()
    {
        if (this.isFading)
        {
            Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
            this.fadeTimer += Engine.deltaTime;
            if (this.fadeTimer > 1.5)
                this.isFading = false;

            if (this.fadeR < 0xF8)
                this.fadeR += 8;

            if (this.fadeG < 0xF8)
                this.fadeG += 8;

            if (this.fadeB < 0xF8)
                this.fadeB += 8;

            if (this.alpha < 0x100)
                this.alpha += 0x10;

            this.Ztrans1 = ((160.0f - this.Ztrans1) / (16.0f * Engine.deltaTime * 60.0f)) + this.Ztrans1;
            this.Ztrans2 = ((160.0f - this.Ztrans2) / (18.0f * Engine.deltaTime * 60.0f)) + this.Ztrans2;
            this.ZtransRender = ((160.0f - this.ZtransRender) / (Engine.deltaTime * 60.0f * 20.0f)) + this.ZtransRender;
            Renderer.RenderRect(-Renderer.SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, Renderer.SCREEN_XSIZE_F, SCREEN_YSIZE_F, this.fadeR, this.fadeG, this.fadeB, 255);
            Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
            Renderer.SetRenderVertexColor(224, 208, 192);
            Renderer.RenderImage(-64.0f, 0.0f, 160.0f, 0.45f, 0.45f, 256.0f, 256.0f, 512.0f, 512.0f, 0.0f, 0.0f, this.alpha, this.textureID);
            Renderer.SetRenderVertexColor(255, 255, 255);
            Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
            this.circle1Rot = this.circle1Rot - Engine.deltaTime;
            if (this.circle1Rot < -MathHelper.TwoPi)
                this.circle1Rot += MathHelper.TwoPi;
            this.circle2Rot += Engine.deltaTime;
            if (this.circle2Rot > MathHelper.TwoPi)
                this.circle2Rot -= MathHelper.TwoPi;
            Renderer.NewRenderState();

            this.circle1 = Matrix.CreateRotationZ(this.circle1Rot) *
                Matrix.CreateTranslation(120.0f, 94.0f, this.Ztrans1);

            Renderer.SetRenderMatrix(this.circle1);
            Renderer.RenderMesh(this.bgCircle1, MESH.COLOURS, false);

            this.circle2 = Matrix.CreateRotationZ(this.circle2Rot) *
                Matrix.CreateTranslation(4.0f, 150.0f, this.Ztrans2);

            Renderer.SetRenderMatrix(this.circle2);
            Renderer.RenderMesh(this.bgCircle2, MESH.COLOURS, false);

            this.renderMatrix = Matrix.CreateTranslation(0.0f, 0.0f, this.ZtransRender);
        }
        else
        {
            Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
            Renderer.RenderRect(-Renderer.SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, Renderer.SCREEN_XSIZE_F, SCREEN_YSIZE_F, 248, 248, 248, 255);
            Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
            Renderer.SetRenderVertexColor(0xE0, 0xD0, 0xC0);
            Renderer.RenderImage(-64.0f, 0.0f, 160.0f, 0.45f, 0.45f, 256.0f, 256.0f, 512.0f, 512.0f, 0.0f, 0.0f, 255, this.textureID);
            Renderer.SetRenderVertexColor(0xFF, 0xFF, 0xFF);
            Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
            this.circle1Rot -= Engine.deltaTime;
            if (this.circle1Rot < -MathHelper.TwoPi)
                this.circle1Rot += MathHelper.TwoPi;
            this.circle2Rot += Engine.deltaTime;
            if (this.circle2Rot > MathHelper.TwoPi)
                this.circle2Rot -= MathHelper.TwoPi;
            Renderer.NewRenderState();

            this.circle1 = Matrix.CreateRotationZ(this.circle1Rot) *
                Matrix.CreateTranslation(120.0f, 94.0f, 160.0f);

            Renderer.SetRenderMatrix(this.circle1);
            Renderer.RenderMesh(this.bgCircle1, MESH.COLOURS, false);

            this.circle2 = Matrix.CreateRotationZ(this.circle2Rot) *
                Matrix.CreateTranslation(4.0f, 150.0f, 160.0f);

            Renderer.SetRenderMatrix(this.circle2);
            Renderer.RenderMesh(this.bgCircle2, MESH.COLOURS, false);

            this.renderMatrix = Matrix.CreateTranslation(0.0f, 0.0f, 160.0f);
        }
        Renderer.SetRenderMatrix(this.renderMatrix);
        this.animator.animationSpeed = 8.0f * Engine.deltaTime;
        Renderer.AnimateMesh(this.bgLines, this.animator);
        Renderer.RenderMesh(this.bgLines, MESH.COLOURS, false);
        Renderer.SetRenderMatrix(null);
    }
}
