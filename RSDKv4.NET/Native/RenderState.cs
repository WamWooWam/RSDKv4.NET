using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RSDKv4.Native;

public class RenderState
{
    public Effect effect;
    public TextureInfo texture;

    public RenderVertex[] renderVertices;
    public int vertexOffset;
    public int vertexCount;

    public short[] renderIndicies;
    public int indexOffset;
    public int indexCount;
    public int primitiveCount;

    public int id;
    public byte blendMode;
    public bool useTexture;
    public bool useColours;
    public bool depthTest;
    public bool useNormals;
    public bool useFilter;

    public Matrix? renderMatrix;
}
