using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.Native;
using RSDKv4.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using static RSDKv4.Drawing;

namespace RSDKv4;

public static class Renderer
{
    public const int SCREEN_XSIZE = 400;
    public const int SCREEN_CENTERX = SCREEN_XSIZE / 2;

    public const int SCREEN_YSIZE = 240;
    public const int SCREEN_CENTERY = SCREEN_YSIZE / 2;

    public static int GFX_LINESIZE = 0;
    public static int GFX_LINESIZE_MINUSONE = 0;
    public static int GFX_LINESIZE_DOUBLE = SCREEN_YSIZE / 2;

    public const int SURFACE_LIMIT = 6;
    public const int SURFACE_SIZE = 1024;
    public const int SURFACE_DATASIZE = SURFACE_SIZE * SURFACE_SIZE * sizeof(short);

    private static Game _game;
    private static GraphicsDevice _device;
    private static BasicEffect _effect;
    private static RenderTarget2D _renderTarget;
    private static RasterizerState _rasterizerState;

    // 1024x1024 atlases
    private static Texture2D[] _surfaces = new Texture2D[SURFACE_LIMIT];

    // only used to flip the display
    private static SpriteBatch _spriteBatch;

    private static Matrix _projection2D;
    private static Rectangle _screenRect;

#if ENABLE_3D
    private static Matrix _projection3D;
#endif

    public static int orthWidth;
    public static int viewWidth;
    public static int viewHeight;
    public static float viewAspect;
    public static int bufferWidth;
    public static int bufferHeight;

    public static Texture2D RenderTarget
        => _renderTarget;

    public static bool InitRenderDevice(Game game, GraphicsDevice device)
    {
        _game = game;
        _game.Window.Title = Engine.gameWindowText;

        _device = device;
        _effect = new BasicEffect(device) { TextureEnabled = true };

        for (int index = 0; index < 6; ++index)
            _surfaces[index] = new Texture2D(device, SURFACE_SIZE, SURFACE_SIZE, false, SurfaceFormat.Bgra5551);

        _renderTarget = new RenderTarget2D(device, SCREEN_XSIZE, SCREEN_YSIZE, false, SurfaceFormat.Bgr565, DepthFormat.Depth16, 0, RenderTargetUsage.PlatformContents);
        _rasterizerState = new RasterizerState() { CullMode = CullMode.None };
        _device.RasterizerState = _rasterizerState;

        _spriteBatch = new SpriteBatch(device);

        SetupPolygonLists();
        SetScreenDimensions(device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight);

        return true;
    }

    public static void SetScreenDimensions(int width, int height)
    {
        //InputSystem.touchWidth = width;
        //InputSystem.touchHeight = height;
        viewWidth = width;
        viewHeight = height;
        bufferWidth = (int)((float)viewWidth / (float)viewHeight * 240f);
        bufferWidth += 8;
        bufferWidth = bufferWidth >> 4 << 4;
        if (bufferWidth > 400)
            bufferWidth = 400;
        viewAspect = 0.75f;

        if (viewHeight >= 480)
        {
            SetScreenRenderSize(bufferWidth, bufferWidth);
            bufferWidth *= 2;
            bufferHeight = 480;
        }
        else
        {
            bufferHeight = 240;
            SetScreenRenderSize(bufferWidth, bufferWidth);
        }

        float aspect = (((width >> 16) * 65536.0f) + width) / (((height >> 16) * 65536.0f) + height);
        NativeRenderer.SCREEN_XSIZE_F = SCREEN_YSIZE * aspect;
        NativeRenderer.SCREEN_CENTERX_F = aspect * SCREEN_CENTERY;
        NativeRenderer.SetPerspectiveMatrix(SCREEN_YSIZE * aspect, NativeRenderer.SCREEN_YSIZE_F, 1.0f, 1000.0f);

        orthWidth = SCREEN_XSIZE * 16;

        _projection2D = Matrix.CreateOrthographicOffCenter(4f, (float)(orthWidth + 4), 3844f, 4f, 0.0f, 100f);
#if ENABLE_3D
        _projection3D = Matrix.CreatePerspectiveFieldOfView(1.832596f, viewAspect, 0.1f, 2000f) * Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0.0f, -0.045f, 0.0f);
#endif

        var ratio = Math.Min((double)viewWidth / SCREEN_XSIZE, (double)viewHeight / SCREEN_YSIZE);
        var realWidth = SCREEN_XSIZE * ratio;
        var realHeight = SCREEN_YSIZE * ratio;
        var x = (viewWidth - realWidth) / 2;
        var y = (viewHeight - realHeight) / 2;

        _screenRect = new Rectangle((int)x, (int)y, (int)realWidth, (int)realHeight);
    }

    internal static void SetScreenRenderSize(int width, int lineSize)
    {
        //SCREEN_XSIZE = width;
        //SCREEN_CENTERX = width / 2;

        GFX_LINESIZE = lineSize;
        GFX_LINESIZE_MINUSONE = lineSize - 1;
        GFX_LINESIZE_DOUBLE = 2 * lineSize;

        Scene.SCREEN_SCROLL_LEFT = SCREEN_CENTERX - 8;
        Scene.SCREEN_SCROLL_RIGHT = SCREEN_CENTERX + 8;

        Objects.OBJECT_BORDER_X1 = 128;
        Objects.OBJECT_BORDER_X2 = SCREEN_XSIZE + 128;
    }

    public static void UpdateSurfaces()
    {
        _device.Textures[0] = null;
        SetActivePalette(0, 0, 240);
        UpdateTextureBufferWithTiles();
        UpdateTextureBufferWithSortedSprites();
        _surfaces[0].SetData(textureBuffer);
        for (byte paletteNum = 1; paletteNum < 6; ++paletteNum)
        {
            SetActivePalette(paletteNum, 0, 240);
            UpdateTextureBufferWithTiles();
            UpdateTextureBufferWithSprites();
            _surfaces[paletteNum].SetData(textureBuffer);
        }
        SetActivePalette((byte)0, 0, 240);
    }

    internal static void UpdateActivePalette()
    {
        _device.Textures[0] = null;

        UpdateTextureBufferWithTiles();
        UpdateTextureBufferWithSprites();
        _surfaces[texPaletteNum].SetData(textureBuffer);

        _device.Textures[0] = _surfaces[texPaletteNum];
    }

    public static void Draw()
    {
        _device.SetRenderTarget(_renderTarget);

        _effect.Texture = _surfaces[texPaletteNum];
        _effect.World = Matrix.Identity;
        _effect.View = Matrix.Identity;
        _effect.Projection = _projection2D;
        _effect.LightingEnabled = false;
        _effect.VertexColorEnabled = true;

        _device.RasterizerState = _rasterizerState;

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
        _device.SamplerStates[0] = SamplerState.PointClamp;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();

            for (int i = 0; i < drawListIdx; i++)
            {
                var drawList = drawLists[i];

                if (drawList.vertexCount == 0 || drawList.indexCount == 0)
                    continue;

                if (drawList.blendMode == BlendMode.None)
                    _device.BlendState = BlendState.Opaque;
                else if (drawList.blendMode == BlendMode.Alpha)
                    _device.BlendState = BlendState.NonPremultiplied;
                else if (drawList.blendMode == BlendMode.Additive)
                    _device.BlendState = BlendState.Additive;

                _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexList, drawList.vertexOffset, drawList.vertexCount, indexList, 0,  drawList.indexCount);
            }
        }

#if ENABLE_3D
        }
#endif

        _effect.Texture = null;
        _device.SetRenderTarget(null);
    }

    public static void Present()
    {
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        _spriteBatch.Draw(_renderTarget, _screenRect, Color.White);
        _spriteBatch.End();
    }
}
