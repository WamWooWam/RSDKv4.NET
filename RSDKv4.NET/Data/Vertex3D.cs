using System.Diagnostics;

namespace RSDKv4;

[DebuggerDisplay("x={x},y={y},z={z}")]
public struct Vertex3D
{
    public int x;
    public int y;
    public int z;
    public int u;
    public int v;
}
