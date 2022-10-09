namespace RSDKv4;

public class TextMenu
{
    public char[] textData = new char[Text.TEXTDATA_COUNT];
    public int[] entryStart = new int[Text.TEXTENTRY_COUNT];
    public int[] entrySize = new int[Text.TEXTENTRY_COUNT];
    public bool[] entryHighlight = new bool[Text.TEXTENTRY_COUNT];
    public int textDataPos;
    public int selection1;
    public int selection2;
    public ushort rowCount;
    public ushort visibleRowCount;
    public ushort visibleRowOffset;
    public byte selectionCount;
    public sbyte timer;
    public int surfaceId;

    public int alignment;
}
