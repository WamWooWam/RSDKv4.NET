using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class SegaSplash : NativeEntity
{
    private enum STATE { ENTER, EXIT, SPAWNCWSPLASH }

    private STATE state;
    private float rectAlpha;
    private int textureId;

    public override void Create()
    {
        state = STATE.ENTER;
        rectAlpha = 320.0f;
        if (Engine.language == LANGUAGE.JP)
            textureId = Renderer.LoadTexture("Data/Game/Menu/SegaJP.png", TEXFMT.RGBA5551);
        else
            textureId = Renderer.LoadTexture("Data/Game/Menu/Sega.png", TEXFMT.RGBA5551);

    }

    public override void Main()
    {
        switch (state)
        {
            case STATE.ENTER:
                this.rectAlpha -= 300.0f * Engine.deltaTime;
                if (this.rectAlpha < -320.0)
                    this.state = STATE.EXIT;
                Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0xFF, 0xFF, 0xFF, 0xFF);
                Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                Renderer.RenderImage(0.0f, 0.0f, 160.0f, 0.4f, 0.4f, 256.0f, 128.0f, 512.0f, 256.0f, 0.0f, 0.0f, 255, (byte)this.textureId);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (int)this.rectAlpha);
                break;
            case STATE.EXIT:
                this.rectAlpha += 300.0f * Engine.deltaTime;
                if (this.rectAlpha > 512.0)
                    this.state = STATE.SPAWNCWSPLASH;
                Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0xFF, 0xFF, 0xFF, 0xFF);
                Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                Renderer.RenderImage(0.0f, 0.0f, 160.0f, 0.4f, 0.4f, 256.0f, 128.0f, 512.0f, 256.0f, 0.0f, 0.0f, 255, (byte)this.textureId);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (int)this.rectAlpha);
                break;
            case STATE.SPAWNCWSPLASH:
                Objects.ResetNativeObject(this, () => new CWSplash());
                break;
        }
    }
}
