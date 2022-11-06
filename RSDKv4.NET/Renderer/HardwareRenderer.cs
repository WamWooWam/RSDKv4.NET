using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static RSDKv4.Drawing;
using static RSDKv4.Palette;
using static RSDKv4.Scene;

namespace RSDKv4.Render;

public class HardwareRenderer : IRenderer
{
    private static readonly Color MAX_COLOR
        = new Color(255, 255, 255, 255);

    private Game _game;
    private GraphicsDevice _device;

#if FAST_PALETTE
    private Effect _effect;
#else
    private BasicEffect _effect;
#endif
    private RenderTarget2D _renderTarget;
    private RasterizerState _noScissorState;
    private RasterizerState _scissorState;

    private BlendState[] _blendStates;

    // 1024x1024 atlases
#if FAST_PALETTE
    private Texture2D _surface;
    private Texture2D[] _palettes = new Texture2D[PALETTE_COUNT];
#else
    private Texture2D[] _textures = new Texture2D[SURFACE_LIMIT];
#endif

    // only used to flip the display
    private SpriteBatch _spriteBatch;

    private Rectangle _screenRect;
    private Matrix _projection2D;

#if ENABLE_3D
    private Matrix _projection3D;
#endif

    public DrawVertex[] vertexList = new DrawVertex[VERTEX_LIMIT];
    public ushort vertexCount = 0;
    public short[] indexList = new short[INDEX_LIMIT];
    public ushort indexCount = 0;

    public DrawBlendState[] drawBlendStates = new DrawBlendState[256];
    public int drawBlendStateIdx = 0;

    public float[] tileUVList = new float[TILEUV_SIZE];

#if FAST_PALETTE
    public byte[] textureBuffer = new byte[SURFACE_SIZE * SURFACE_SIZE];
#else
    public ushort[] textureBuffer = new ushort[SURFACE_SIZE * SURFACE_SIZE];
#endif
    public byte textureBufferMode = 0;

    public static float PIXEL_TO_UV = 1.0f / (float)SURFACE_SIZE;

    private List<ShaderDef> _retroShaders;

    public HardwareRenderer(Game game, GraphicsDevice device)
    {
        _game = game;
        _device = device;

#if FAST_PALETTE
        _effect = game.Content.Load<Effect>("Shaders/Palette");
        _surface = new Texture2D(device, SURFACE_SIZE, SURFACE_SIZE, false, SurfaceFormat.Alpha8);
        for (int i = 0; i < PALETTE_COUNT; i++)
            _palettes[i] = new Texture2D(device, 16, 16, false, SurfaceFormat.Bgra5551);
#else
        _effect = new BasicEffect(device) { TextureEnabled = true };
        for (int index = 0; index < SURFACE_LIMIT; ++index)
            _textures[index] = new Texture2D(device, SURFACE_SIZE, SURFACE_SIZE, false, SurfaceFormat.Bgra5551);
#endif

        _retroShaders = new List<ShaderDef>();
        _retroShaders.Add(new(game.Content.Load<Effect>("Shaders/None"), SamplerState.PointClamp));
        _retroShaders.Add(new(game.Content.Load<Effect>("Shaders/Clean"), SamplerState.LinearClamp));
        _retroShaders.Add(new(game.Content.Load<Effect>("Shaders/CRT-Yeetron"), SamplerState.LinearClamp));
        _retroShaders.Add(new(game.Content.Load<Effect>("Shaders/CRT-Yee64"), SamplerState.LinearClamp));

        _renderTarget = new RenderTarget2D(device, SCREEN_XSIZE, SCREEN_YSIZE, false, SurfaceFormat.Bgr565, DepthFormat.None, 0, RenderTargetUsage.PlatformContents);
        _noScissorState = new RasterizerState() { CullMode = CullMode.None };
        _scissorState = new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true };
        _device.RasterizerState = _noScissorState;

        _spriteBatch = new SpriteBatch(device);

        _blendStates = new BlendState[5];
        _blendStates[(int)BlendMode.None] = BlendState.Opaque;
        _blendStates[(int)BlendMode.Alpha] = BlendState.NonPremultiplied;
        _blendStates[(int)BlendMode.Additive] = BlendState.Additive;
        _blendStates[(int)BlendMode.Subtractive] = new BlendState()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.ReverseSubtract,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.ReverseSubtract
        };

        SetScreenDimensions(device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight);
        SetupPolygonLists();
    }

    public void SetScreenDimensions(int width, int height)
    {
        NativeRenderer.SetScreenDimensions(width, height);

        Input.touchWidth = width;
        Input.touchHeight = height;
        var viewWidth = width;
        var viewHeight = height;
        var bufferWidth = (int)((float)viewWidth / (float)viewHeight * 240f);
        bufferWidth += 8;
        bufferWidth = bufferWidth >> 4 << 4;
        if (bufferWidth > 400)
            bufferWidth = 400;
        //var viewAspect = 0.75f;

        if (viewHeight >= 480)
        {
            SetScreenRenderSize(bufferWidth, bufferWidth);
            bufferWidth *= 2;
            //var bufferHeight = 480;
        }
        else
        {
            //var bufferHeight = 240;
            SetScreenRenderSize(bufferWidth, bufferWidth);
        }

        var orthWidth = SCREEN_XSIZE * 16;
        _projection2D = Matrix.CreateOrthographicOffCenter(4f, (float)(orthWidth + 4), 3844f, 4f, 0.0f, 100f);
#if ENABLE_3D
        _projection3D = Matrix.CreatePerspectiveFieldOfView(1.832596f, viewAspect, 0.1f, 2000f) * Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0.0f, -0.045f, 0.0f);
#endif

        //var ratio = Math.Min((double)viewWidth / SCREEN_XSIZE, (double)viewHeight / SCREEN_YSIZE);
        //var realWidth = SCREEN_XSIZE * ratio;
        //var realHeight = SCREEN_YSIZE * ratio;
        //var x = (viewWidth - realWidth) / 2;
        //var y = (viewHeight - realHeight) / 2;

        //_screenRect = new Rectangle((int)x, (int)y, (int)realWidth, (int)realHeight);

        _screenRect = new Rectangle(0, 0, viewWidth, viewHeight);
    }

    public void SetScreenRenderSize(int width, int lineSize)
    {
        //SCREEN_XSIZE = width;
        //SCREEN_CENTERX = width / 2;

        GFX_LINESIZE = lineSize;
        GFX_LINESIZE_MINUSONE = lineSize - 1;
        GFX_LINESIZE_DOUBLE = 2 * lineSize;

        SCREEN_SCROLL_LEFT = SCREEN_CENTERX - 8;
        SCREEN_SCROLL_RIGHT = SCREEN_CENTERX + 8;

        Objects.OBJECT_BORDER_X1 = 128;
        Objects.OBJECT_BORDER_X2 = SCREEN_XSIZE + 128;
    }

    public void UpdateSurfaces()
    {
#if FAST_PALETTE
        UpdateTextureBufferWithTiles();
        UpdateTextureBufferWithSortedSprites();
        _surface.SetData(textureBuffer);

        for (byte paletteNum = 0; paletteNum < PALETTE_COUNT; ++paletteNum)
        {
            fullPalette[paletteNum][255] = RGB_16BIT5551(0xFF, 0xFF, 0xFF, 1);
            _palettes[paletteNum].SetData(fullPalette[paletteNum]);
        }

#else
        SetActivePalette(0, 0, SCREEN_YSIZE);
        UpdateTextureBufferWithTiles();
        UpdateTextureBufferWithSortedSprites();
        _textures[0].SetData(textureBuffer);

        for (byte paletteNum = 1; paletteNum < 6; ++paletteNum)
        {
            SetActivePalette(paletteNum, 0, SCREEN_YSIZE);
            UpdateTextureBufferWithTiles();
            UpdateTextureBufferWithSortedSprites();
            _textures[paletteNum].SetData(textureBuffer);
        }
        SetActivePalette((byte)0, 0, SCREEN_YSIZE);
#endif
    }

    public void UpdateActivePalettes()
    {
#if FAST_PALETTE
        fullPalette[texPaletteNum][255] = RGB_16BIT5551(0xFF, 0xFF, 0xFF, 1);
        _palettes[texPaletteNum].SetData(fullPalette[texPaletteNum]);

        for (int i = 0; i < activePaletteCount; i++)
        {
            var palette = activePalettes[i];

            if ((palette.endLine - palette.startLine) == 0 || palette.paletteNum == texPaletteNum)
                continue;

            fullPalette[palette.paletteNum][255] = RGB_16BIT5551(0xFF, 0xFF, 0xFF, 1);
            _palettes[palette.paletteNum].SetData(fullPalette[palette.paletteNum]);
        }
#else
        UpdateTextureBufferWithTiles();
        //UpdateTextureBufferWithSprites();
        UpdateTextureBufferWithSortedSprites();
        _textures[texPaletteNum].SetData(textureBuffer);
#endif
    }

    private int frame = 0;
    public void Draw()
    {
        frame++;
        _device.SetRenderTarget(_renderTarget);
        _device.Clear(Color.Black);

        if (surfaceDirty)
        {
            Debug.WriteLine($"{frame} Updating surfaces");
            UpdateSurfaces();
            surfaceDirty = false;
        }

#if FAST_PALETTE
        if (paletteDirty)
        {
            // Debug.WriteLine($"{frame} Updating palettes");
            UpdateActivePalettes();
            paletteDirty = false;
        }
#endif

#if FAST_PALETTE
        _device.SamplerStates[0] = SamplerState.PointClamp;
        _device.SamplerStates[1] = SamplerState.PointWrap;

        _effect.Parameters["Texture"].SetValue(_surface);
        _effect.Parameters["Palette"].SetValue(_palettes[texPaletteNum]);
        _effect.Parameters["MatrixTransform"].SetValue(_projection2D);
#else
        _device.SamplerStates[0] = SamplerState.PointClamp;

        _effect.Texture = _textures[texPaletteNum];
        _effect.World = Matrix.Identity;
        _effect.View = Matrix.Identity;
        _effect.Projection = _projection2D;
        _effect.LightingEnabled = false;
        _effect.VertexColorEnabled = true;
#endif

        _device.BlendState = BlendState.Opaque;
        _device.RasterizerState = activePaletteCount > 1 ? _scissorState : _noScissorState;

        for (int i = 0; i < Math.Max(activePaletteCount, 1); i++)
        {
            if (activePaletteCount > 0)
            {
                var palette = activePalettes[i];

                if ((palette.endLine - palette.startLine) == 0)
                    continue;

                _device.ScissorRectangle = new Rectangle(0, palette.startLine, SCREEN_XSIZE, palette.endLine - palette.startLine);

#if FAST_PALETTE
                _effect.Parameters["Palette"].SetValue(_palettes[palette.paletteNum]);
#else
                _effect.Texture = _textures[palette.paletteNum];
#endif
            }

#if ENABLE_3D
        if (Drawing.isRender3DEnabled)
        {
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.BlendState = BlendState.Opaque;
                _device.SamplerStates[0] = SamplerState.PointClamp;
                if (Drawing.indexCountOpaque > (ushort)0)
                    _effect.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Drawing.vertexList, 0, (int)Drawing.vertexCountOpaque, Drawing.indexList, 0, (int)Drawing.indexCountOpaque);
            }
            _device.BlendState = BlendState.NonPremultiplied;
            _effect.World = Matrix.CreateTranslation(Drawing.floor3DPos) * Matrix.CreateRotationY((float)(3.14159274101257 * (180.0 + (double)Drawing.floor3DAngle) / 180.0));
            _effect.Projection = _projection3D;
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                if (Drawing.indexCount3D > (ushort)0)
                    _effect.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Drawing.vertexList3D, 0, (int)Drawing.vertexCount3D, Drawing.indexList, 0, (int)Drawing.indexCount3D);
            }
            _effect.World = Matrix.Identity;
            _effect.Projection = _projection2D;
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int primitiveCount = (int)Drawing.indexCount - (int)Drawing.indexCountOpaque;
                if (primitiveCount > 0)
                    _effect.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Drawing.vertexList, (int)Drawing.vertexCountOpaque, (int)Drawing.vertexCount - (int)Drawing.vertexCountOpaque, Drawing.indexList, 0, primitiveCount);
            }
        }
        else
        {
#endif

            for (int j = 0; j < drawBlendStateIdx; j++)
            {
                var drawList = drawBlendStates[j];

                if (drawList.vertexCount == 0 || drawList.indexCount == 0)
                    continue;

                _device.BlendState = _blendStates[(int)drawList.blendMode];

                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexList, drawList.vertexOffset, drawList.vertexCount, indexList, 0, drawList.indexCount);
                }
            }

#if ENABLE_3D
        }
#endif
        }


#if FAST_PALETTE
        _device.Textures[0] = null;
        _device.Textures[1] = null;
#else
        _device.Textures[0] = null;
#endif
        _device.SetRenderTarget(null);
    }

    public void Present()
    {
        var shader = _retroShaders[0];
        shader.effect.Parameters["pixelSize"].SetValue(new Vector2(SCREEN_XSIZE, SCREEN_YSIZE));
        shader.effect.Parameters["textureSize"].SetValue(new Vector2(SCREEN_XSIZE, SCREEN_YSIZE));
        shader.effect.Parameters["viewSize"].SetValue(new Vector2(_screenRect.Width, _screenRect.Height));

        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, shader.samplerState, DepthStencilState.None, RasterizerState.CullNone, shader.effect);       
        _spriteBatch.Draw(_renderTarget, _screenRect, Color.White);
        _spriteBatch.End();
    }

    public Texture2D CopyRetroBuffer()
    {
        // no copying here sir
        return _renderTarget;
    }

    public void Reset()
    {
        textureBufferMode = 0;
        for (int i = 0; i < LAYER_COUNT; i++)
        {
            if (stageLayouts[i].type == LAYER.THREEDSKY)
                textureBufferMode = 1;
        }

        for (int i = 0; i < hParallax.entryCount; i++)
        {
            if (hParallax.deform[i] != 0)
                textureBufferMode = 1;
        }

        if (tilesetGFXData[0x32002] > 0)
            textureBufferMode = 0;

        if (textureBufferMode != 0)
        {
            for (int i = 0; i < TILEUV_SIZE; i += 4)
            {
                tileUVList[i + 0] = (i >> 2) % 28 * 18 + 1;
                tileUVList[i + 1] = (i >> 2) / 28 * 18 + 1;
                tileUVList[i + 2] = tileUVList[i + 0] + 16;
                tileUVList[i + 3] = tileUVList[i + 1] + 16;
            }
            tileUVList[TILEUV_SIZE - 4] = 487.0f;
            tileUVList[TILEUV_SIZE - 3] = 487.0f;
            tileUVList[TILEUV_SIZE - 2] = 503.0f;
            tileUVList[TILEUV_SIZE - 1] = 503.0f;
        }
        else
        {
            for (int i = 0; i < TILEUV_SIZE; i += 4)
            {
                tileUVList[i + 0] = (i >> 2 & 31) * 16;
                tileUVList[i + 1] = (i >> 2 >> 5) * 16;
                tileUVList[i + 2] = tileUVList[i + 0] + 16;
                tileUVList[i + 3] = tileUVList[i + 1] + 16;
            }
        }

        for (int i = 0; i < TILEUV_SIZE; i++)
        {
            tileUVList[i] *= PIXEL_TO_UV;
        }

        surfaceDirty = true;

        indexCount = 0;
        vertexCount = 0;
        drawBlendStateIdx = 0;
        drawListEntries[0] = new DrawListEntry();
    }

    public void BeginDraw()
    {
        vertexCount = 0;
        indexCount = 0;

        drawBlendStates[0] = new DrawBlendState();
        drawBlendStateIdx = 0;

        activePalettes[0] = new PaletteEntry(0, 0, SCREEN_YSIZE);
        activePaletteCount = 0;
    }

    public void EndDraw()
    {
        drawBlendStates[drawBlendStateIdx].vertexCount = vertexCount - drawBlendStates[drawBlendStateIdx].vertexOffset;
        drawBlendStates[drawBlendStateIdx].indexCount = indexCount - drawBlendStates[drawBlendStateIdx].indexOffset;
        drawBlendStateIdx++;
    }

    public void EnsureBlendMode(BlendMode mode)
    {
        if (drawBlendStates[drawBlendStateIdx].blendMode != mode)
        {
            drawBlendStates[drawBlendStateIdx].vertexCount = vertexCount - drawBlendStates[drawBlendStateIdx].vertexOffset;
            drawBlendStates[drawBlendStateIdx].indexCount = indexCount - drawBlendStates[drawBlendStateIdx].indexOffset;
            drawBlendStateIdx++;

            drawBlendStates[drawBlendStateIdx].vertexOffset = vertexCount;
            drawBlendStates[drawBlendStateIdx].indexOffset = indexCount;
            drawBlendStates[drawBlendStateIdx].blendMode = mode;
        }
    }


    public void Copy16x16Tile(int dest, int src)
    {
        src <<= 2;
        dest <<= 2;
        if (src < tileUVList.Length && dest < tileUVList.Length)
            Array.Copy(tileUVList, src, tileUVList, dest, 4);
    }

    public void UpdateTextureBufferWithTiles()
    {
        var cnt = 0;
        var bufPos = 0;
        var currentPalette = fullPalette[texPaletteNum];
        var tilesetGFXData = Drawing.tilesetGFXData;
        var textureBuffer = this.textureBuffer;

        if (textureBufferMode == 0)
        {
            for (int h = 0; h < 512; h += 16)
            {
                for (int w = 0; w < 512; w += 16)
                {
                    int dataPos = cnt << 8;
                    cnt++;
                    bufPos = w + (h * SURFACE_SIZE);
                    for (int y = 0; y < TILE_SIZE; y++)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
                        textureBuffer[bufPos + 1] = tilesetGFXData[dataPos + 1];
                        textureBuffer[bufPos + 2] = tilesetGFXData[dataPos + 2];
                        textureBuffer[bufPos + 3] = tilesetGFXData[dataPos + 3];
                        textureBuffer[bufPos + 4] = tilesetGFXData[dataPos + 4];
                        textureBuffer[bufPos + 5] = tilesetGFXData[dataPos + 5];
                        textureBuffer[bufPos + 6] = tilesetGFXData[dataPos + 6];
                        textureBuffer[bufPos + 7] = tilesetGFXData[dataPos + 7];
                        textureBuffer[bufPos + 8] = tilesetGFXData[dataPos + 8];
                        textureBuffer[bufPos + 9] = tilesetGFXData[dataPos + 9];
                        textureBuffer[bufPos + 10] = tilesetGFXData[dataPos + 10];
                        textureBuffer[bufPos + 11] = tilesetGFXData[dataPos + 11];
                        textureBuffer[bufPos + 12] = tilesetGFXData[dataPos + 12];
                        textureBuffer[bufPos + 13] = tilesetGFXData[dataPos + 13];
                        textureBuffer[bufPos + 14] = tilesetGFXData[dataPos + 14];
                        textureBuffer[bufPos + 15] = tilesetGFXData[dataPos + 15];

#else
                        textureBuffer[bufPos] =  currentPalette[tilesetGFXData[dataPos]];
                        textureBuffer[bufPos + 1] =  currentPalette[tilesetGFXData[dataPos + 1]];
                        textureBuffer[bufPos + 2] =  currentPalette[tilesetGFXData[dataPos + 2]];
                        textureBuffer[bufPos + 3] =  currentPalette[tilesetGFXData[dataPos + 3]];
                        textureBuffer[bufPos + 4] =  currentPalette[tilesetGFXData[dataPos + 4]];
                        textureBuffer[bufPos + 5] =  currentPalette[tilesetGFXData[dataPos + 5]];
                        textureBuffer[bufPos + 6] =  currentPalette[tilesetGFXData[dataPos + 6]];
                        textureBuffer[bufPos + 7] =  currentPalette[tilesetGFXData[dataPos + 7]];
                        textureBuffer[bufPos + 8] =  currentPalette[tilesetGFXData[dataPos + 8]];
                        textureBuffer[bufPos + 9] =  currentPalette[tilesetGFXData[dataPos + 9]];
                        textureBuffer[bufPos + 10] =  currentPalette[tilesetGFXData[dataPos + 10]];
                        textureBuffer[bufPos + 11] =  currentPalette[tilesetGFXData[dataPos + 11]];
                        textureBuffer[bufPos + 12] =  currentPalette[tilesetGFXData[dataPos + 12]];
                        textureBuffer[bufPos + 13] =  currentPalette[tilesetGFXData[dataPos + 13]];
                        textureBuffer[bufPos + 14] =  currentPalette[tilesetGFXData[dataPos + 14]];
                        textureBuffer[bufPos + 15] =  currentPalette[tilesetGFXData[dataPos + 15]];
#endif
                        dataPos += 16;
                        bufPos += SURFACE_SIZE;
                    }
                }
            }
        }
        else
        {
            for (int h = 0; h < 504; h += 18)
            {
                for (int w = 0; w < 504; w += 18)
                {
                    int dataPos = cnt << 8;
                    cnt++;
                    if (cnt == 783)
                        cnt = 1023;

                    bufPos = w + (h * SURFACE_SIZE);
#if FAST_PALETTE
                    textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                    textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    bufPos++;

                    for (int l = 0; l < 15; l++)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
                        dataPos++;
                    }

                    if (tilesetGFXData[dataPos] > 0)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    }
                    else
                    {
                        textureBuffer[bufPos] = 0;
                        bufPos++;
                        textureBuffer[bufPos] = 0;
                    }
                    bufPos++;
                    dataPos -= 15;
                    bufPos += 1006;

                    for (int k = 0; k < 16; k++)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
                        // TODO: is it worth unrolling these inner loops?
                        for (int l = 0; l < 15; l++)
                        {
#if FAST_PALETTE
                            textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                            textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                            bufPos++;
                            dataPos++;
                        }

                        if (tilesetGFXData[dataPos] > 0)
                        {
#if FAST_PALETTE
                            textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                            textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                            bufPos++;
#if FAST_PALETTE
                            textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                            textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        }
                        else
                        {
                            textureBuffer[bufPos] = 0;
                            bufPos++;
                            textureBuffer[bufPos] = 0;
                        }
                        bufPos++;
                        dataPos++;
                        bufPos += 1006;
                    }
                    dataPos -= 16;
#if FAST_PALETTE
                    textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                    textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    bufPos++;

                    for (int l = 0; l < 15; l++)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
                        dataPos++;
                    }

                    if (tilesetGFXData[dataPos] > 0)
                    {
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                        bufPos++;
#if FAST_PALETTE
                        textureBuffer[bufPos] = tilesetGFXData[dataPos];
#else
                        textureBuffer[bufPos] = currentPalette[tilesetGFXData[dataPos]];
#endif
                    }
                    else
                    {
                        textureBuffer[bufPos] = 0;
                        bufPos++;
                        textureBuffer[bufPos] = 0;
                    }
                    bufPos++;
                    bufPos += 1006;
                }
            }
        }

        bufPos = 0;
        for (int k = 0; k < TILE_SIZE; k++)
        {
            for (int l = 0; l < TILE_SIZE; l++)
            {
#if FAST_PALETTE
                textureBuffer[bufPos] = 255;
#else
                textureBuffer[bufPos] = RGB_16BIT5551(0xFF, 0xFF, 0xFF, 1);
#endif
                bufPos++;
            }
            bufPos += 1008;
        }
    }

    public void UpdateTextureBufferWithSprites()
    {
        var currentPalette = fullPalette[texPaletteNum];

        for (int i = 0; i < SURFACE_MAX; ++i)
        {
            SurfaceDesc surface = surfaces[i];
            if (surface.texStartY + surface.height <= SURFACE_SIZE && surface.texStartX > -1)
            {
                int pos = surface.dataPosition;
                int teXPos = surface.texStartX + (surface.texStartY * SURFACE_SIZE);
                for (int j = 0; j < surface.height; j++)
                {
                    for (int k = 0; k < surface.width; k++)
                    {
#if FAST_PALETTE
                        textureBuffer[teXPos] = graphicsBuffer[pos];
#else
                        textureBuffer[teXPos] = currentPalette[graphicsBuffer[pos]];
#endif

                        teXPos++;
                        pos++;
                    }
                    teXPos += SURFACE_SIZE - surface.width;
                }
            }
        }
    }

    public void UpdateTextureBufferWithSortedSprites()
    {
        var currentPalette = fullPalette[texPaletteNum];

        byte surfCnt = 0;
        byte[] surfList = new byte[SURFACE_MAX];
        bool flag = true;
        for (int i = 0; i < SURFACE_MAX; i++) surfaces[i].texStartX = -1;

        for (int i = 0; i < SURFACE_MAX; i++)
        {
            int gfxSize = 0;
            int surfID = -1;
            for (int s = 0; s < SURFACE_MAX; s++)
            {
                var surface = surfaces[s];
                if (surface != null && surface.texStartX == -1)
                {
                    if (CheckSurfaceSize(surface.width) && CheckSurfaceSize(surface.height))
                    {
                        if (surface.width + surface.height > gfxSize)
                        {
                            gfxSize = surface.width + surface.height;
                            surfID = s;
                        }
                    }
                    else
                    {
                        surface.texStartX = 0;
                    }
                }
            }

            if (surfID == -1)
            {
                i = SURFACE_MAX;
            }
            else
            {
                surfaces[surfID].texStartX = 0;
                surfList[surfCnt++] = (byte)surfID;
            }
        }

        for (int i = 0; i < SURFACE_MAX; i++)
            surfaces[i].texStartX = -1;

        for (int i = 0; i < surfCnt; i++)
        {
            var curSurface = surfaces[surfList[i]];
            curSurface.texStartX = 0;
            curSurface.texStartY = 0;
            bool loopFlag = true;
            while (loopFlag)
            {
                loopFlag = false;
                if (curSurface.height == SURFACE_SIZE)
                    flag = false;

                if (flag)
                {
                    if (curSurface.texStartX < 512 && curSurface.texStartY < 512)
                    {
                        loopFlag = true;
                        curSurface.texStartX += curSurface.width;
                        if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                        {
                            curSurface.texStartX = 0;
                            curSurface.texStartY += curSurface.height;
                        }
                    }
                    else
                    {
                        for (int s = 0; s < SURFACE_MAX; s++)
                        {
                            var surface = surfaces[s];
                            if (surface.texStartX > -1 && s != surfList[i] && curSurface.texStartX < surface.texStartX + surface.width
                                && curSurface.texStartX >= surface.texStartX && curSurface.texStartY < surface.texStartY + surface.height)
                            {
                                loopFlag = true;
                                curSurface.texStartX += curSurface.width;
                                if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                                {
                                    curSurface.texStartX = 0;
                                    curSurface.texStartY += curSurface.height;
                                }
                                s = SURFACE_MAX;
                            }
                        }
                    }
                }
                else
                {
                    if (curSurface.width < SURFACE_SIZE)
                    {
                        if (curSurface.texStartX < 16 && curSurface.texStartY < 16)
                        {
                            loopFlag = true;
                            curSurface.texStartX += curSurface.width;
                            if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                            {
                                curSurface.texStartX = 0;
                                curSurface.texStartY += curSurface.height;
                            }
                        }
                        else
                        {
                            for (int s = 0; s < SURFACE_MAX; s++)
                            {
                                var surface = surfaces[s];
                                if (surface.texStartX > -1 && s != surfList[i] && curSurface.texStartX < surface.texStartX + surface.width
                                    && curSurface.texStartX >= surface.texStartX && curSurface.texStartY < surface.texStartY + surface.height)
                                {
                                    loopFlag = true;
                                    curSurface.texStartX += curSurface.width;
                                    if (curSurface.texStartX + curSurface.width > SURFACE_SIZE)
                                    {
                                        curSurface.texStartX = 0;
                                        curSurface.texStartY += curSurface.height;
                                    }
                                    s = SURFACE_MAX;
                                }
                            }
                        }
                    }
                }
            }

            if (curSurface.texStartY + curSurface.height <= SURFACE_SIZE)
            {
                int gfXPos = curSurface.dataPosition;
                int dataPos = curSurface.texStartX + (curSurface.texStartY * SURFACE_SIZE);
                for (int h = 0; h < curSurface.height; h++)
                {
                    for (int w = 0; w < curSurface.width; w++)
                    {
#if FAST_PALETTE
                        textureBuffer[dataPos] = graphicsBuffer[gfXPos];
#else
                        textureBuffer[dataPos] = currentPalette[graphicsBuffer[gfXPos]];
#endif
                        dataPos++;
                        gfXPos++;
                    }
                    dataPos += SURFACE_SIZE - curSurface.width;
                }
            }
        }
    }

    public void SetupPolygonLists()
    {
        int vID = 0;
        for (int i = 0; i < VERTEX_LIMIT; ++i)
        {
            indexList[vID++] = (short)((i << 2) + 0);
            indexList[vID++] = (short)((i << 2) + 1);
            indexList[vID++] = (short)((i << 2) + 2);
            indexList[vID++] = (short)((i << 2) + 1);
            indexList[vID++] = (short)((i << 2) + 3);
            indexList[vID++] = (short)((i << 2) + 2);

            vertexList[i].color = MAX_COLOR;
        }

#if ENABLE_3D
    for (int index2 = 0; index2 < VERTEX3D_LIMIT; ++index2)
        vertexList3D[index2].color = MAX_COLOR;
#endif
    }

    public void ClearScreen(byte clearColour)
    {
        vertexList[vertexCount].position.X = 0.0f;
        vertexList[vertexCount].position.Y = 0.0f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = SCREEN_XSIZE << 4;
        vertexList[vertexCount].position.Y = 0.0f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = 0.0f;
        vertexList[vertexCount].position.Y = 3840f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = SCREEN_XSIZE << 4;
        vertexList[vertexCount].position.Y = 3840f;
        vertexList[vertexCount].color.R = fullPalette32[texPaletteNum][clearColour].R;
        vertexList[vertexCount].color.G = fullPalette32[texPaletteNum][clearColour].G;
        vertexList[vertexCount].color.B = fullPalette32[texPaletteNum][clearColour].B;
        vertexList[vertexCount].color.A = byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawAdditiveBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Additive);

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;

        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;

        var color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);

        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawAlphaBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;
        if (alpha < 0)
            alpha = 0;

        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;

        var color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color.R = byte.MaxValue;
        vertexList[vertexCount].color.G = byte.MaxValue;
        vertexList[vertexCount].color.B = byte.MaxValue;
        vertexList[vertexCount].color.A = 128;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawFadedQuad(Quad2D face, uint colour, uint fogColour, int alpha)
    {
        EnsureBlendMode(BlendMode.Alpha);

        if (vertexCount >= VERTEX_LIMIT)
            return;

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;

        byte cr = (byte)((colour >> 16) & 0xFF);
        byte cg = (byte)((colour >> 8) & 0xFF);
        byte cb = (byte)((colour >> 0) & 0xFF);
        byte fr = (byte)((fogColour >> 16) & 0xFF);
        byte fg = (byte)((fogColour >> 8) & 0xFF);
        byte fb = (byte)((fogColour >> 0) & 0xFF);

        var col = new Color(((ushort)(fr * (0xFF - alpha) + alpha * cr) >> 8), ((ushort)(fg * (0xFF - alpha) + alpha * cg) >> 8), ((ushort)(fb * (0xFF - alpha) + alpha * cb) >> 8), 0xff);

        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = col;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = col;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color = col;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = col;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        indexCount += 2;
    }
    public void DrawHLineScrollLayer(byte layerNum)
    {
        int num1 = 0;
        int[] gfxDataPos = tiles128x128.gfxDataPos;
        byte[] direction = tiles128x128.direction;
        byte[] visualPlane = tiles128x128.visualPlane;
        TileLayer layer = stageLayouts[activeTileLayers[layerNum]];

        int layerWidth = (int)layer.xsize;
        int layerHeight = (int)layer.ysize;

        int num4 = (SCREEN_XSIZE >> 4) + 3;
        byte aboveMidPoint = layerNum < tLayerMidPoint ? (byte)0 : (byte)1;

        ushort[] tileMap = layer.tiles;
        byte[] lineScrollRef;
        int deformationDataOffset;
        int deformationDataWOffset;
        int[] deformationData;
        int[] deformationDataW;
        int yscrollOffset;

        if (activeTileLayers[layerNum] != 0)
        {
            // BG Layer
            int yScroll = Scene.yScrollOffset * layer.parallaxFactor >> 8;
            int fullheight = layerHeight << 7;
            layer.scrollPos += layer.scrollSpeed;
            if (layer.scrollPos > fullheight << 16)
                layer.scrollPos -= fullheight << 16;
            yscrollOffset = (yScroll + (layer.scrollPos >> 16)) % fullheight;
            layerHeight = fullheight >> 7;
            lineScrollRef = layer.lineScroll;
            deformationDataOffset = (byte)(yscrollOffset + layer.deformationOffset);
            deformationDataWOffset = (byte)(yscrollOffset + waterDrawPos + layer.deformationOffsetW);
            deformationData = bgDeformationData2;
            deformationDataW = bgDeformationData3;
        }
        else
        {
            // FG Layer
            lastXSize = layer.xsize;
            yscrollOffset = yScrollOffset;
            lineScrollRef = layer.lineScroll;
            for (int i = 0; i < PARALLAX_COUNT; ++i) hParallax.linePos[i] = xScrollOffset;
            deformationDataOffset = (byte)(yscrollOffset + layer.deformationOffset);
            deformationDataWOffset = (byte)(yscrollOffset + waterDrawPos + layer.deformationOffsetW);
            deformationData = bgDeformationData0;
            deformationDataW = bgDeformationData1;
        }

        if (layer.type == LAYER.HSCROLL)
        {
            if (lastXSize != layerWidth)
            {
                int fullLayerwidth = layerWidth << 7;
                for (int i = 0; i < hParallax.entryCount; ++i)
                {
                    hParallax.linePos[i] = xScrollOffset * hParallax.parallaxFactor[i] >> 8;
                    if (hParallax.scrollPos[i] > fullLayerwidth << 16)
                        hParallax.scrollPos[i] -= fullLayerwidth << 16;
                    if (hParallax.scrollPos[i] < 0)
                        hParallax.scrollPos[i] += fullLayerwidth << 16;
                    hParallax.linePos[i] += hParallax.scrollPos[i] >> 16;
                    hParallax.linePos[i] %= fullLayerwidth;
                }
            }
            int w = -1;
            if (activeTileLayers[layerNum] != 0)
                w = layerWidth;
            lastXSize = w;
        }

        if (yscrollOffset < 0)
            yscrollOffset += layerHeight << 7;

        int deformY = yscrollOffset >> 4 << 4;
        int lineIdx = num1 + deformY;
        int deformOffset = deformationDataOffset + (deformY - yscrollOffset);
        int deformOffsetW = deformationDataWOffset + (deformY - yscrollOffset);
        if (deformOffset < 0)
            deformOffset += 256;
        if (deformOffsetW < 0)
            deformOffsetW += 256;
        deformY = -(yscrollOffset & 15);
        int num13 = yscrollOffset >> 7;
        int num14 = (yscrollOffset & sbyte.MaxValue) >> 4;
        waterDrawPos <<= 4;
        deformY = deformY << 4;
        for (int i1 = deformY != 0 ? 272 : 256; i1 > 0; i1 -= 16)
        {
            int parallaxLinePos = hParallax.linePos[lineScrollRef[lineIdx]] - 16;
            int lineIdx1 = lineIdx + 8;
            bool flag;
            if (parallaxLinePos == hParallax.linePos[lineScrollRef[lineIdx1]] - 16)
            {
                if (hParallax.deform[(int)lineScrollRef[lineIdx1]] == (byte)1)
                {
                    int deformX1 = deformY < waterDrawPos ? deformationData[deformOffset] : deformationDataW[deformOffsetW];
                    int deformX1Offset = deformOffset + 8;
                    int deformY1Offset = deformOffsetW + 8;
                    int deformY1 = deformY + 64 <= waterDrawPos ? deformationData[deformX1Offset] : deformationDataW[deformY1Offset];
                    flag = deformX1 != deformY1;
                    deformOffset = deformX1Offset - 8;
                    deformOffsetW = deformY1Offset - 8;
                }
                else
                    flag = false;
            }
            else
                flag = true;

            int lineIdx2 = lineIdx1 - 8;
            if (flag)
            {
                int num10 = layerWidth << 7;
                if (parallaxLinePos < 0)
                    parallaxLinePos += num10;
                if (parallaxLinePos >= num10)
                    parallaxLinePos -= num10;
                int chunkPosX = parallaxLinePos >> 7;
                int chunkTileX = (parallaxLinePos & sbyte.MaxValue) >> 4;
                int deformX1 = -((parallaxLinePos & 15) << 4) - 256;
                int deformX2 = deformX1;
                int index6;
                int index7;
                if (hParallax.deform[lineScrollRef[lineIdx2]] == 1)
                {
                    if (deformY >= waterDrawPos)
                        deformX1 -= deformationDataW[deformOffsetW];
                    else
                        deformX1 -= deformationData[deformOffset];
                    index6 = deformOffset + 8;
                    index7 = deformOffsetW + 8;
                    if (deformY + 64 > waterDrawPos)
                        deformX2 -= deformationDataW[index7];
                    else
                        deformX2 -= deformationData[index6];
                }
                else
                {
                    index6 = deformOffset + 8;
                    index7 = deformOffsetW + 8;
                }
                int index9 = lineIdx2 + 8;
                int index10 = (chunkPosX <= -1 || num13 <= -1 ? 0 : tileMap[chunkPosX + (num13 << 8)] << 6) + (chunkTileX + (num14 << 3));
                for (int i2 = num4; i2 > 0; --i2)
                {
                    if (visualPlane[index10] == aboveMidPoint && gfxDataPos[index10] > 0)
                    {
                        int num21 = 0;
                        switch (direction[index10])
                        {
                            case 0:
                                vertexList[vertexCount].position.X = deformX1;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num22 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num22];
                                int num23 = num22 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX1 + 256;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num23];
                                int num24 = num23 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX2;
                                vertexList[vertexCount].position.Y = deformY + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num24] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX2 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 1:
                                vertexList[vertexCount].position.X = deformX1 + 256;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num25 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num25];
                                int num26 = num25 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX1;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num26];
                                int num27 = num26 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX2 + 256;
                                vertexList[vertexCount].position.Y = deformY + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num27] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX2;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 2:
                                vertexList[vertexCount].position.X = deformX2;
                                vertexList[vertexCount].position.Y = deformY + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num28 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num28] + 1f / 128f;
                                int num29 = num28 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX2 + 256;
                                vertexList[vertexCount].position.Y = deformY + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num29];
                                int num30 = num29 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX1;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num30];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX1 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 3:
                                vertexList[vertexCount].position.X = deformX2 + 256;
                                vertexList[vertexCount].position.Y = deformY + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num21];
                                int num31 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num31] + 1f / 128f;
                                int num32 = num31 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX2;
                                vertexList[vertexCount].position.Y = deformY + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index10] + num32];
                                int num33 = num32 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX1 + 256;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index10] + num33];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = deformX1;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                        }
                    }
                    deformX1 += 256;
                    deformX2 += 256;
                    ++chunkTileX;
                    if (chunkTileX > 7)
                    {
                        ++chunkPosX;
                        if (chunkPosX == layerWidth)
                            chunkPosX = 0;
                        chunkTileX = 0;
                        index10 = (tileMap[chunkPosX + (num13 << 8)] << 6) + (chunkTileX + (num14 << 3));
                    }
                    else
                        ++index10;
                }
                int num34 = deformY + 128;
                int num35 = hParallax.linePos[lineScrollRef[index9]] - 16;
                int num36 = layerWidth << 7;
                if (num35 < 0)
                    num35 += num36;
                if (num35 >= num36)
                    num35 -= num36;
                int num37 = num35 >> 7;
                int num38 = (num35 & sbyte.MaxValue) >> 4;
                int num39 = -((num35 & 15) << 4) - 256;
                int num40 = num39;
                if (hParallax.deform[lineScrollRef[index9]] == 1)
                {
                    if (num34 >= waterDrawPos)
                        num39 -= deformationDataW[index7];
                    else
                        num39 -= deformationData[index6];
                    deformOffset = index6 + 8;
                    deformOffsetW = index7 + 8;
                    if (num34 + 64 > waterDrawPos)
                        num40 -= deformationDataW[deformOffsetW];
                    else
                        num40 -= deformationData[deformOffset];
                }
                else
                {
                    deformOffset = index6 + 8;
                    deformOffsetW = index7 + 8;
                }
                lineIdx = index9 + 8;
                int index12 = (num37 <= -1 || num13 <= -1 ? 0 : tileMap[num37 + (num13 << 8)] << 6) + (num38 + (num14 << 3));
                for (int index11 = num4; index11 > 0; --index11)
                {
                    if (visualPlane[index12] == aboveMidPoint && gfxDataPos[index12] > 0)
                    {
                        int num21 = 0;
                        switch (direction[index12])
                        {
                            case 0:
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num22 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num22] + 1f / 128f;
                                int num23 = num22 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num23];
                                int num24 = num23 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num24];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 1:
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num25 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num25] + 1f / 128f;
                                int num26 = num25 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num26];
                                int num27 = num26 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num27];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 2:
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num28 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num28];
                                int num29 = num28 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num29];
                                int num30 = num29 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num30] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 3:
                                vertexList[vertexCount].position.X = num40 + 256;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num21];
                                int num31 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num31];
                                int num32 = num31 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num40;
                                vertexList[vertexCount].position.Y = num34 + 128;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index12] + num32];
                                int num33 = num32 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39 + 256;
                                vertexList[vertexCount].position.Y = num34;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index12] + num33] - 1f / 128f;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num39;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                        }
                    }
                    num39 += 256;
                    num40 += 256;
                    ++num38;
                    if (num38 > 7)
                    {
                        ++num37;
                        if (num37 == layerWidth)
                            num37 = 0;
                        num38 = 0;
                        index12 = (tileMap[num37 + (num13 << 8)] << 6) + (num38 + (num14 << 3));
                    }
                    else
                        ++index12;
                }
                deformY = num34 + 128;
            }
            else
            {
                int num10 = layerWidth << 7;
                if (parallaxLinePos < 0)
                    parallaxLinePos += num10;
                if (parallaxLinePos >= num10)
                    parallaxLinePos -= num10;
                int num17 = parallaxLinePos >> 7;
                int num18 = (parallaxLinePos & sbyte.MaxValue) >> 4;
                int num19 = -((parallaxLinePos & 15) << 4) - 256;
                int num20 = num19;
                if (hParallax.deform[lineScrollRef[lineIdx2]] == 1)
                {
                    if (deformY >= waterDrawPos)
                        num19 -= deformationDataW[deformOffsetW];
                    else
                        num19 -= deformationData[deformOffset];
                    deformOffset += 16;
                    deformOffsetW += 16;
                    if (deformY + 128 > waterDrawPos)
                        num20 -= deformationDataW[deformOffsetW];
                    else
                        num20 -= deformationData[deformOffset];
                }
                else
                {
                    deformOffset += 16;
                    deformOffsetW += 16;
                }
                lineIdx = lineIdx2 + 16;
                int index6 = (num17 <= -1 || num13 <= -1 ? 0 : tileMap[num17 + (num13 << 8)] << 6) + (num18 + (num14 << 3));
                for (int index7 = num4; index7 > 0; --index7)
                {
                    if (visualPlane[index6] == aboveMidPoint && gfxDataPos[index6] > 0)
                    {
                        int num21 = 0;
                        switch (direction[index6])
                        {
                            case 0:
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num22 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num22];
                                int num23 = num22 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num23];
                                int num24 = num23 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = deformY + 256;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num24];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 1:
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num25 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num25];
                                int num26 = num25 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num26];
                                int num27 = num26 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = deformY + 256;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num27];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 2:
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = deformY + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num28 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num28];
                                int num29 = num28 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = deformY + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num29];
                                int num30 = num29 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num30];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                            case 3:
                                vertexList[vertexCount].position.X = num20 + 256;
                                vertexList[vertexCount].position.Y = deformY + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num21];
                                int num31 = num21 + 1;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num31];
                                int num32 = num31 + 1;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num20;
                                vertexList[vertexCount].position.Y = deformY + 256;
                                vertexList[vertexCount].texCoord.X = tileUVList[gfxDataPos[index6] + num32];
                                int num33 = num32 + 1;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19 + 256;
                                vertexList[vertexCount].position.Y = deformY;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = tileUVList[gfxDataPos[index6] + num33];
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                vertexList[vertexCount].position.X = num19;
                                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                                vertexList[vertexCount].color = MAX_COLOR;
                                ++vertexCount;
                                indexCount += 2;
                                break;
                        }
                    }
                    num19 += 256;
                    num20 += 256;
                    ++num18;
                    if (num18 > 7)
                    {
                        ++num17;
                        if (num17 == layerWidth)
                            num17 = 0;
                        num18 = 0;
                        index6 = (tileMap[num17 + (num13 << 8)] << 6) + (num18 + (num14 << 3));
                    }
                    else
                        ++index6;
                }
                deformY += 256;
            }
            ++num14;
            if (num14 > 7)
            {
                ++num13;
                if (num13 == layerHeight)
                {
                    num13 = 0;
                    lineIdx -= layerHeight << 7;
                }
                num14 = 0;
            }
        }
        waterDrawPos >>= 4;
    }

    public void DrawQuad(Quad2D face, int rgbVal)
    {
        if (vertexCount >= VERTEX_LIMIT)
            return;
        rgbVal = (rgbVal & 2130706432) >> 23;
        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color.R = (byte)(rgbVal >> 16 & byte.MaxValue);
        vertexList[vertexCount].color.G = (byte)(rgbVal >> 8 & byte.MaxValue);
        vertexList[vertexCount].color.B = (byte)(rgbVal & byte.MaxValue);
        vertexList[vertexCount].color.A = rgbVal <= 253 ? (byte)rgbVal : byte.MaxValue;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawRectangle(int xPos, int yPos, int xSize, int ySize, int r, int g, int b, int alpha)
    {
        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;
        if (alpha < 0)
            alpha = 0;

        if (vertexCount >= VERTEX_LIMIT)
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color.R = (byte)r;
        vertexList[vertexCount].color.G = (byte)g;
        vertexList[vertexCount].color.B = (byte)b;
        vertexList[vertexCount].color.A = (byte)alpha;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.0f;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.0f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = vertexList[vertexCount - 1].color;
        vertexList[vertexCount].texCoord.X = 0.01f;
        vertexList[vertexCount].texCoord.Y = 0.01f;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawRotatedSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xBegin, int yBegin, int xSize, int ySize, int rotAngle, int surfaceNum)
    {
        xPos <<= 4;
        yPos <<= 4;
        rotAngle -= rotAngle >> 9 << 9;
        if (rotAngle < 0)
            rotAngle += 512;
        if (rotAngle != 0)
            rotAngle = 512 - rotAngle;
        int num1 = FastMath.Sin512(rotAngle);
        int num2 = FastMath.Cos512(rotAngle);
        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -8192 || xPos >= 13952) || (yPos <= -8192 || yPos >= 12032))
            return;
        if (direction == 0)
        {
            int num3 = -xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xSize - xPivot;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = -xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xSize - xPivot;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
        else
        {
            int num3 = xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xPivot - xSize;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xPivot - xSize;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
    }

    public void DrawRotoZoomSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xBegin, int yBegin, int xSize, int ySize, int rotAngle, int scale, int surfaceNum)
    {
        xPos <<= 4;
        yPos <<= 4;
        rotAngle -= rotAngle >> 9 << 9;
        if (rotAngle < 0)
            rotAngle += 512;
        if (rotAngle != 0)
            rotAngle = 512 - rotAngle;
        int num1 = FastMath.Sin512(rotAngle) * scale >> 9;
        int num2 = FastMath.Cos512(rotAngle) * scale >> 9;
        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -8192 || xPos >= 13952) || (yPos <= -8192 || yPos >= 12032))
            return;
        if (direction == 0)
        {
            int num3 = -xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xSize - xPivot;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = -xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xSize - xPivot;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
        else
        {
            int num3 = xPivot;
            int num4 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num3 * num2 + num4 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num4 * num2 - num3 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
            ++vertexCount;
            int num5 = xPivot - xSize;
            int num6 = -yPivot;
            vertexList[vertexCount].position.X = xPos + (num5 * num2 + num6 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num6 * num2 - num5 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            int num7 = xPivot;
            int num8 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num7 * num2 + num8 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num8 * num2 - num7 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
            ++vertexCount;
            int num9 = xPivot - xSize;
            int num10 = ySize - yPivot;
            vertexList[vertexCount].position.X = xPos + (num9 * num2 + num10 * num1 >> 5);
            vertexList[vertexCount].position.Y = yPos + (num10 * num2 - num9 * num1 >> 5);
            vertexList[vertexCount].color = MAX_COLOR;
            vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
            vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
            ++vertexCount;
            indexCount += 2;
        }
    }

    public void DrawScaledChar(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum)
    {
        if (vertexCount >= VERTEX_LIMIT || xPos <= -8192 || (xPos >= 13951 || yPos <= -1024) || yPos >= 4864)
            return;
        xPos -= xPivot * xScale >> 5;
        xScale = xSize * xScale >> 5;
        yPos -= yPivot * yScale >> 5;
        yScale = ySize * yScale >> 5;
        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1)
            return;
        vertexList[vertexCount].position.X = xPos;
        vertexList[vertexCount].position.Y = yPos;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xScale;
        vertexList[vertexCount].position.Y = yPos;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos;
        vertexList[vertexCount].position.Y = yPos + yScale;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawScaledSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum)
    {
        if (vertexCount >= VERTEX_LIMIT || xPos <= -512 || (xPos >= 872 || yPos <= -512) || yPos >= 752)
            return;
        xScale <<= 2;
        yScale <<= 2;
        xPos -= xPivot * xScale >> 11;
        xScale = xSize * xScale >> 11;
        yPos -= yPivot * yScale >> 11;
        yScale = ySize * yScale >> 11;
        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1)
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xScale << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + yScale << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawSpriteFlipped(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int direction, int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Alpha);

        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT)
            return;
        switch (direction)
        {
            case FLIP.NONE:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
            case FLIP.X:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
            case FLIP.Y:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
            case FLIP.XY:
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos + xSize << 4;
                vertexList[vertexCount].position.Y = yPos << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                vertexList[vertexCount].position.X = xPos << 4;
                vertexList[vertexCount].position.Y = yPos + ySize << 4;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
                ++vertexCount;
                vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
                vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
                vertexList[vertexCount].color = MAX_COLOR;
                vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
                vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
                ++vertexCount;
                break;
        }
        indexCount += 2;
    }

    public void DrawSubtractiveBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum)
    {
        EnsureBlendMode(BlendMode.Subtractive);

        if (alpha > byte.MaxValue)
            alpha = byte.MaxValue;

        var surfaceDesc = surfaces[surfaceNum];
        if (surfaceDesc.texStartX <= -1 || vertexCount >= VERTEX_LIMIT || (xPos <= -512 || xPos >= 872) || (yPos <= -512 || yPos >= 752))
            return;

        var color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);

        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos + xSize << 4;
        vertexList[vertexCount].position.Y = yPos << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + xBegin + xSize) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        vertexList[vertexCount].position.X = xPos << 4;
        vertexList[vertexCount].position.Y = yPos + ySize << 4;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + yBegin + ySize) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = vertexList[vertexCount - 2].position.X;
        vertexList[vertexCount].position.Y = vertexList[vertexCount - 1].position.Y;
        vertexList[vertexCount].color = color;
        vertexList[vertexCount].texCoord.X = vertexList[vertexCount - 2].texCoord.X;
        vertexList[vertexCount].texCoord.Y = vertexList[vertexCount - 1].texCoord.Y;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawTexturedBlendedQuad(Quad2D face, int surfaceNum)
    {
        if (vertexCount >= VERTEX_LIMIT)
            return;
        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[0].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[0].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[1].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[1].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[2].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[2].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[3].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[3].v) * PIXEL_TO_UV;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawTexturedQuad(Quad2D face, int surfaceNum)
    {

        if (vertexCount >= VERTEX_LIMIT)
            return;
        SurfaceDesc surfaceDesc = surfaces[surfaceNum];
        vertexList[vertexCount].position.X = face.vertex[0].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[0].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[0].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[0].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[1].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[1].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[1].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[1].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[2].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[2].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[2].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[2].v) * PIXEL_TO_UV;
        ++vertexCount;
        vertexList[vertexCount].position.X = face.vertex[3].x << 4;
        vertexList[vertexCount].position.Y = face.vertex[3].y << 4;
        vertexList[vertexCount].color = MAX_COLOR;
        vertexList[vertexCount].texCoord.X = (surfaceDesc.texStartX + face.vertex[3].u) * PIXEL_TO_UV;
        vertexList[vertexCount].texCoord.Y = (surfaceDesc.texStartY + face.vertex[3].v) * PIXEL_TO_UV;
        ++vertexCount;
        indexCount += 2;
    }

    public void DrawVLineScrollLayer(int layer)
    {
        Debug.WriteLine("DrawVLineScrollLayer({0})", layer);
    }

    public void Draw3DSkyLayer(int layer)
    {
        Debug.WriteLine("Draw3DSkyLayer({0})", layer);
    }

    public void Draw3DFloorLayer(int layer)
    {
        Debug.WriteLine("Draw3DFloorLayer({0})", layer);
    }

    public void DrawTintRectangle(int xPos, int yPos, int xSize, int ySize)
    {
        Debug.WriteLine("DrawTintRectangle({0},{1},{2},{3})", xPos, yPos, xSize, ySize);
    }

    public void DrawTintSpriteMask(
      int xPos,
      int yPos,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int tableNo,
      int surfaceNum)
    {
        Debug.WriteLine("DrawTintSpriteMask({0},{1},{2},{3},{4},{5},{6},{7})", xPos, yPos, xSize, ySize, xBegin, yBegin, tableNo, surfaceNum);
    }

    public void DrawScaledTintMask(
      byte direction,
      int xPos,
      int yPos,
      int xPivot,
      int yPivot,
      int xScale,
      int yScale,
      int xSize,
      int ySize,
      int xBegin,
      int yBegin,
      int surfaceNum)
    {
        Debug.WriteLine("DrawScaledTintMask({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11})", direction, xPos, yPos, xPivot, yPivot, xScale, yScale, xSize, ySize, xBegin, yBegin, surfaceNum);
    }
}
