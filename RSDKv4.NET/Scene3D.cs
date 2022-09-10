﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

using static RSDKv4.Drawing;

namespace RSDKv4;

public struct Vertex2D
{
    public int x;
    public int y;
    public int u;
    public int v;
}

public struct Vertex3D
{
    public int x;
    public int y;
    public int z;
    public int u;
    public int v;
}

public struct Face3D
{
    public int a;
    public int b;
    public int c;
    public int d;
    public int color;
    public byte flag;
}
public struct SortList
{
    public int z;
    public int index;
}

public struct SortListComparer : IComparer<SortList>
{
    public int Compare(SortList x, SortList y)
    {
        return y.z - x.z;
    }
}

public class Quad2D
{
    public Vertex2D[] vertex = new Vertex2D[4];
}
public static class FACE_FLAG
{
    public const int TEXTURED_3D = 0,
    TEXTURED_2D = 1,
    COLOURED_3D = 2,
    COLOURED_2D = 3,
    FADED = 4,
    TEXTURED_C = 5,
    TEXTURED_C_BLEND = 6,
    THREEDSPRITE = 7;
};
public class Scene3D
{
    public static Vertex3D[] vertexBuffer = new Vertex3D[4096];
    public static Vertex3D[] vertexBufferT = new Vertex3D[4096];
    public static Face3D[] faceBuffer = new Face3D[1024];
    public static SortList[] drawList = new SortList[1024];
    public static int vertexCount = 0;
    public static int faceCount = 0;
    public static int projectionX = 136;
    public static int projectionY = 160;
    public static int fogColor = 0;
    public static int fogStrength = 0;
    public static int[] matWorld = new int[16];
    public static int[] matView = new int[16];
    public static int[] matFinal = new int[16];
    public static int[] matTemp = new int[16];

    public static void SetIdentityMatrix(ref int[] m)
    {
        m[0] = 256;
        m[1] = 0;
        m[2] = 0;
        m[3] = 0;
        m[4] = 0;
        m[5] = 256;
        m[6] = 0;
        m[7] = 0;
        m[8] = 0;
        m[9] = 0;
        m[10] = 256;
        m[11] = 0;
        m[12] = 0;
        m[13] = 0;
        m[14] = 0;
        m[15] = 256;
    }


    public static void MatrixMultiply(ref int[] a, ref int[] b)
    {
        int[] numArray = new int[16];
        for (uint index = 0; index < 16U; ++index)
        {
            uint num1 = index & 3U;
            uint num2 = index & 12U;
            numArray[index] = (b[num1] * a[num2] >> 8) + (b[(num1 + 4U)] * a[(num2 + 1U)] >> 8) + (b[(num1 + 8U)] * a[(num2 + 2U)] >> 8) + (b[(num1 + 12U)] * a[(num2 + 3U)] >> 8);
        }
        for (uint index = 0; index < 16U; ++index)
            a[index] = numArray[index];
    }

    public static void MatrixTranslateXYZ(ref int[] m, int xPos, int yPos, int zPos)
    {
        m[0] = 256;
        m[1] = 0;
        m[2] = 0;
        m[3] = 0;
        m[4] = 0;
        m[5] = 256;
        m[6] = 0;
        m[7] = 0;
        m[8] = 0;
        m[9] = 0;
        m[10] = 256;
        m[11] = 0;
        m[12] = xPos;
        m[13] = yPos;
        m[14] = zPos;
        m[15] = 256;
    }

    public static void MatrixScaleXYZ(ref int[] m, int xScale, int yScale, int zScale)
    {
        m[0] = xScale;
        m[1] = 0;
        m[2] = 0;
        m[3] = 0;
        m[4] = 0;
        m[5] = yScale;
        m[6] = 0;
        m[7] = 0;
        m[8] = 0;
        m[9] = 0;
        m[10] = zScale;
        m[11] = 0;
        m[12] = 0;
        m[13] = 0;
        m[14] = 0;
        m[15] = 256;
    }

    public static void MatrixRotateX(ref int[] m, int angle)
    {
        if (angle < 0)
            angle = 512 - angle;
        angle &= 511;
        int num1 = FastMath.Sin512(angle) >> 1;
        int num2 = FastMath.Cos512(angle) >> 1;
        m[0] = 256;
        m[1] = 0;
        m[2] = 0;
        m[3] = 0;
        m[4] = 0;
        m[5] = num2;
        m[6] = num1;
        m[7] = 0;
        m[8] = 0;
        m[9] = -num1;
        m[10] = num2;
        m[11] = 0;
        m[12] = 0;
        m[13] = 0;
        m[14] = 0;
        m[15] = 256;
    }

    public static void MatrixRotateY(ref int[] m, int angle)
    {
        if (angle < 0)
            angle = 512 - angle;
        angle &= 511;
        int num1 = FastMath.Sin512(angle) >> 1;
        int num2 = FastMath.Cos512(angle) >> 1;
        m[0] = num2;
        m[1] = 0;
        m[2] = num1;
        m[3] = 0;
        m[4] = 0;
        m[5] = 256;
        m[6] = 0;
        m[7] = 0;
        m[8] = -num1;
        m[9] = 0;
        m[10] = num2;
        m[11] = 0;
        m[12] = 0;
        m[13] = 0;
        m[14] = 0;
        m[15] = 256;
    }

    public static void MatrixRotateZ(ref int[] m, int angle)
    {
        if (angle < 0)
            angle = 512 - angle;
        angle &= 511;
        int num1 = FastMath.Sin512(angle) >> 1;
        int num2 = FastMath.Cos512(angle) >> 1;
        m[0] = num2;
        m[1] = 0;
        m[2] = num1;
        m[3] = 0;
        m[4] = 0;
        m[5] = 256;
        m[6] = 0;
        m[7] = 0;
        m[8] = -num1;
        m[9] = 0;
        m[10] = num2;
        m[11] = 0;
        m[12] = 0;
        m[13] = 0;
        m[14] = 0;
        m[15] = 256;
    }

    public static void MatrixRotateXYZ(ref int[] m, int angleX, int angleY, int angleZ)
    {
        if (angleX < 0)
            angleX = 512 - angleX;
        angleX &= 511;
        if (angleY < 0)
            angleY = 512 - angleY;
        angleY &= 511;
        if (angleZ < 0)
            angleZ = 512 - angleZ;
        angleZ &= 511;
        int num1 = FastMath.Sin512(angleX) >> 1;
        int num2 = FastMath.Cos512(angleX) >> 1;
        int num3 = FastMath.Sin512(angleY) >> 1;
        int num4 = FastMath.Cos512(angleY) >> 1;
        int num5 = FastMath.Sin512(angleZ) >> 1;
        int num6 = FastMath.Cos512(angleZ) >> 1;
        m[0] = (num4 * num6 >> 8) + ((num1 * num3 >> 8) * num5 >> 8);
        m[1] = (num4 * num5 >> 8) - ((num1 * num3 >> 8) * num6 >> 8);
        m[2] = num2 * num3 >> 8;
        m[3] = 0;
        m[4] = -num2 * num5 >> 8;
        m[5] = num2 * num6 >> 8;
        m[6] = num1;
        m[7] = 0;
        m[8] = ((num1 * num4 >> 8) * num5 >> 8) - (num3 * num6 >> 8);
        m[9] = (-num3 * num5 >> 8) - ((num1 * num4 >> 8) * num6 >> 8);
        m[10] = num2 * num4 >> 8;
        m[11] = 0;
        m[12] = 0;
        m[13] = 0;
        m[14] = 0;
        m[15] = 256;
    }

    public static void MatrixInverse(ref int[] matrix)
    {
        var m = new Matrix(matrix[0] / 256.0f, matrix[1] / 256.0f, matrix[2] / 256.0f, matrix[3] / 256.0f,
                           matrix[4] / 256.0f, matrix[5] / 256.0f, matrix[6] / 256.0f, matrix[7] / 256.0f,
                           matrix[8] / 256.0f, matrix[9] / 256.0f, matrix[10] / 256.0f, matrix[11] / 256.0f,
                           matrix[12] / 256.0f, matrix[13] / 256.0f, matrix[14] / 256.0f, matrix[15] / 256.0f);

        var inv = Matrix.Invert(m);

        matrix[0] = (int)(inv.M11 * 256);
        matrix[1] = (int)(inv.M12 * 256);
        matrix[2] = (int)(inv.M13 * 256);
        matrix[3] = (int)(inv.M14 * 256);
        matrix[4] = (int)(inv.M21 * 256);
        matrix[5] = (int)(inv.M22 * 256);
        matrix[6] = (int)(inv.M23 * 256);
        matrix[7] = (int)(inv.M24 * 256);
        matrix[8] = (int)(inv.M31 * 256);
        matrix[9] = (int)(inv.M32 * 256);
        matrix[10] = (int)(inv.M33 * 256);
        matrix[11] = (int)(inv.M34 * 256);
        matrix[12] = (int)(inv.M41 * 256);
        matrix[13] = (int)(inv.M42 * 256);
        matrix[14] = (int)(inv.M43 * 256);
        matrix[15] = (int)(inv.M44 * 256);
    }

    public static void TransformVertexBuffer()
    {
        int index1 = 0;
        int index2 = 0;
        for (int index3 = 0; index3 < 16; ++index3)
            matFinal[index3] = matWorld[index3];
        MatrixMultiply(ref matFinal, ref matView);
        for (int index3 = 0; index3 < vertexCount; ++index3)
        {
            Vertex3D vertex3D = vertexBuffer[index1];
            vertexBufferT[index2] = vertex3D;
            vertexBufferT[index2].x = (matFinal[0] * vertex3D.x >> 8) + (matFinal[4] * vertex3D.y >> 8) + (matFinal[8] * vertex3D.z >> 8) + matFinal[12];
            vertexBufferT[index2].y = (matFinal[1] * vertex3D.x >> 8) + (matFinal[5] * vertex3D.y >> 8) + (matFinal[9] * vertex3D.z >> 8) + matFinal[13];
            vertexBufferT[index2].z = (matFinal[2] * vertex3D.x >> 8) + (matFinal[6] * vertex3D.y >> 8) + (matFinal[10] * vertex3D.z >> 8) + matFinal[14];
            if (vertexBufferT[index2].z < 1 && vertexBufferT[index2].z > 0)
                vertexBufferT[index2].z = 1;
            ++index1;
            ++index2;
        }
    }

    public static void TransformVertices(ref int[] m, int vStart, int vEnd)
    {
        int num = 0;
        Vertex3D vertex3D1 = new Vertex3D();
        ++vEnd;
        for (int index = vStart; index < vEnd - 1; ++index)
        {
            Vertex3D vertex3D2 = vertexBuffer[index];
            vertex3D1.x = (m[0] * vertex3D2.x >> 8) + (m[4] * vertex3D2.y >> 8) + (m[8] * vertex3D2.z >> 8) + m[12];
            vertex3D1.y = (m[1] * vertex3D2.x >> 8) + (m[5] * vertex3D2.y >> 8) + (m[9] * vertex3D2.z >> 8) + m[13];
            vertex3D1.z = (m[2] * vertex3D2.x >> 8) + (m[6] * vertex3D2.y >> 8) + (m[10] * vertex3D2.z >> 8) + m[14];
            vertexBuffer[index].x = vertex3D1.x;
            vertexBuffer[index].y = vertex3D1.y;
            vertexBuffer[index].z = vertex3D1.z;
            ++num;
        }
    }

    public static void Sort3DDrawList()
    {
        for (int index = 0; index < faceCount; ++index)
        {
            drawList[index].z = vertexBufferT[faceBuffer[index].a].z + vertexBufferT[faceBuffer[index].b].z + vertexBufferT[faceBuffer[index].c].z + vertexBufferT[faceBuffer[index].d].z >> 2;
            drawList[index].index = index;
        }

        //for (int index1 = 0; index1 < faceCount; ++index1)
        //{
        //    for (int index2 = faceCount - 1; index2 > index1; --index2)
        //    {
        //        if (drawList[index2].z > drawList[index2 - 1].z)
        //        {
        //            int index3 = drawList[index2].index;
        //            int z = drawList[index2].z;
        //            drawList[index2].index = drawList[index2 - 1].index;
        //            drawList[index2].z = drawList[index2 - 1].z;
        //            drawList[index2 - 1].index = index3;
        //            drawList[index2 - 1].z = z;
        //        }
        //    }
        //}

        Array.Sort(drawList, 0, faceCount, new SortListComparer());
    }

    public static void Draw3DScene(int surfaceNum)
    {
        Quad2D face = new Quad2D();
        for (int index = 0; index < faceCount; ++index)
        {
            var face3D = faceBuffer[drawList[index].index];
            var a = vertexBufferT[face3D.a];
            var b = vertexBufferT[face3D.b];
            var c = vertexBufferT[face3D.c];
            var d = vertexBufferT[face3D.d];

            switch (face3D.flag)
            {
                case 0:
                    if (a.z > 256 && b.z > 256 && (c.z > 256 && d.z > 256))
                    {
                        face.vertex[0].x = SCREEN_CENTERX + a.x * projectionX / a.z;
                        face.vertex[0].y = SCREEN_CENTERY - a.y * projectionY / a.z;
                        face.vertex[1].x = SCREEN_CENTERX + b.x * projectionX / b.z;
                        face.vertex[1].y = SCREEN_CENTERY - b.y * projectionY / b.z;
                        face.vertex[2].x = SCREEN_CENTERX + c.x * projectionX / c.z;
                        face.vertex[2].y = SCREEN_CENTERY - c.y * projectionY / c.z;
                        face.vertex[3].x = SCREEN_CENTERX + d.x * projectionX / d.z;
                        face.vertex[3].y = SCREEN_CENTERY - d.y * projectionY / d.z;
                        face.vertex[0].u = a.u;
                        face.vertex[0].v = a.v;
                        face.vertex[1].u = b.u;
                        face.vertex[1].v = b.v;
                        face.vertex[2].u = c.u;
                        face.vertex[2].v = c.v;
                        face.vertex[3].u = d.u;
                        face.vertex[3].v = d.v;
                        DrawTexturedQuad(face, surfaceNum);
                        break;
                    }
                    break;
                case 1:
                    face.vertex[0].x = a.x;
                    face.vertex[0].y = a.y;
                    face.vertex[1].x = b.x;
                    face.vertex[1].y = b.y;
                    face.vertex[2].x = c.x;
                    face.vertex[2].y = c.y;
                    face.vertex[3].x = d.x;
                    face.vertex[3].y = d.y;
                    face.vertex[0].u = a.u;
                    face.vertex[0].v = a.v;
                    face.vertex[1].u = b.u;
                    face.vertex[1].v = b.v;
                    face.vertex[2].u = c.u;
                    face.vertex[2].v = c.v;
                    face.vertex[3].u = d.u;
                    face.vertex[3].v = d.v;
                    DrawTexturedQuad(face, surfaceNum);
                    break;
                case 2:
                    if (a.z > 0 && b.z > 0 && c.z > 0 && d.z > 0)
                    {
                        face.vertex[0].x = SCREEN_CENTERX + projectionX * a.x / a.z;
                        face.vertex[0].y = SCREEN_CENTERY - projectionY * a.y / a.z;
                        face.vertex[1].x = SCREEN_CENTERX + projectionX * b.x / b.z;
                        face.vertex[1].y = SCREEN_CENTERY - projectionY * b.y / b.z;
                        face.vertex[2].x = SCREEN_CENTERX + projectionX * c.x / c.z;
                        face.vertex[2].y = SCREEN_CENTERY - projectionY * c.y / c.z;
                        face.vertex[3].x = SCREEN_CENTERX + projectionX * d.x / d.z;
                        face.vertex[3].y = SCREEN_CENTERY - projectionY * d.y / d.z;
                        DrawQuad(face, face3D.color);
                        break;
                    }
                    break;
                case 3:
                    face.vertex[0].x = a.x;
                    face.vertex[0].y = a.y;
                    face.vertex[1].x = b.x;
                    face.vertex[1].y = b.y;
                    face.vertex[2].x = c.x;
                    face.vertex[2].y = c.y;
                    face.vertex[3].x = d.x;
                    face.vertex[3].y = d.y;
                    DrawQuad(face, face3D.color);
                    break;
                case FACE_FLAG.FADED:
                    if (a.z > 0 && b.z > 0 && c.z > 0 && d.z > 0)
                    {
                        face.vertex[0].x = SCREEN_CENTERX + projectionX * a.x / a.z;
                        face.vertex[0].y = SCREEN_CENTERY - projectionY * a.y / a.z;
                        face.vertex[1].x = SCREEN_CENTERX + projectionX * b.x / b.z;
                        face.vertex[1].y = SCREEN_CENTERY - projectionY * b.y / b.z;
                        face.vertex[2].x = SCREEN_CENTERX + projectionX * c.x / c.z;
                        face.vertex[2].y = SCREEN_CENTERY - projectionY * c.y / c.z;
                        face.vertex[3].x = SCREEN_CENTERX + projectionX * d.x / d.z;
                        face.vertex[3].y = SCREEN_CENTERY - projectionY * d.y / d.z;

                        int fogStr = 0;
                        if ((drawList[index].z - 0x8000) >> 8 >= 0)
                            fogStr = (drawList[index].z - 0x8000) >> 8;
                        if (fogStr > fogStrength)
                            fogStr = fogStrength;

                        Drawing.DrawFadedQuad(face,  (uint)face3D.color, (uint)fogColor, 0xFF - fogStr);
                    }
                    break;
                case FACE_FLAG.TEXTURED_C:
                    if (a.z > 0)
                    {
                        // [face.a].uv == sprite center
                        // [face.b].uv == ???
                        // [face.c].uv == sprite extend (how far to each edge X & Y)
                        // [face.d].uv == unused

                        face.vertex[0].x = SCREEN_CENTERX + projectionX * (a.x - b.u) / a.z;
                        face.vertex[0].y = SCREEN_CENTERY - projectionY * (a.y + b.v) / a.z;
                        face.vertex[1].x = SCREEN_CENTERX + projectionX * (a.x + b.u) / a.z;
                        face.vertex[1].y = SCREEN_CENTERY - projectionY * (a.y + b.v) / a.z;
                        face.vertex[2].x = SCREEN_CENTERX + projectionX * (a.x - b.u) / a.z;
                        face.vertex[2].y = SCREEN_CENTERY - projectionY * (a.y - b.v) / a.z;
                        face.vertex[3].x = SCREEN_CENTERX + projectionX * (a.x + b.u) / a.z;
                        face.vertex[3].y = SCREEN_CENTERY - projectionY * (a.y - b.v) / a.z;

                        face.vertex[0].u = a.u - c.u;
                        face.vertex[0].v = a.v - c.v;
                        face.vertex[1].u = a.u + c.u;
                        face.vertex[1].v = a.v - c.v;
                        face.vertex[2].u = a.u - c.u;
                        face.vertex[2].v = a.v + c.v;
                        face.vertex[3].u = a.u + c.u;
                        face.vertex[3].v = a.v + c.v;

                        Drawing.DrawTexturedQuad(face, face3D.color);
                    }
                    break;
                case FACE_FLAG.TEXTURED_C_BLEND:
                    if (a.z > 0)
                    {
                        // See above, its the same just blended

                        face.vertex[0].x = SCREEN_CENTERX + projectionX * (a.x - b.u) / a.z;
                        face.vertex[0].y = SCREEN_CENTERY - projectionY * (a.y + b.v) / a.z;
                        face.vertex[1].x = SCREEN_CENTERX + projectionX * (a.x + b.u) / a.z;
                        face.vertex[1].y = SCREEN_CENTERY - projectionY * (a.y + b.v) / a.z;
                        face.vertex[2].x = SCREEN_CENTERX + projectionX * (a.x - b.u) / a.z;
                        face.vertex[2].y = SCREEN_CENTERY - projectionY * (a.y - b.v) / a.z;
                        face.vertex[3].x = SCREEN_CENTERX + projectionX * (a.x + b.u) / a.z;
                        face.vertex[3].y = SCREEN_CENTERY - projectionY * (a.y - b.v) / a.z;

                        face.vertex[0].u = a.u - c.u;
                        face.vertex[0].v = a.v - c.v;
                        face.vertex[1].u = a.u + c.u;
                        face.vertex[1].v = a.v - c.v;
                        face.vertex[2].u = a.u - c.u;
                        face.vertex[2].v = a.v + c.v;
                        face.vertex[3].u = a.u + c.u;
                        face.vertex[3].v = a.v + c.v;

                        Drawing.DrawTexturedBlendedQuad(face, face3D.color);
                    }
                    break;
                case FACE_FLAG.THREEDSPRITE:
                    if (a.z > 0)
                    {
                        int xpos = SCREEN_CENTERX + projectionX * a.x / a.z;
                        int ypos = SCREEN_CENTERY - projectionY * a.y / a.z;

                        ObjectScript scriptInfo = Script.objectScriptList[a.u];
                        SpriteFrame frame = Animation.scriptFrames[scriptInfo.frameListOffset + b.u];

                        switch (a.v)
                        {
                            case FX.SCALE:
                                Drawing.DrawScaledSprite((byte)b.v, xpos, ypos, -frame.pivotX, -frame.pivotY, c.u,
                                                 c.u, frame.width, frame.height, frame.spriteX, frame.spriteY,
                                                 scriptInfo.spriteSheetId);
                                break;
                            case FX.ROTATE:
                                Drawing.DrawRotatedSprite((byte)b.v, xpos, ypos, -frame.pivotX, -frame.pivotY, frame.spriteX, frame.spriteY,
                                                  frame.width, frame.height, c.v, scriptInfo.spriteSheetId);
                                break;
                            case FX.ROTOZOOM:
                                Drawing.DrawRotoZoomSprite((byte)b.v, xpos, ypos, -frame.pivotX, -frame.pivotY, frame.spriteX, frame.spriteY,
                                                   frame.width, frame.height, c.v, c.u,
                                                   scriptInfo.spriteSheetId);
                                break;
                        }
                    }
                    break;
            }
        }
    }
}
