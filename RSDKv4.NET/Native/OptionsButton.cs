using Microsoft.Xna.Framework;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class OptionsButton : MenuButton
{
    public override void Create()
    {
        this.textureCircle = LoadTexture("Data/Game/Menu/Circle.png", TEXFMT.RGBA4444);

        int texture = LoadTexture("Data/Game/Menu/Intro.png", TEXFMT.RGBA4444);
        this.mesh = LoadMesh("Data/Game/Models/Options.bin", texture);
        this.x = 0.0f;
        this.y = 16.0f;
        this.z = 160.0f;
        this.r = 0xFF;
        this.g = 0xFF;
        this.b = 0x00;
        this.label = Objects.CreateNativeObject(() => new TextLabel());
        this.label.fontId = FONT.HEADING;
        this.label.scale = 0.15f;
        this.label.alpha = 0;
        this.label.state = TextLabel.STATE.IDLE;
        this.label.text = Font.GetCharactersForString(Strings.strHelpAndOptions, FONT.HEADING);
        this.label.SetAlignment(ALIGN.CENTER);
    }

    public override void Main()
    {
        if (this.visible)
        {
            if (this.scale < 0.2f)
            {
                this.scale += ((0.25f - this.scale) / ((60.0f * Engine.deltaTime) * 16.0f));
                if (this.scale > 0.2f)
                    this.scale = 0.2f;
            }
            SetRenderBlendMode(RENDER_BLEND.ALPHA);
            SetRenderVertexColor(this.r, this.g, this.b);
            RenderImage(this.x, this.y, this.z, this.scale, this.scale, 256.0f, 256.0f, 512.0f, 512.0f, 0.0f, 0.0f, 255, this.textureCircle);
            SetRenderVertexColor(0xFF, 0xFF, 0xFF);
            SetRenderBlendMode(RENDER_BLEND.NONE);

            this.angle -= Engine.deltaTime;
            if (this.angle < -MathHelper.TwoPi)
                this.angle += MathHelper.TwoPi;

            NewRenderState();

            this.renderMatrix = Matrix.CreateRotationZ(this.angle) *
                Matrix.CreateTranslation(this.x, this.y, this.z - 8.0f);

            SetRenderMatrix(this.renderMatrix);
            RenderMesh(this.mesh, MESH.COLOURS, true);
            SetRenderMatrix(null);

            TextLabel label = this.label;
            label.x = this.x;
            label.y = this.y - 72.0f;
            label.z = this.z;
            if (label.x <= -8.0 || label.x >= 8.0)
            {
                if (label.alpha > 0)
                    label.alpha -= 8;
            }
            else if (label.alpha < 0x100)
                label.alpha += 8;
        }
    }
}
