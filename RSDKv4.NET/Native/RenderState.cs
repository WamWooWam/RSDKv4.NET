using Microsoft.Xna.Framework;

namespace RSDKv4.Native;

public class RenderState
{
    public RenderVertex[] vertices;
    public short[] indices;
    public Matrix? renderMatrix;
    public int vertexOffset;
    public int indexOffset;
    public int indexCount;
    public int id;
    public byte blendMode;
    public bool useTexture;
    public bool useColours;
    public bool depthTest;
    public bool useNormals;
    public bool useFilter;
}
