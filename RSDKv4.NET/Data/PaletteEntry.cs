namespace RSDKv4;

public struct PaletteEntry
{
    public int paletteNum;
    public int startLine;
    public int endLine;

    public PaletteEntry(int palette, int startLine, int endLine)
    {
        this.paletteNum = palette;
        this.startLine = startLine;
        this.endLine = endLine;
    }
}
