﻿using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.Utility;

using static RSDKv4.Drawing;

namespace RSDKv4.Native;

public class NativeRenderer
{
    public static float SCREEN_XSIZE_F = 400;
    public static float SCREEN_CENTERX_F = 400 / 2;

    public static float SCREEN_YSIZE_F = SCREEN_YSIZE;
    public static float SCREEN_CENTERY_F = SCREEN_YSIZE / 2;

    public const int DRAWFACE_LIMIT = 0x1000;
    public const int DRAWVERTEX_LIMIT = DRAWFACE_LIMIT * 4;
    public const int DRAWINDEX_LIMIT = DRAWFACE_LIMIT * 6;
    public const int TEXTURE_LIMIT = 0x80;
    public const int MESH_LIMIT = 0x40;
    public const int RENDERSTATE_LIMIT = 0x100;

    private static Game _game;
    private static GraphicsDevice _device;
    private static BasicEffect _effect;

    private static Effect _retroEffect;

    // general textures
    private static TextureInfo[] textures = new TextureInfo[TEXTURE_LIMIT];
    private static int textureCount = 0;

    private static MeshInfo[] meshes = new MeshInfo[MESH_LIMIT];
    private static int meshCount = 0;

    //private static int renderStateCount = 0;
    //private static RenderState[] renderStateList = new RenderState[RENDERSTATE_LIMIT];
    //private static RenderState currentRenderState;

    //private static RenderVertex[] drawVertexList = new RenderVertex[DRAWVERTEX_LIMIT];
    //private static int vertexCount = 0;
    //private static int indexCount = 0;

    private static float[] retroVertexList = new float[40];
    private static float[] screenBufferVertexList = new float[40];

    private static short[] drawIndexList = new short[DRAWINDEX_LIMIT];

    private static byte vertexR;
    private static byte vertexG;
    private static byte vertexB;

    static NativeRenderer()
    {
        Helpers.Memset(meshes, () => new MeshInfo());
        //Helpers.Memset(renderStateList, () => new RenderState());
    }

    public static void InitRenderDevice(Game game, GraphicsDevice device)
    {
        _game = game;
        _device = device;
        _effect = new BasicEffect(device) { TextureEnabled = true };

        SetScreenDimensions(800, 480);
        SetupDrawIndexList();
    }

    public static void SetScreenDimensions(int width, int height)
    {
        double aspect = (width / (double)height);
        SCREEN_XSIZE_F = (float)(SCREEN_YSIZE * aspect);
        SCREEN_CENTERX_F = (float)(aspect * SCREEN_CENTERY);

        _effect.Projection =
            Matrix.CreateScale((float)(320.0 / (SCREEN_YSIZE * aspect)), 1.0f, 1.0f)
            * CreatePerspectiveMatrix(90.0f, 0.75f, 1.0f, 5000.0f);

        _effect.View = Matrix.Identity;
        _effect.World = Matrix.Identity;
    }

    public static void ResetRenderStates()
    {
        vertexR = 0xFF;
        vertexG = 0xFF;
        vertexB = 0xFF;
    }

    public static void SetRenderBlendMode(byte mode)
    {
        _device.BlendState = mode == 0 ? BlendState.Opaque : BlendState.NonPremultiplied;
    }

    public static void SetRenderVertexColor(byte r, byte g, byte b)
    {
        vertexR = r;
        vertexG = g;
        vertexB = b;
    }

    public static Matrix CreatePerspectiveMatrix(float w, float h, float near, float far)
    {
        var result = new Matrix();
        var val = (float)Math.Tan((float)(0.017453292f * w) * 0.5f);
        result.M11 = (float)(1.0 / val);
        result.M22 = (float)(1.0 / (val * h));
        result.M33 = (far + near) / (far - near);
        result.M34 = 1.0f;
        result.M43 = -((far + far) * near) / (far - near);
        return result;
    }

    public static void SetupDrawIndexList()
    {
        int index = 0;
        for (int i = 0; i < DRAWINDEX_LIMIT; i += 6)
        {
            drawIndexList[i + 2] = (short)(index + 0);
            drawIndexList[i + 1] = (short)(index + 1);
            drawIndexList[i + 0] = (short)(index + 2);
            drawIndexList[i + 5] = (short)(index + 1);
            drawIndexList[i + 4] = (short)(index + 3);
            drawIndexList[i + 3] = (short)(index + 2);
            index += 4;
        }

        int width2 = 0;
        int wBuf = GFX_LINESIZE - 1;
        while (wBuf > 0)
        {
            width2++;
            wBuf >>= 1;
        }
        int height2 = 0;
        int hBuf = SCREEN_YSIZE - 1;
        while (hBuf > 0)
        {
            height2++;
            hBuf >>= 1;
        }
        int texWidth = 1 << width2;
        int texHeight = 1 << height2;

        textures[0] = new TextureInfo();
        textures[0].widthN = 1.0f / texWidth;
        textures[0].heightN = 1.0f / texHeight;

        float w = (SCREEN_XSIZE * textures[0].widthN);
        float w2 = (GFX_LINESIZE * textures[0].widthN);
        float h = (SCREEN_YSIZE * textures[0].heightN);

        retroVertexList[0] = -SCREEN_CENTERX;
        retroVertexList[1] = SCREEN_CENTERY;
        retroVertexList[2] = 160.0f;
        retroVertexList[6] = 0.0f;
        retroVertexList[7] = 0.0f;

        retroVertexList[9] = SCREEN_CENTERX;
        retroVertexList[10] = SCREEN_CENTERY;
        retroVertexList[11] = 160.0f;
        retroVertexList[15] = 1.0f;
        retroVertexList[16] = 0.0f;

        retroVertexList[18] = -SCREEN_CENTERX;
        retroVertexList[19] = -SCREEN_CENTERY;
        retroVertexList[20] = 160.0f;
        retroVertexList[24] = 0.0f;
        retroVertexList[25] = 1.0f;

        retroVertexList[27] = SCREEN_CENTERX;
        retroVertexList[28] = -SCREEN_CENTERY;
        retroVertexList[29] = 160.0f;
        retroVertexList[33] = 1.0f;
        retroVertexList[34] = 1.0f;

        screenBufferVertexList[0] = -1.0f;
        screenBufferVertexList[1] = 1.0f;
        screenBufferVertexList[2] = 1.0f;
        screenBufferVertexList[6] = 0.0f;
        screenBufferVertexList[7] = h;

        screenBufferVertexList[9] = 1.0f;
        screenBufferVertexList[10] = 1.0f;
        screenBufferVertexList[11] = 1.0f;
        screenBufferVertexList[15] = w2;
        screenBufferVertexList[16] = h;

        screenBufferVertexList[18] = -1.0f;
        screenBufferVertexList[19] = -1.0f;
        screenBufferVertexList[20] = 1.0f;
        screenBufferVertexList[24] = 0.0f;
        screenBufferVertexList[25] = 0.0f;

        screenBufferVertexList[27] = 1.0f;
        screenBufferVertexList[28] = -1.0f;
        screenBufferVertexList[29] = 1.0f;
        screenBufferVertexList[33] = w2;
        screenBufferVertexList[34] = 0.0f;

        textures[0].fileName = "RetroBuffer";
        textures[0].width = texWidth;
        textures[0].height = texHeight;
        textures[0].format = TEXFMT.RETROBUFFER;
        textures[0].widthN = 1.0f / texWidth;
        textures[0].heightN = 1.0f / texHeight;
        //textures[0].id = Drawing.CopyRetroBuffer();
    }

    public static void SetRenderMatrix(Matrix? matrix)
    {
#if FNA && DEBUG
        //matrix?.CheckForNaNs();
#endif
        _effect.World = matrix.HasValue ? matrix.Value : Matrix.Identity;
    }

    public static void NewRenderState()
    {
    }

    public static void BeginDraw()
    {
        _effect.TextureEnabled = false;
        _effect.VertexColorEnabled = false;
        _effect.LightingEnabled = false;
        _effect.AmbientLightColor = new Vector3(1, 1, 1);

        _device.SetRenderTarget(null);
        _device.BlendState = BlendState.NonPremultiplied;
        _device.RasterizerState = RasterizerState.CullNone;
        _device.DepthStencilState = DepthStencilState.None;
        _device.Clear(Color.Green);
    }

    public static void EndDraw()
    {

    }

    public static void RenderRect(float x, float y, float z, float w, float h, byte r, byte g, byte b, int alpha)
    {
        if (alpha < 0)
            alpha = 0;
        if (alpha > 255)
            alpha = 255;

        byte a = (byte)alpha;

        _device.DepthStencilState = DepthStencilState.None;
        _effect.TextureEnabled = false;
        _effect.VertexColorEnabled = true;
        _effect.LightingEnabled = false;

        var drawVertexList = new RenderVertex[4];
        drawVertexList[0] = new RenderVertex(new Vector3(x, y, z), Vector3.Zero, Vector2.Zero, new Color(r, g, b, a));
        drawVertexList[1] = new RenderVertex(new Vector3(w + x, y, z), Vector3.Zero, Vector2.Zero, new Color(r, g, b, a));
        drawVertexList[2] = new RenderVertex(new Vector3(x, y - h, z), Vector3.Zero, Vector2.Zero, new Color(r, g, b, a));
        drawVertexList[3] = new RenderVertex(new Vector3(w + x, y - h, z), Vector3.Zero, Vector2.Zero, new Color(r, g, b, a));

        DrawVertices(drawVertexList, 4, drawIndexList, 2);
    }

    public static void RenderImage(float x, float y, float z, float scaleX, float scaleY, float pivotX, float pivotY, float sprW, float sprH, float sprX, float sprY,
                 int alpha, int texture)
    {
        if (alpha < 0)
            alpha = 0;
        if (alpha > 255)
            alpha = 255;

        byte a = (byte)alpha;

        _device.DepthStencilState = DepthStencilState.None;

        _effect.TextureEnabled = true;
        _effect.Texture = textures[texture].id;
        _effect.VertexColorEnabled = true;
        _effect.LightingEnabled = false;

        var drawVertexList = new RenderVertex[4];
        drawVertexList[0] = new RenderVertex(
            new Vector3(x - (pivotX * scaleX), (pivotY * scaleY) + y, z),
            Vector3.Zero,
            new Vector2(sprX * textures[texture].widthN, sprY * textures[texture].heightN),
            new Color(vertexR, vertexG, vertexB, a)
            );
        drawVertexList[1] = new RenderVertex(
            new Vector3(((sprW - pivotX) * scaleX) + x, (pivotY * scaleY) + y, z),
            Vector3.Zero,
            new Vector2((sprX + sprW) * textures[texture].widthN, sprY * textures[texture].heightN),
            new Color(vertexR, vertexG, vertexB, a)
            );
        drawVertexList[2] = new RenderVertex(
            new Vector3(x - (pivotX * scaleX), y - ((sprH - pivotY) * scaleY), z),
            Vector3.Zero,
            new Vector2(sprX * textures[texture].widthN, (sprY + sprH) * textures[texture].heightN),
            new Color(vertexR, vertexG, vertexB, a)
            );
        drawVertexList[3] = new RenderVertex(
            new Vector3(((sprW - pivotX) * scaleX) + x, y - ((sprH - pivotY) * scaleY), z),
            Vector3.Zero,
            new Vector2((sprX + sprW) * textures[texture].widthN, (sprY + sprH) * textures[texture].heightN),
            new Color(vertexR, vertexG, vertexB, a)
            );

        DrawVertices(drawVertexList, 4, drawIndexList, 2);
    }

    public static void RenderText(ushort[] text, int fontID, float x, float y, int z, float scale, int alpha)
    {
        BitmapFont font = Font.fontList[fontID];
        float posX = x;
        float posY = (font.baseline * scale) + y;

        if (alpha < 0)
            alpha = 0;
        if (alpha > 255)
            alpha = 255;

        byte a = (byte)alpha;

        _device.DepthStencilState = DepthStencilState.None;
        _effect.TextureEnabled = true;
        _effect.VertexColorEnabled = true;
        _effect.LightingEnabled = false;

        var drawVertexList = new RenderVertex[(text.Length + 1) * 4];
        var drawListIdx = 0;
        var primitiveCount = 0;
        Texture2D oldTexture = null;

        for (int i = 0; i < text.Length; i++)
        {
            var character = text[i];
            BitmapFontCharacter fontChar = font.characters[character];
            TextureInfo texture = textures[fontChar.textureID];

            if (oldTexture != null && oldTexture != texture.id)
            {
                DrawVertices(drawVertexList, drawListIdx, drawIndexList, primitiveCount);
                drawListIdx = 0;
                primitiveCount = 0;
            }

            _effect.Texture = texture.id;

            if (character == 1)
            {
                posX = x;
                posY -= (font.lineHeight * scale);
            }
            else
            {
                var vertex1 = new RenderVertex();
                vertex1.position.X = posX + (fontChar.xOffset * scale);
                vertex1.position.Y = posY - (fontChar.yOffset * scale);
                vertex1.position.Z = z;
                vertex1.texCoord.X = fontChar.x * texture.widthN;
                vertex1.texCoord.Y = fontChar.y * texture.heightN;
                vertex1.color.R = vertexR;
                vertex1.color.G = vertexG;
                vertex1.color.B = vertexB;
                vertex1.color.A = (byte)a;

                var vertex2 = new RenderVertex();
                vertex2.position.X = posX + ((fontChar.width + fontChar.xOffset) * scale);
                vertex2.position.Y = vertex1.position.Y;
                vertex2.position.Z = z;
                vertex2.texCoord.X = (fontChar.x + fontChar.width) * texture.widthN;
                vertex2.texCoord.Y = vertex1.texCoord.Y;
                vertex2.color.R = vertexR;
                vertex2.color.G = vertexG;
                vertex2.color.B = vertexB;
                vertex2.color.A = (byte)a;

                var vertex3 = new RenderVertex();
                vertex3.position.X = vertex1.position.X;
                vertex3.position.Y = posY - ((fontChar.height + fontChar.yOffset) * scale);
                vertex3.position.Z = z;
                vertex3.texCoord.X = vertex1.texCoord.X;
                vertex3.texCoord.Y = (fontChar.y + fontChar.height) * texture.heightN;
                vertex3.color.R = vertexR;
                vertex3.color.G = vertexG;
                vertex3.color.B = vertexB;
                vertex3.color.A = (byte)a;

                var vertex4 = new RenderVertex();
                vertex4.position.X = vertex2.position.X;
                vertex4.position.Y = vertex3.position.Y;
                vertex4.position.Z = z;
                vertex4.texCoord.X = vertex2.texCoord.X;
                vertex4.texCoord.Y = vertex3.texCoord.Y;
                vertex4.color.R = vertexR;
                vertex4.color.G = vertexG;
                vertex4.color.B = vertexB;
                vertex4.color.A = (byte)a;

                drawVertexList[drawListIdx + 0] = vertex1;
                drawVertexList[drawListIdx + 1] = vertex2;
                drawVertexList[drawListIdx + 2] = vertex3;
                drawVertexList[drawListIdx + 3] = vertex4;

                drawListIdx += 4;
                primitiveCount += 2;
            }
            //}
            posX += (fontChar.xAdvance * scale);
        }

        DrawVertices(drawVertexList, drawListIdx, drawIndexList, primitiveCount);
    }

    public static void RenderMesh(MeshInfo mesh, byte type, bool depthTest)
    {
        if (mesh == null)
            return;

        switch (type)
        {
            case MESH.COLOURS:
                _effect.VertexColorEnabled = true;
                _effect.LightingEnabled = false;
                break;
            case MESH.NORMALS:
                _effect.VertexColorEnabled = false;
                _effect.LightingEnabled = false;
                break;
            case MESH.COLOURS_NORMALS:
                _effect.VertexColorEnabled = true;
                _effect.LightingEnabled = false;
                break;
        }

        _device.DepthStencilState = depthTest ? DepthStencilState.Default : DepthStencilState.None;

        if (mesh.textureId >= TEXTURE_LIMIT)
        {
            _effect.TextureEnabled = false;
        }
        else
        {
            _effect.TextureEnabled = true;
            _effect.Texture = textures[mesh.textureId].id;
        }

        DrawVertices(mesh.vertices, mesh.vertices.Length, mesh.indices, mesh.indices.Length / 3);
    }

    public static void RenderRetroBuffer(int alpha, float z)
    {
        if (alpha < 0)
            alpha = 0;
        if (alpha > 255)
            alpha = 255;

        byte a = (byte)alpha;

        _device.DepthStencilState = DepthStencilState.None;
        _device.SamplerStates[0] = SamplerState.LinearClamp;

        _effect.TextureEnabled = true;
        _effect.Texture = Drawing.CopyRetroBuffer();
        _effect.VertexColorEnabled = false;
        _effect.LightingEnabled = false;
        _device.BlendState = BlendState.Opaque;

        RenderVertex vertex1 = new RenderVertex();
        vertex1.position.X = retroVertexList[0];
        vertex1.position.Y = retroVertexList[1];
        vertex1.position.Z = z;
        vertex1.texCoord.X = retroVertexList[6];
        vertex1.texCoord.Y = retroVertexList[7];
        vertex1.color.R = vertexR;
        vertex1.color.G = vertexG;
        vertex1.color.B = vertexB;
        vertex1.color.A = (byte)a;

        RenderVertex vertex2 = new RenderVertex();
        vertex2.position.X = retroVertexList[9];
        vertex2.position.Y = retroVertexList[10];
        vertex2.position.Z = z;
        vertex2.texCoord.X = retroVertexList[15];
        vertex2.texCoord.Y = retroVertexList[16];
        vertex1.color.R = vertexR;
        vertex1.color.G = vertexG;
        vertex1.color.B = vertexB;
        vertex1.color.A = (byte)a;

        RenderVertex vertex3 = new RenderVertex();
        vertex3.position.X = retroVertexList[18];
        vertex3.position.Y = retroVertexList[19];
        vertex3.position.Z = z;
        vertex3.texCoord.X = retroVertexList[24];
        vertex3.texCoord.Y = retroVertexList[25];
        vertex1.color.R = vertexR;
        vertex1.color.G = vertexG;
        vertex1.color.B = vertexB;
        vertex1.color.A = (byte)a;

        RenderVertex vertex4 = new RenderVertex();
        vertex4.position.X = retroVertexList[27];
        vertex4.position.Y = retroVertexList[28];
        vertex4.position.Z = z;
        vertex4.texCoord.X = retroVertexList[33];
        vertex4.texCoord.Y = retroVertexList[34];
        vertex1.color.R = vertexR;
        vertex1.color.G = vertexG;
        vertex1.color.B = vertexB;
        vertex1.color.A = (byte)a;

        var drawVertexList = new RenderVertex[4];
        drawVertexList[0] = vertex1;
        drawVertexList[1] = vertex2;
        drawVertexList[2] = vertex3;
        drawVertexList[3] = vertex4;

        DrawVertices(drawVertexList, 4, drawIndexList, 2);
    }

    // PRIMITIVES NOT INDICES YOU TWIT
    private static void DrawVertices(RenderVertex[] drawVertexList, int vertexCount, short[] drawIndexList, int primitiveCount)
    {
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, drawVertexList, 0, vertexCount, drawIndexList, 0, primitiveCount);
        }
    }

    public static int LoadTexture(string filePath, int format)
    {
        int texID = 0;
        for (int i = 0; i < TEXTURE_LIMIT; ++i)
        {
            if (textures[texID] == null)
                break;
            if (textures[texID].fileName == filePath)
                return texID;
            texID++;
        }

        if (texID == TEXTURE_LIMIT)
            return 0;

        if (!FileIO.LoadFile(filePath, out var info))
            return 0;

        using var stream = FileIO.CreateFileStream();
        var texture = textures[texID] = new TextureInfo();
        texture.id = Texture2D.FromStream(_device, stream);
        texture.width = texture.id.Width;
        texture.height = texture.id.Height;
        texture.fileName = filePath;

        float normalize = 0;
        if (filePath.Contains("@2"))
            normalize = 2.0f;
        else if (filePath.Contains("@1"))
            normalize = 0.5f;
        else
            normalize = 1.0f;

        texture.widthN = normalize / texture.width;
        texture.heightN = normalize / texture.height;

        return texID;
    }

    public static MeshInfo LoadMesh(string filePath, int textureId)
    {
        int meshID = 0;
        for (int i = 0; i < MESH_LIMIT; i++)
        {
            if (meshes[meshID].fileName == filePath && meshes[meshID].textureId == textureId)
                return meshes[meshID];

            if (string.IsNullOrEmpty(meshes[meshID].fileName))
                break;

            meshID++;
        }
        if (FileIO.LoadFile(filePath, out var info))
        {
            byte[] buffer = new byte[4];
            FileIO.ReadFile(buffer, 0, 4);
            if (buffer[0] == 'R' && buffer[1] == '3' && buffer[2] == 'D' && buffer[3] == '\0')
            {
                MeshInfo mesh = meshes[meshID];
                mesh.fileName = filePath;
                mesh.textureId = (byte)textureId;

                //FileIO.ReadFile(buffer, 0, sizeof(ushort));
                var vertexCount = FileIO.ReadUInt16();
                mesh.vertices = new RenderVertex[vertexCount];

                for (int v = 0; v < mesh.vertices.Length; ++v)
                {
                    //float buf = 0;
                    //FileRead(&buf, sizeof(float));
                    mesh.vertices[v].texCoord.X = FileIO.ReadFloat();
                    mesh.vertices[v].texCoord.Y = FileIO.ReadFloat();
                    mesh.vertices[v].color = new Color(255, 255, 255, 255);
                }

                var indexCount = FileIO.ReadUInt16();
                mesh.indices = new short[3 * indexCount];

                int id = 0;
                for (int i = 0; i < indexCount; ++i)
                {
                    mesh.indices[id + 2] = FileIO.ReadInt16();
                    mesh.indices[id + 1] = FileIO.ReadInt16();
                    mesh.indices[id + 0] = FileIO.ReadInt16();

                    id += 3;
                }

                //FileRead(buffer, sizeof(ushort));
                var frameCount = FileIO.ReadUInt16();
                if (frameCount <= 1)
                {
                    for (int v = 0; v < vertexCount; ++v)
                    {
                        mesh.vertices[v].position.X = FileIO.ReadFloat();
                        mesh.vertices[v].position.Y = FileIO.ReadFloat();
                        mesh.vertices[v].position.Z = FileIO.ReadFloat();
                        mesh.vertices[v].normal.X = FileIO.ReadFloat();
                        mesh.vertices[v].normal.Y = FileIO.ReadFloat();
                        mesh.vertices[v].normal.Z = FileIO.ReadFloat();
                    }
                }
                else
                {
                    mesh.frames = new MeshVertex[frameCount * vertexCount];
                    for (int f = 0; f < frameCount; ++f)
                    {
                        int frameOff = (f * vertexCount);
                        for (int v = 0; v < vertexCount; ++v)
                        {
                            mesh.frames[frameOff + v].position.X = FileIO.ReadFloat();
                            mesh.frames[frameOff + v].position.Y = FileIO.ReadFloat();
                            mesh.frames[frameOff + v].position.Z = FileIO.ReadFloat();
                            mesh.frames[frameOff + v].normal.X = FileIO.ReadFloat();
                            mesh.frames[frameOff + v].normal.Y = FileIO.ReadFloat();
                            mesh.frames[frameOff + v].normal.Z = FileIO.ReadFloat();
                        }
                    }
                }

                FileIO.CloseFile();

                return mesh;
            }
            else
            {
                FileIO.CloseFile();
            }
        }
        return null;
    }

    public static void SetMeshAnimation(MeshInfo mesh, MeshAnimator animator, ushort frameID, ushort frameCount, float speed)
    {
        animator.frameCount = frameCount;
        if (frameCount >= mesh.frames.Length)
            animator.frameCount = (ushort)(mesh.frames.Length - 1);
        if (frameID < mesh.frames.Length)
        {
            animator.loopIndex = frameID;
            animator.frameId = frameID;
        }
        else
        {
            animator.loopIndex = 0;
            animator.frameId = 0;
        }
        animator.animationSpeed = speed;
    }

    public static void SetMeshVertexColors(MeshInfo mesh, byte r, byte g, byte b, byte a)
    {
        for (int v = 0; v < mesh.vertices.Length; ++v)
        {
            mesh.vertices[v].color = new Color(r, g, b, a);
        }
    }

    public static void AnimateMesh(MeshInfo mesh, MeshAnimator animator)
    {
        if (mesh.frames.Length > 1)
        {
            if (!animator.animationFinished)
            {
                animator.animationTimer += animator.animationSpeed;

                while (animator.animationTimer > 1.0f)
                { 
                    // new frame (forwards)
                    animator.animationTimer -= 1.0f;
                    animator.frameId++;

                    if (animator.loopAnimation)
                    {
                        if (animator.frameId >= animator.frameCount)
                            animator.frameId = animator.loopIndex;
                    }
                    else if (animator.frameId >= animator.frameCount)
                    {
                        animator.frameId = animator.frameCount;
                        animator.animationFinished = true;
                        animator.animationTimer = 0.0f;
                    }
                }
                while (animator.animationTimer < 0.0f)
                { 
                    // new frame (backwards)
                    animator.animationTimer += 1.0f;
                    animator.frameId--;

                    if (animator.frameId < animator.loopIndex || animator.frameId >= animator.frameCount)
                    {
                        if (animator.loopAnimation)
                        {
                            animator.frameId = animator.frameCount;
                        }
                        else
                        {
                            animator.frameId = animator.loopIndex;
                            animator.animationTimer = 0.0f;
                            animator.animationFinished = true;
                        }
                    }
                }

                ushort frameID = animator.frameId;
                ushort nextFrame = (ushort)(animator.frameId + 1);
                if (nextFrame >= animator.frameCount && animator.animationSpeed >= 0)
                    nextFrame = animator.loopIndex;
                if (frameID >= animator.frameCount && animator.animationSpeed < 0)
                    frameID = animator.loopIndex;

                float interp2 = animator.animationTimer;
                float interp = 1.0f - animator.animationTimer;

                var vertOffset = frameID * mesh.vertices.Length;
                var nextVertOffset = nextFrame * mesh.vertices.Length;
                for (int v = 0; v < mesh.vertices.Length; ++v)
                {
                    var vert = mesh.frames[vertOffset];
                    var nextVert = mesh.frames[nextVertOffset];

                    mesh.vertices[v].position = (vert.position * interp) + (nextVert.position * interp2);
                    mesh.vertices[v].normal = (vert.normal * interp) + (nextVert.normal * interp2);

                    vertOffset++;
                    nextVertOffset++;
                }
            }
            else if (animator.loopAnimation)
                animator.animationFinished = false;
        }
    }
}
