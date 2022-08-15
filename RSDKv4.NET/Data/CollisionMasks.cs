namespace RSDKv4;

public class CollisionMasks
{
    public sbyte[] floorMasks = new sbyte[Scene.TILE_COUNT * Scene.TILE_COUNT];
    public sbyte[] lWallMasks = new sbyte[Scene.TILE_COUNT * Scene.TILE_COUNT];
    public sbyte[] rWallMasks = new sbyte[Scene.TILE_COUNT * Scene.TILE_COUNT];
    public sbyte[] roofMasks = new sbyte[Scene.TILE_COUNT * Scene.TILE_COUNT];
    public uint[] angles = new uint[Scene.TILE_COUNT];
    public byte[] flags = new byte[Scene.TILE_COUNT];
}
