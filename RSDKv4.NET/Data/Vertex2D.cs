using System.Diagnostics;

namespace RSDKv4;

[DebuggerDisplay("x={x},y={y}")]
public struct Vertex2D
{
    public int x;
    public int y;
    public int u;
    public int v;
}
