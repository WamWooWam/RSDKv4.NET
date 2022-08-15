using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RSDKv4;

public struct DrawVertex3D : IVertexType
{
    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement[3]
    {
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    });

    public Vector3 position;
    public Vector2 texCoord;
    public Color color;

    public DrawVertex3D(Vector3 position, Vector2 texCoord, Color color)
    {
        this.position = position;
        this.texCoord = texCoord;
        this.color = color;
    }

    VertexDeclaration IVertexType.VertexDeclaration 
        => VertexDeclaration;
}
