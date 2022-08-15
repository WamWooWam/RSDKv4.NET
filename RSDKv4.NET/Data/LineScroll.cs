namespace RSDKv4;

public class LineScroll
{
    public int[] parallaxFactor = new int[Scene.PARALLAX_COUNT];
    public int[] scrollSpeed = new int[Scene.PARALLAX_COUNT];
    public int[] scrollPos = new int[Scene.PARALLAX_COUNT];
    public int[] linePos = new int[Scene.PARALLAX_COUNT];
    public int[] deform = new int[Scene.PARALLAX_COUNT];
    public byte entryCount;
}
