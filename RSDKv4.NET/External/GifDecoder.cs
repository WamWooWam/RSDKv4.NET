namespace RSDKv4.External;

public class GifDecoder
{
    public byte[] buffer = new byte[256];
    public byte[] stack = new byte[4096];
    public byte[] suffix = new byte[4096];
    public uint[] prefix = new uint[4096];
    public int depth;
    public int clearCode;
    public int eofCode;
    public int runningCode;
    public int runningBits;
    public int prevCode;
    public int currentCode;
    public int maxCodePlusOne;
    public int stackPtr;
    public int shiftState;
    public int fileState;
    public int position;
    public int bufferSize;
    public uint shiftData;
    public uint pixelCount;
}
