using Microsoft.Xna.Framework;

namespace RSDKv4.Native;

public abstract class MenuButton : NativeEntity
{
    public int field_10;
    public bool visible;
    public int field_18;
    public int field_1C;
    public float x;
    public float y;
    public float z;
    public MeshInfo mesh;
    public float angle;
    public float scale;
    public int textureCircle;
    public byte r;
    public byte g;
    public byte b;
    public Matrix renderMatrix;
    public Matrix matrixTemp;
    public TextLabel label;
}
