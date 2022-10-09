using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RSDKv4.Native;

public struct RenderVertex : IVertexType
{
    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement[4]
    {
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    });

    public Vector3 position;
    public Vector3 normal;
    public Vector2 texCoord;
    public Color color;

    public RenderVertex(Vector3 position, Vector3 normal, Vector2 texCoord, Color color)
    {
        this.position = position;
        this.normal = normal;
        this.texCoord = texCoord;
        this.color = color;
    }

    VertexDeclaration IVertexType.VertexDeclaration
        => VertexDeclaration;
}
