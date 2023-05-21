using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static RSDKv4.Native.NativeGlobals;
using static RSDKv4.Native.NativeRenderer;
using static RSDKv4.Font;

namespace RSDKv4.Native;

public class SubMenuButton : NativeEntity
{
    public enum STATE { IDLE, FLASHING1, FLASHING2, SAVEBUTTON_UNSELECTED, SAVEBUTTON_SELECTED };

    public float matX;
    public float matY;
    public float matZ;
    public STATE state;
    public float matXOff;
    public float textY;
    public float afterFlashTimer;
    public float flashTimer;
    public float scale;
    public int alpha;
    public byte r;
    public byte g;
    public byte b;
    public ushort[] text;
    public MeshInfo meshButton;
    public MeshInfo meshButtonH;
    public bool useMatrix;
    public Matrix matrix;
    public Matrix renderMatrix;
    public byte symbol;
    public byte flags;
    public int textureSymbols;
    public bool useMeshH;

    public override void Create()
    {
        this.matZ = 160.0f;
        this.alpha = 0xFF;
        this.state = STATE.IDLE;
        this.matXOff = 0.0f;
        this.r = 0xFF;
        this.g = 0xFF;
        this.b = 0xFF;
        this.textureSymbols = Renderer.LoadTexture("Data/Game/Menu/Symbols.png", TEXFMT.RGBA4444);
        this.meshButton = Renderer.LoadMesh("Data/Game/Models/Button.bin", 255);
        this.meshButtonH = Renderer.LoadMesh("Data/Game/Models/ButtonH.bin", 255);
        Renderer.SetMeshVertexColors(this.meshButton, 0, 0, 0, 0xC0);
        Renderer.SetMeshVertexColors(this.meshButtonH, 0xA0, 0, 0, 0xC0);
    }

    public override void Main()
    {
        if (this.useMatrix)
        {
            Renderer.NewRenderState();
            this.renderMatrix = Matrix.CreateTranslation(this.matX - this.matXOff, this.matY, this.matZ)
                * this.matrix;
            Renderer.SetRenderMatrix(this.renderMatrix);
        }
        Renderer.SetRenderVertexColor(this.r, this.g, this.b);

        switch (this.state)
        {
            case STATE.IDLE:
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderMesh(this.meshButton, MESH.COLOURS, false);
                    Renderer.RenderText(this.text, FONT.LABEL, -80.0f, this.textY, 0, this.scale, this.alpha);
                    break;
                }
            case STATE.FLASHING1:
                {
                    this.flashTimer += Engine.deltaTime;
                    if (this.flashTimer > 1.0)
                        this.flashTimer -= 1.0f;

                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderMesh(this.meshButton, MESH.COLOURS, false);
                    if (this.flashTimer > 0.5)
                        Renderer.RenderText(this.text, FONT.LABEL, -80.0f, this.textY, 0, this.scale, this.alpha);
                    break;
                }
            case STATE.FLASHING2:
                {
                    this.flashTimer += Engine.deltaTime;
                    if (this.flashTimer > 0.1)
                        this.flashTimer -= 0.1f;
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    Renderer.RenderMesh(this.meshButton, MESH.COLOURS, false);
                    if (this.flashTimer > 0.05)
                        Renderer.RenderText(this.text, FONT.LABEL, -80.0f, this.textY, 0, this.scale, this.alpha);

                    this.afterFlashTimer += Engine.deltaTime;
                    if (this.afterFlashTimer > 0.5f)
                    {
                        this.afterFlashTimer = 0.0f;
                        this.state = STATE.IDLE;
                    }
                    break;
                }
            case STATE.SAVEBUTTON_UNSELECTED:
                {
                    this.flashTimer += Engine.deltaTime;
                    if (this.flashTimer > 0.1)
                        this.flashTimer -= 0.1f;

                    this.afterFlashTimer += Engine.deltaTime;
                    if (this.afterFlashTimer > 0.5)
                    {
                        this.flashTimer = 0.0f;
                        this.afterFlashTimer = 0.0f;
                        this.state = STATE.SAVEBUTTON_SELECTED;
                    }

                    goto case STATE.SAVEBUTTON_SELECTED;
                }
            case STATE.SAVEBUTTON_SELECTED:
                {
                    Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
                    if (this.useMeshH)
                        Renderer.RenderMesh(this.meshButtonH, MESH.COLOURS, false);
                    else
                        Renderer.RenderMesh(this.meshButton, MESH.COLOURS, false);
                    if (this.flashTimer < 0.05)
                        Renderer.RenderText(this.text, FONT.LABEL, -64.0f, this.textY, 0, this.scale, this.alpha);

                    switch (this.symbol)
                    {
                        case 0: Renderer.RenderImage(-76.0f, 0.0f, 0.0f, 0.3f, 0.35f, 28.0f, 43.0f, 56.0f, 86.0f, 0.0f, 170.0f, 255, this.textureSymbols); break;
                        case 1: Renderer.RenderImage(-76.0f, 0.0f, 0.0f, 0.3f, 0.35f, 34.0f, 43.0f, 68.0f, 86.0f, 58.0f, 170.0f, 255, this.textureSymbols); break;
                        case 2: Renderer.RenderImage(-76.0f, 0.0f, 0.0f, 0.3f, 0.35f, 29.0f, 43.0f, 58.0f, 86.0f, 130.0f, 170.0f, 255, this.textureSymbols); break;
                        case 3:
                            Renderer.RenderImage(-76.0f, 0.0f, 0.0f, 0.3f, 0.35f, 34.0f, 43.0f, 68.0f, 86.0f, 58.0f, 170.0f, 255, this.textureSymbols);
                            Renderer.RenderImage(-84.0f, 0.0f, 0.0f, 0.3f, 0.35f, 28.0f, 43.0f, 56.0f, 86.0f, 0.0f, 170.0f, 255, this.textureSymbols);
                            break;
                    }

                    uint[] emeraldColorsS1 = new uint[] { 0x8080FF, 0xFFFF00, 0xFF60C0, 0xA0FF00, 0xFF4060, 0xFFFFFF, };
                    uint[] emeraldColorsS2 = new uint[] { 0x00C0FF, 0x8000C0, 0xFF0000, 0xFF60FF, 0xFFC000, 0x60C000, 0xFFFFFF, };

                    float x = -60.0f;
                    for (int i = 0; i < (Engine.gameType == GAME.SONIC1 ? 6 : 7); ++i)
                    {
                        if ((this.flags & (1 << i)) != 0)
                        {
                            if (Engine.gameType == GAME.SONIC1)
                                Renderer.SetRenderVertexColor((byte)((emeraldColorsS1[i] >> 16) & 0xFF), (byte)((emeraldColorsS1[i] >> 8) & 0xFF), (byte)(emeraldColorsS1[i] & 0xFF));
                            else
                                Renderer.SetRenderVertexColor((byte)((emeraldColorsS2[i] >> 16) & 0xFF), (byte)((emeraldColorsS2[i] >> 8) & 0xFF), (byte)(emeraldColorsS2[i] & 0xFF));
                            Renderer.RenderImage(x, -6.0f, 0.0f, 0.125f, 0.125f, 28.0f, 35.0f, 56.0f, 70.0f, 188.0f, 0.0f, 255, this.textureSymbols);
                        }
                        else
                        {
                            Renderer.SetRenderVertexColor(0xFF, 0xFF, 0xFF);
                            Renderer.RenderImage(x, -6.0f, 0.0f, 0.125f, 0.125f, 28.0f, 35.0f, 56.0f, 70.0f, 133.0f, 0.0f, 255, this.textureSymbols);
                        }

                        x += 16.0f;
                    }
                    break;
                }
            default: break;
        }

        Renderer.SetRenderVertexColor(255, 255, 255);
        if (this.useMatrix)
        {
            Renderer.NewRenderState();
            Renderer.SetRenderMatrix(null);
        }
    }
}
