namespace RSDKv4;

public struct FunctionInfo
{
    public string name;
    public int opcodeSize;

    public FunctionInfo(string name, int size)
    {
        this.name = name;
        this.opcodeSize = size;
    }
}
