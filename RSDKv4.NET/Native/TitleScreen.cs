using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

internal class TitleScreen : NativeEntity
{
    private enum TITLESCREEN_STATE
    {
        SETUP, ENTERINTRO, INTRO, ENTERBOX, TITLE, EXITTITLE, EXIT
    }

    private TITLESCREEN_STATE state;
    private float introRectAlpha;
    private TextLabel labelPtr;
    private MeshInfo introMesh;
    private MeshInfo boxMesh;
    private MeshInfo cartMesh;
    private MeshAnimator meshAnimator = new MeshAnimator();

    private float rectY;
    private float field_3C;
    private float meshScale;
    private float rotationY;
    private float x;
    private float field_4C;
    private float field_50;
    private float rotationZ;
    private float matrixY;
    private float matrixZ;
    private Matrix renderMatrix;
    private Matrix renderMatrix2;
    private Matrix matrixTemp;
    private int logoTextureId;
    private int introTextureId;
    private int logoAlpha;
    private int skipButtonAlpha;
    private int field_12C;
    private byte field_130;

    public override void Create()
    {
        int heading = 0;
        int labelTex = 0;
        int textTex = 0;

        state = TITLESCREEN_STATE.SETUP;
        introRectAlpha = 320.0f;
        logoTextureId = LoadTexture("Data/Game/Menu/SonicLogo.png", TEXFMT.RGBA8888);

        //Font.ResetBitmapFonts();
        heading = LoadTexture("Data/Game/Menu/Heading_EN@1x.png", TEXFMT.RGBA4444);
        Font.LoadBitmapFont("Data/Game/Menu/Heading_EN.fnt", FONT.HEADING, heading);
        labelTex = LoadTexture("Data/Game/Menu/Label_EN@1x.png", TEXFMT.RGBA4444);
        Font.LoadBitmapFont("Data/Game/Menu/Label_EN.fnt", FONT.LABEL, labelTex);
        textTex = LoadTexture("Data/Game/Menu/Text_EN.png", TEXFMT.RGBA4444);
        Font.LoadBitmapFont("Data/Game/Menu/Text_EN.fnt", FONT.TEXT, textTex);

        labelPtr = (TextLabel)Objects.CreateNativeObject(() => new TextLabel());
        labelPtr.fontID = FONT.HEADING;
        labelPtr.scale = 0.15f;
        labelPtr.alpha = 256;
        labelPtr.state = TextLabel.TEXTLABEL_STATE.BLINK;
        //if (Engine.gameDeviceType == RETRO_MOBILE)
        //    SetStringToFont(labelPtr.text, strTouchToStart, FONT.HEADING);
        //else
        //    SetStringToFont(labelPtr.text, strPressStart, FONT.HEADING);

        labelPtr.text = Font.GetCharactersForString("PRESS START", FONT.HEADING);

        labelPtr.Align(ALIGN.CENTER);

        labelPtr.x = 64.0f;
        labelPtr.y = -96.0f;

        introTextureId = LoadTexture("Data/Game/Menu/Intro.png", TEXFMT.RGBA5551);
        int package = 0;
        switch (Engine.globalBoxRegion)
        {
            case REGION.JP:
                package = LoadTexture("Data/Game/Models/Package_JP.png", TEXFMT.RGBA5551);
                introMesh = LoadMesh("Data/Game/Models/Intro.bin", introTextureId);
                boxMesh = LoadMesh("Data/Game/Models/JPBox.bin", package);
                cartMesh = LoadMesh("Data/Game/Models/JPCartridge.bin", package);
                break;
            case REGION.US:
                package = LoadTexture("Data/Game/Models/Package_US.png", TEXFMT.RGBA5551);
                introMesh = LoadMesh("Data/Game/Models/Intro.bin", introTextureId);
                boxMesh = LoadMesh("Data/Game/Models/Box.bin", package);
                cartMesh = LoadMesh("Data/Game/Models/Cartridge.bin", package);
                break;
            case REGION.EU:
                package = LoadTexture("Data/Game/Models/Package_EU.png", TEXFMT.RGBA5551);
                introMesh = LoadMesh("Data/Game/Models/Intro.bin", introTextureId);
                boxMesh = LoadMesh("Data/Game/Models/Box.bin", package);
                cartMesh = LoadMesh("Data/Game/Models/Cartridge.bin", package);
                break;
        }

        SetMeshAnimation(boxMesh, meshAnimator, 16, 16, 0.0f);
        AnimateMesh(boxMesh, meshAnimator);
        SetMeshAnimation(introMesh, meshAnimator, 0, 36, 0.09f);
        rectY = 160.0f;
        meshScale = 0.0f;
        rotationY = 0.0f;
        Audio.SetMusicTrack("MenuIntro.ogg", 0, false, 0);
        Audio.SetMusicTrack("MainMenu.ogg", 1, true, 106596);
        LoadTexture("Data/Game/Menu/Circle.png", TEXFMT.RGBA4444);
        LoadTexture("Data/Game/Menu/BG1.png", TEXFMT.RGBA4444);
        LoadTexture("Data/Game/Menu/ArrowButtons.png", TEXFMT.RGBA4444);
        //if (Engine.deviceType == RETRO.MOBILE)
        //    LoadTexture("Data/Game/Menu/VirtualDPad.png", TEXFMT.RGBA8888);
        //else
        LoadTexture("Data/Game/Menu/Generic.png", TEXFMT.RGBA8888);
        LoadTexture("Data/Game/Menu/PlayerSelect.png", TEXFMT.RGBA8888);
        LoadTexture("Data/Game/Menu/SegaID.png", TEXFMT.RGBA8888);
    }

    public override void Main()
    {

        switch (state)
        {
            case TITLESCREEN_STATE.SETUP:
                {
                    Audio.PlayMusic(0, 0);
                    state = TITLESCREEN_STATE.ENTERINTRO;
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (byte)introRectAlpha);
                    break;
                }
            case TITLESCREEN_STATE.ENTERINTRO:
                {
                    SetRenderBlendMode(RENDER_BLEND.NONE);
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    meshAnimator.animationSpeed = 6.0f * Engine.deltaTime;
                    AnimateMesh(introMesh, meshAnimator);
                    RenderMesh(introMesh, MESH.NORMALS, true);
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);

                    //if (Engine.gameDeviceType == RETRO_MOBILE && skipButtonAlpha < 0x100 && introRectAlpha < 0.0)
                    //{
                    //    skipButtonAlpha += 8;
                    //}
                    RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    introRectAlpha -= (300.0f * Engine.deltaTime);
                    if (introRectAlpha < -320.0)
                        state = TITLESCREEN_STATE.INTRO;
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 0, 0, 0, (byte)introRectAlpha);

                    //CheckKeyDown(&inputDown);
                    //CheckKeyPress(&inputPress);

                    //if (CheckTouchRect(SCREEN_CENTERX_F - 32.0f, 104.0f, 20.0f, 20.0) >= 0 || (inputPress.start || inputPress.A))
                    //{
                    //    state = TITLESCREEN_STATE.TITLE;
                    //    x = -96.0;
                    //    meshScale = 1.0;
                    //    rectY = -48.0;
                    //    field_12C = 256;
                    //    logoAlpha = 256;
                    //    field_130 = 1;
                    //    NativeEntity_TextLabel* label = labelPtr;
                    //    label.alpha = 256;
                    //    label.state = TEXTLABEL_STATE_BLINK;
                    //}
                    break;
                }
            case TITLESCREEN_STATE.INTRO:
                {
                    Input.CheckKeyDown(ref Input.inputDown);
                    Input.CheckKeyPress(ref Input.inputPress);
                    SetRenderBlendMode(RENDER_BLEND.NONE);
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    meshAnimator.animationSpeed = 6.0f * Engine.deltaTime;
                    AnimateMesh(introMesh, meshAnimator);
                    RenderMesh(introMesh, MESH.NORMALS, true);
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    if (meshAnimator.frameId > 26)
                        state = TITLESCREEN_STATE.ENTERBOX;

                    if (/*CheckTouchRect(SCREEN_CENTERX_F - 32.0f, 104.0f, 20.0f, 20.0) >= 0 || */(Input.inputPress.start != 0 || Input.inputPress.A != 0))
                    {
                        state = TITLESCREEN_STATE.TITLE;
                        x = -96.0f;
                        meshScale = 1.0f;
                        rectY = -48.0f;
                        field_12C = 256;
                        logoAlpha = 256;
                        field_130 = 1;
                        labelPtr.alpha = 256;
                        labelPtr.state = TextLabel.TEXTLABEL_STATE.BLINK;
                    }
                    break;
                }
            case TITLESCREEN_STATE.ENTERBOX:
                {
                    SetRenderBlendMode(RENDER_BLEND.NONE);
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);

                    float y = 0;
                    if (rectY > -48.0f)
                    {
                        rectY -= (300.0f * Engine.deltaTime);
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
                    RenderRect(-SCREEN_CENTERX_F, y, 160.0f, SCREEN_XSIZE_F, 256.0f, 160, 192, 255, 255);
                    RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);
                    meshAnimator.animationSpeed = 6.0f * Engine.deltaTime;
                    AnimateMesh(introMesh, meshAnimator);
                    RenderMesh(introMesh, MESH.NORMALS, true);

                    if (meshScale < 1.0f)
                    {
                        meshScale += (0.75f * Engine.deltaTime);
                        if (meshScale > 1.0f)
                            meshScale = 1.0f;
                    }
                    else
                    {
                        //NativeEntity_TextLabel* label = labelPtr;
                        //label.state = TEXTLABEL_STATE_BLINK;
                        state = TITLESCREEN_STATE.TITLE;
                        x = 0.0f;
                    }
                    rotationY += Engine.deltaTime;

                    renderMatrix = Matrix.CreateScale(meshScale);
                    matrixTemp = Matrix.CreateRotationY(rotationY);
                    renderMatrix = renderMatrix * matrixTemp;
                    matrixTemp = Matrix.CreateTranslation(0, 0, 200);
                    renderMatrix = renderMatrix * matrixTemp;

                    SetRenderMatrix(renderMatrix);
                    RenderMesh(boxMesh, MESH.NORMALS, true);
                    SetRenderMatrix(null);
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    break;
                }
            case TITLESCREEN_STATE.TITLE:
                {
                    SetRenderBlendMode(RENDER_BLEND.NONE);
                    RenderRect(-SCREEN_CENTERX_F, Drawing.SCREEN_CENTERY, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    RenderRect(-SCREEN_CENTERX_F, rectY + 240.0f, 160.0f, SCREEN_XSIZE_F, 256.0f, 160, 192, 255, 255);
                    RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);

                    rotationY += Engine.deltaTime;
                    if (rotationY > MathHelper.TwoPi)
                        rotationY -= MathHelper.TwoPi;

                    if (x <= -96.0)
                    {
                        if (logoAlpha > 255)
                        {
                            Input.CheckKeyDown(ref Input.inputDown);
                            Input.CheckKeyPress(ref Input.inputPress);
                            if (Input.inputPress.start != 0 /*|| touches > 0*/ || Input.inputPress.A != 0)
                            {
                                //if (field_130 != 0)
                                //{
                                Audio.PlaySfxByName("Menu Select", false);
                                Audio.StopMusic(true);
                                labelPtr.state = TextLabel.TEXTLABEL_STATE.BLINK_FAST;
                                introRectAlpha = 0.0f;
                                state = TITLESCREEN_STATE.EXITTITLE;
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
                            labelPtr.alpha += 8;
                        }
                    }
                    else
                    {
                        x += ((-97.0f - x) / ((Engine.deltaTime * 60.0f) * 16.0f));
                    }
                    NewRenderState();
                    renderMatrix = Matrix.CreateScale(meshScale);
                    matrixTemp = Matrix.CreateRotationY(rotationY);
                    renderMatrix = renderMatrix * matrixTemp;
                    matrixTemp = Matrix.CreateTranslation(x, 0, 200);
                    renderMatrix = renderMatrix * matrixTemp;

                    SetRenderMatrix(renderMatrix);
                    RenderMesh(boxMesh, MESH.NORMALS, true);
                    SetRenderMatrix(null);
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);

                    if (skipButtonAlpha > 0)
                    {
                        skipButtonAlpha -= 8;
                    }
                    RenderImage(SCREEN_CENTERX_F - 32.0f, 104.0f, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 704.0f, 544.0f, skipButtonAlpha,
                                (byte)introTextureId);
                    RenderImage(64.0f, 32.0f, 160.0f, 0.3f, 0.3f, 256.0f, 128.0f, 512.0f, 256.0f, 0.0f, 0.0f, logoAlpha, (byte)logoTextureId);

                    if (field_12C > 0)
                    {
                        field_12C -= 32;
                        RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, field_12C);
                    }
                    break;
                }
            case TITLESCREEN_STATE.EXITTITLE:
                {
                    SetRenderBlendMode(RENDER_BLEND.NONE);
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    RenderRect(-SCREEN_CENTERX_F, rectY + 240.0f, 160.0f, SCREEN_XSIZE_F, 256.0f, 160, 192, 255, 255);
                    RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);

                    float div = (60.0f * Engine.deltaTime) * 1.125f;
                    x /= div;
                    rotationY /= div;
                    NewRenderState();
                    renderMatrix = Matrix.CreateRotationY(rotationY);
                    matrixTemp = Matrix.CreateTranslation(0, 0, 200);
                    renderMatrix = renderMatrix * matrixTemp;
                    SetRenderMatrix(renderMatrix);
                    RenderMesh(boxMesh, MESH.NORMALS, true);
                    SetRenderMatrix(null);
                    SetRenderBlendMode(RENDER_BLEND.ALPHA);

                    if (logoAlpha > 0)
                        logoAlpha -= 8;

                    introRectAlpha += Engine.deltaTime;
                    if (introRectAlpha > 1.0)
                    {
                        state = TITLESCREEN_STATE.EXIT;
                        Objects.RemoveNativeObject(labelPtr);
                        SetMeshAnimation(boxMesh, meshAnimator, 4, 16, 0.0f);
                        meshAnimator.animationTimer = 0.0f;
                        meshAnimator.frameId = 16;
                        matrixZ = 200.0f;
                        field_3C = 4.0f;
                        rotationZ = MathHelper.ToRadians(-90.0f);
                    }
                    RenderImage(64.0f, 32.0f, 160.0f, 0.3f, 0.3f, 256.0f, 128.0f, 512.0f, 256.0f, 0.0f, 0.0f, logoAlpha, (byte)logoTextureId);
                    break;
                }
            case TITLESCREEN_STATE.EXIT:
                {
                    SetRenderBlendMode(RENDER_BLEND.NONE);
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, 255, 255, 255, 255);
                    RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_CENTERY_F - rectY, 160, 192, 255, 255);
                    RenderRect(-SCREEN_CENTERX_F, rectY, 160.0f, SCREEN_XSIZE_F, 16.0f, 0, 0, 0, 255);
                    meshAnimator.animationSpeed = -16.0f * Engine.deltaTime;
                    AnimateMesh(boxMesh, meshAnimator);

                    float val = 60.0f * Engine.deltaTime;
                    float val2 = 0.125f * val;

                    field_3C = (field_3C - val2) - val2;
                    rectY += (val * (field_3C - val2));
                    if (meshAnimator.frameId <= 7)
                    {
                        if (rotationY < 1.0)
                            rotationY += Engine.deltaTime;
                        field_50 = (field_50 - val2) - val2;
                        field_4C += (val * (field_50 - val2));
                        matrixY += ((16.0f - matrixY) / (val * 16.0f));

                        matrixZ += ((152.0f - matrixZ) / (val * 16.0f));
                        rotationZ += ((0.0f - rotationZ) / (val * 22.0f));
                    }
                    NewRenderState();
                    renderMatrix = Matrix.CreateRotationX(rotationY);
                    renderMatrix *= Matrix.CreateTranslation(x, field_4C, 200.0f);

                    SetRenderMatrix(renderMatrix);
                    RenderMesh(boxMesh, MESH.NORMALS, true);

                    renderMatrix2 = Matrix.CreateRotationZ(rotationZ);
                    renderMatrix2 *= Matrix.CreateTranslation(0, matrixY, matrixZ);
                    SetRenderMatrix(renderMatrix2);
                    RenderMesh(cartMesh, MESH.NORMALS, true);
                    SetRenderMatrix(null);
                    if (field_4C < -360.0)
                    {
                        //ShowPromoPopup(0, "BootupPromo");
                        //ResetNativeObject(entity, MenuControl_Create, MenuControl_Main);
                        Objects.ResetNativeObject(this, () => new RetroGameLoop());
                    }
                    break;
                }
            default: break;
        }
    }

}
