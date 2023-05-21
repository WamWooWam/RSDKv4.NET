using Microsoft.Xna.Framework;
using RSDKv4.Utility;
using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;


public class TitleScreen : NativeEntity
{
    public enum STATE { SETUP, ENTERINTRO, INTRO, ENTERBOX, TITLE, EXITTITLE, EXIT }

    public STATE state;
    public float introRectAlpha;
    public TextLabel label;
    public MeshInfo introMesh;
    public MeshInfo boxMesh;
    public MeshInfo cartMesh;
    public MeshAnimator meshAnimator = new MeshAnimator();

    public float rectY;
    public float field_3C;
    public float meshScale;
    public float rotationY;
    public float x;
    public float field_4C;
    public float field_50;
    public float rotationZ;
    public float matrixY;
    public float matrixZ;
    public Matrix renderMatrix;
    public Matrix renderMatrix2;
    public Matrix matrixTemp;
    public int logoTextureId;
    public int introTextureId;
    public int logoAlpha;
    public int skipButtonAlpha;
    public int field_12C;
    public byte field_130;

    public override void Create()
    {
        int heading = 0;
        int labelTex = 0;
        int textTex = 0;

        state = STATE.SETUP;
        introRectAlpha = 320.0f;
        logoTextureId = Renderer.LoadTexture("Data/Game/Menu/SonicLogo.png", TEXFMT.RGBA8888);

        //Font.ResetBitmapFonts();
        heading = Renderer.LoadTexture("Data/Game/Menu/Heading_EN@1x.png", TEXFMT.RGBA4444);
        Font.LoadBitmapFont("Data/Game/Menu/Heading_EN.fnt", FONT.HEADING, heading);
        labelTex = Renderer.LoadTexture("Data/Game/Menu/Label_EN@1x.png", TEXFMT.RGBA4444);
        Font.LoadBitmapFont("Data/Game/Menu/Label_EN.fnt", FONT.LABEL, labelTex);
        textTex = Renderer.LoadTexture("Data/Game/Menu/Text_EN.png", TEXFMT.RGBA4444);
        Font.LoadBitmapFont("Data/Game/Menu/Text_EN.fnt", FONT.TEXT, textTex);

        label = (TextLabel)Objects.CreateNativeObject(() => new TextLabel());
        label.fontId = FONT.HEADING;
        label.scale = 0.15f;
        label.alpha = 256;
        label.state = TextLabel.STATE.NONE;
        if (Engine.deviceType == DEVICE.MOBILE)
            label.text = Font.GetCharactersForString(Strings.strTouchToStart, FONT.HEADING);
        else
            label.text = Font.GetCharactersForString(Strings.strPressStart, FONT.HEADING);

        label.SetAlignment(ALIGN.CENTER);

        label.x = 64.0f;
        label.y = -96.0f;

        introTextureId = Renderer.LoadTexture("Data/Game/Menu/Intro.png", TEXFMT.RGBA5551);
        int package = 0;
        switch (Engine.globalBoxRegion)
        {
            case REGION.JP:
                package = Renderer.LoadTexture("Data/Game/Models/Package_JP.png", TEXFMT.RGBA5551);
                introMesh = Renderer.LoadMesh("Data/Game/Models/Intro.bin", introTextureId);
                boxMesh = Renderer.LoadMesh("Data/Game/Models/JPBox.bin", package);
                cartMesh = Renderer.LoadMesh("Data/Game/Models/JPCartridge.bin", package);
                break;
            case REGION.US:
                package = Renderer.LoadTexture("Data/Game/Models/Package_US.png", TEXFMT.RGBA5551);
                introMesh = Renderer.LoadMesh("Data/Game/Models/Intro.bin", introTextureId);
                boxMesh = Renderer.LoadMesh("Data/Game/Models/Box.bin", package);
                cartMesh = Renderer.LoadMesh("Data/Game/Models/Cartridge.bin", package);
                break;
            case REGION.EU:
                package = Renderer.LoadTexture("Data/Game/Models/Package_EU.png", TEXFMT.RGBA5551);
                introMesh = Renderer.LoadMesh("Data/Game/Models/Intro.bin", introTextureId);
                boxMesh = Renderer.LoadMesh("Data/Game/Models/Box.bin", package);
                cartMesh = Renderer.LoadMesh("Data/Game/Models/Cartridge.bin", package);
                break;
        }

        Renderer.SetMeshAnimation(boxMesh, meshAnimator, 16, 16, 0.0f);
        Renderer.AnimateMesh(boxMesh, meshAnimator);
        Renderer.SetMeshAnimation(introMesh, meshAnimator, 0, 36, 0.09f);
        rectY = 160.0f;
        meshScale = 0.0f;
        rotationY = 0.0f;
        Audio.SetMusicTrack("MenuIntro.ogg", 0, false, 0);
        Audio.SetMusicTrack("MainMenu.ogg", 1, true, 106596);
        Renderer.LoadTexture("Data/Game/Menu/Circle.png", TEXFMT.RGBA4444);
        Renderer.LoadTexture("Data/Game/Menu/BG1.png", TEXFMT.RGBA4444);
        Renderer.LoadTexture("Data/Game/Menu/ArrowButtons.png", TEXFMT.RGBA4444);
        if (Engine.deviceType == DEVICE.MOBILE)
            Renderer.LoadTexture("Data/Game/Menu/VirtualDPad.png", TEXFMT.RGBA8888);
        else
            Renderer.LoadTexture("Data/Game/Menu/Generic.png", TEXFMT.RGBA8888);
        Renderer.LoadTexture("Data/Game/Menu/PlayerSelect.png", TEXFMT.RGBA8888);
        Renderer.LoadTexture("Data/Game/Menu/SegaID.png", TEXFMT.RGBA8888);
    }

    public override void Main()
    {

        switch (state)
        {
            case STATE.SETUP:
                {
                    Audio.PlayMusic(0, 0);
                    state = STATE.ENTERINTRO;
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (int)introRectAlpha);
                    break;
                }
            case STATE.ENTERINTRO:
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    meshAnimator.animationSpeed = 6.0f * Engine.deltaTime;
                    Renderer.AnimateMesh(introMesh, meshAnimator);
                    Renderer.RenderMesh(introMesh, MESH.NORMALS, true);
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);

                    if (Engine.deviceType == DEVICE.MOBILE && skipButtonAlpha < 0x100 && introRectAlpha < 0.0)
                    {
                        skipButtonAlpha += 8;
                    }
                    Renderer.RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    introRectAlpha -= 300.0f * Engine.deltaTime;
                    if (introRectAlpha < -320.0)
                        state = STATE.INTRO;
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (int)introRectAlpha);

                    Input.CheckKeyDown(ref Input.keyDown);
                    Input.CheckKeyPress(ref Input.keyPress);

                    if (Input.CheckTouchRect(SCREEN_CENTERX_F - 32.0f, 104.0f, 20.0f, 20.0f) >= 0 || Input.keyPress.start || Input.keyPress.A)
                    {
                        state = STATE.TITLE;
                        x = -96.0f;
                        meshScale = 1.0f;
                        rectY = -48.0f;
                        field_12C = 256;
                        logoAlpha = 256;
                        field_130 = 1;
                        label.alpha = 256;
                        label.state = TextLabel.STATE.BLINK;
                    }
                    break;
                }
            case STATE.INTRO:
                {
                    Input.CheckKeyDown(ref Input.keyDown);
                    Input.CheckKeyPress(ref Input.keyPress);
                    Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    meshAnimator.animationSpeed = 6.0f * Engine.deltaTime;
                    Renderer.AnimateMesh(introMesh, meshAnimator);
                    Renderer.RenderMesh(introMesh, MESH.NORMALS, true);
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    if (meshAnimator.frameId > 26)
                        state = STATE.ENTERBOX;

                    if (Input.CheckTouchRect(SCREEN_CENTERX_F - 32.0f, 104.0f, 20.0f, 20.0f) >= 0 || Input.keyPress.start || Input.keyPress.A)
                    {
                        state = STATE.TITLE;
                        x = -96.0f;
                        meshScale = 1.0f;
                        rectY = -48.0f;
                        field_12C = 256;
                        logoAlpha = 256;
                        field_130 = 1;
                        label.alpha = 256;
                        label.state = TextLabel.STATE.BLINK;
                    }
                    break;
                }
            case STATE.ENTERBOX:
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);

                    float y = 0;
                    if (rectY > -48.0f)
                    {
                        rectY -= 300.0f * Engine.deltaTime;
                        if (rectY >= -48.0f)
                        {
                            y = rectY + 240.0f;
                        }
                        else
                        {
                            rectY = -48.0f;
                            y = 192.0f;
                        }
                    }
                    else
                    {
                        y = rectY + 240.0f;
                    }
                    Renderer.RenderRect(-SCREEN_CENTERX_F, y, 160.0f, SCREEN_XSIZE_F, 256.0f, 160, 192, 255, 255);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);
                    meshAnimator.animationSpeed = 6.0f * Engine.deltaTime;
                    Renderer.AnimateMesh(introMesh, meshAnimator);
                    Renderer.RenderMesh(introMesh, MESH.NORMALS, true);

                    if (meshScale < 1.0f)
                    {
                        meshScale += 0.75f * Engine.deltaTime;
                        if (meshScale > 1.0f)
                            meshScale = 1.0f;
                    }
                    else
                    {
                        label.state = TextLabel.STATE.BLINK;
                        state = STATE.TITLE;
                        x = 0.0f;
                    }
                    rotationY += Engine.deltaTime;

                    renderMatrix = Matrix.CreateScale(meshScale);
                    matrixTemp = Helpers.CreateRotationY(rotationY);
                    renderMatrix = renderMatrix * matrixTemp;
                    matrixTemp = Matrix.CreateTranslation(0, 0, 200);
                    renderMatrix = renderMatrix * matrixTemp;

                    Renderer.SetRenderMatrix(renderMatrix);
                    Renderer.RenderMesh(boxMesh, MESH.NORMALS, true);
                    Renderer.SetRenderMatrix(null);
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    break;
                }
            case STATE.TITLE:
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, Drawing.SCREEN_CENTERY, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, rectY + 240.0f, 160.0f, SCREEN_XSIZE_F, 256.0f, 160, 192, 255, 255);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);

                    rotationY += Engine.deltaTime;
                    if (rotationY > MathHelper.TwoPi)
                        rotationY -= MathHelper.TwoPi;

                    if (x <= -96.0)
                    {
                        if (logoAlpha > 255)
                        {
                            Input.CheckKeyDown(ref Input.keyDown);
                            Input.CheckKeyPress(ref Input.keyPress);
                            if (Input.keyPress.start || Input.touches > 0 || Input.keyPress.A)
                            {
                                //if (field_130 != 0)
                                //{
                                Audio.PlaySfxByName("Menu Select", false);
                                Audio.StopMusic(true);
                                label.state = TextLabel.STATE.BLINK_FAST;
                                introRectAlpha = 0.0f;
                                state = STATE.EXITTITLE;
                                //}
                            }
                            else
                            {
                                field_130 = 0;
                            }
                        }
                        else
                        {
                            logoAlpha += 8;
                            label.alpha += 8;
                        }
                    }
                    else
                    {
                        x += (-97.0f - x) / (Engine.deltaTime * 60.0f * 16.0f);
                    }
                    Renderer.NewRenderState();
                    renderMatrix = Matrix.CreateScale(meshScale);
                    matrixTemp = Helpers.CreateRotationY(rotationY);
                    renderMatrix = renderMatrix * matrixTemp;
                    matrixTemp = Matrix.CreateTranslation(x, 0, 200);
                    renderMatrix = renderMatrix * matrixTemp;

                    Renderer.SetRenderMatrix(renderMatrix);
                    Renderer.RenderMesh(boxMesh, MESH.NORMALS, true);
                    Renderer.SetRenderMatrix(null);
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);

                    if (skipButtonAlpha > 0)
                    {
                        skipButtonAlpha -= 8;
                    }
                    Renderer.RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    Renderer.RenderImage(64.0f, 32.0f, 160.0f, 0.3f, 0.3f, 256.0f, 128.0f, 512.0f, 256.0f, 0.0f, 0.0f, logoAlpha, (byte)logoTextureId);

                    if (field_12C > 0)
                    {
                        field_12C -= 32;
                        Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, field_12C);
                    }
                    break;
                }
            case STATE.EXITTITLE:
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, rectY + 240.0f, 160.0f, SCREEN_XSIZE_F, 256.0f, 160, 192, 255, 255);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);

                    float div = 60.0f * Engine.deltaTime * 1.125f;
                    x /= div;
                    rotationY /= div;
                    Renderer.NewRenderState();
                    renderMatrix = Helpers.CreateRotationY(rotationY);
                    matrixTemp = Matrix.CreateTranslation(0, 0, 200);
                    renderMatrix = renderMatrix * matrixTemp;
                    Renderer.SetRenderMatrix(renderMatrix);
                    Renderer.RenderMesh(boxMesh, MESH.NORMALS, true);
                    Renderer.SetRenderMatrix(null);
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);

                    if (logoAlpha > 0)
                        logoAlpha -= 8;

                    introRectAlpha += Engine.deltaTime;
                    if (introRectAlpha > 1.0)
                    {
                        state = STATE.EXIT;
                        Objects.RemoveNativeObject(label);
                        Renderer.SetMeshAnimation(boxMesh, meshAnimator, 4, 16, 0.0f);
                        meshAnimator.animationTimer = 0.0f;
                        meshAnimator.frameId = 16;
                        matrixZ = 200.0f;
                        field_3C = 4.0f;
                        rotationZ = MathHelper.ToRadians(-90.0f);
                    }
                    Renderer.RenderImage(64.0f, 32.0f, 160.0f, 0.3f, 0.3f, 256.0f, 128.0f, 512.0f, 256.0f, 0.0f, 0.0f, logoAlpha, (byte)logoTextureId);
                    break;
                }
            case STATE.EXIT:
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.NONE);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_CENTERY_F - rectY, 160, 192, 255, 255);
                    Renderer.RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);
                    meshAnimator.animationSpeed = -16.0f * Engine.deltaTime;
                    Renderer.AnimateMesh(boxMesh, meshAnimator);

                    float val = 60.0f * Engine.deltaTime;
                    float val2 = 0.125f * val;

                    field_3C = field_3C - val2 - val2;
                    rectY += val * (field_3C - val2);
                    if (meshAnimator.frameId <= 7)
                    {
                        if (rotationY < 1.0)
                            rotationY += Engine.deltaTime;
                        field_50 = field_50 - val2 - val2;
                        field_4C += val * (field_50 - val2);
                        matrixY += (16.0f - matrixY) / (val * 16.0f);

                        matrixZ += (152.0f - matrixZ) / (val * 16.0f);
                        rotationZ += (0.0f - rotationZ) / (val * 22.0f);
                    }
                    Renderer.NewRenderState();
                    renderMatrix = Matrix.CreateRotationX(rotationY);
                    renderMatrix *= Matrix.CreateTranslation(x, field_4C, 200.0f);

                    Renderer.SetRenderMatrix(renderMatrix);
                    Renderer.RenderMesh(boxMesh, MESH.NORMALS, true);

                    renderMatrix2 = Matrix.CreateRotationZ(rotationZ);
                    renderMatrix2 *= Matrix.CreateTranslation(0, matrixY, matrixZ);
                    Renderer.SetRenderMatrix(renderMatrix2);
                    Renderer.RenderMesh(cartMesh, MESH.NORMALS, true);
                    Renderer.SetRenderMatrix(null);
                    if (field_4C < -360.0)
                    {
                        //ShowPromoPopup(0, "BootupPromo");
                        //ResetNativeObject(entity, MenuControl_Create, MenuControl_Main);
                        Objects.ResetNativeObject(this, () => new MenuControl());
                    }
                    break;
                }
            default: break;
        }
    }

}
