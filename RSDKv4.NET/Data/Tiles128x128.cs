namespace RSDKv4;

public class Tiles128x128
{
    public int[] gfxDataPos = new int[Scene.CHUNKTILE_COUNT];
    public ushort[] tileIndex = new ushort[Scene.CHUNKTILE_COUNT];
    public byte[] direction = new byte[Scene.CHUNKTILE_COUNT];
    public byte[] visualPlane = new byte[Scene.CHUNKTILE_COUNT];
    public byte[][] collisionFlags = new byte[Scene.CPATH_COUNT][];

    public Tiles128x128()
    {
        for (int i = 0; i < Scene.CPATH_COUNT; i++)
            collisionFlags[i] = new byte[Scene.CHUNKTILE_COUNT];
    }
}
