using RSDKv4.External;

namespace RSDKv4;

public class RSDKFileInfo
{
    public MD5Digest hash;
    public int offset;
    public int fileSize;
    public bool encrypted;
    public byte packId;
}
