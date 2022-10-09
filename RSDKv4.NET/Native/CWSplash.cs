using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class CWSplash : NativeEntity
{
    private enum STATE
    {
        ENTER, EXIT, SPAWNTITLE
    }

    private STATE state;
    private float rectAlpha;
    private int textureId;

    public override void Create()
    {
        this.state = STATE.ENTER;
        this.rectAlpha = 320.0f;
        this.textureId = LoadTexture("Data/Game/Menu/CWLogo.png", TEXFMT.RGBA8888);
    }

    public override void Main()
    {
        switch (this.state)
        {
            case STATE.ENTER:
                this.rectAlpha -= 300.0f * Engine.deltaTime;
                if (this.rectAlpha < -320.0f)
                    this.state = STATE.EXIT;
                SetRenderBlendMode(RENDER_BLEND.ALPHA);
                RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0xFF, 0x90, 0x00, 0xFF);
                SetRenderBlendMode(RENDER_BLEND.ALPHA);
                RenderImage(0.0f, 0.0f, 160.0f, 0.25f, 0.25f, 512.0f, 256.0f, 1024.0f, 512.0f, 0.0f, 0.0f, 255, (byte)this.textureId);
                RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (int)this.rectAlpha);
                break;
            case STATE.EXIT:
                this.rectAlpha += 300.0f * Engine.deltaTime;
                if (this.rectAlpha > 512.0f)
                    this.state = STATE.SPAWNTITLE;
                SetRenderBlendMode(RENDER_BLEND.ALPHA);
                RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0xFF, 0x90, 0x00, 0xFF);
                SetRenderBlendMode(RENDER_BLEND.ALPHA);
                RenderImage(0.0f, 0.0f, 160.0f, 0.25f, 0.25f, 512.0f, 256.0f, 1024.0f, 512.0f, 0.0f, 0.0f, 255, (byte)this.textureId);
                RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (int)this.rectAlpha);
                break;
            case STATE.SPAWNTITLE: 
                Objects.ResetNativeObject(this, () => new TitleScreen());
                break;
        }
    }
}
