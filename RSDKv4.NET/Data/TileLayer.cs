namespace RSDKv4;

public class TileLayer
{
    public ushort[] tiles = new ushort[Scene.TILELAYER_CHUNK_MAX];
    public byte[] lineScroll = new byte[Scene.TILELAYER_SCROLL_MAX];
    public int parallaxFactor;
    public int scrollSpeed;
    public int scrollPos;
    public int angle;
    public int xpos;
    public int ypos;
    public int zpos;
    public int deformationOffset;
    public int deformationOffsetW;
    public byte type;
    public byte xsize;
    public byte ysize;
}
