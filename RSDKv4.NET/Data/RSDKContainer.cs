using RSDKv4.External;
using System.IO;
using System.Linq;
using System.Text;

namespace RSDKv4;

public enum RSDKVersion
{
    Invalid, v3, v4, vU
}

public class RSDKContainer
{
    public RSDKVersion dataVersion;

    public RSDKFileInfo[] files = new RSDKFileInfo[0x1000];
    public int fileCount = 0;

    public string[] packNames = new string[0x10];
    public int packCount = 0;

    private static readonly byte[] rsdkV4Magic = Encoding.UTF8.GetBytes("RSDKvB");
    private static readonly byte[] rsdkVUMagic = Encoding.UTF8.GetBytes("RSDKv4");

    public bool LoadRSDK(Stream fileHandle, BinaryReader fileReader, string filePath)
    {
        var magic = new byte[6];
        fileHandle.Read(magic, 0, 6);

        if (magic.SequenceEqual(rsdkV4Magic))
            dataVersion = RSDKVersion.v4;

        if (magic.SequenceEqual(rsdkVUMagic))
            dataVersion = RSDKVersion.vU;

        if (dataVersion == RSDKVersion.Invalid)
            return false;

        packNames[packCount] = filePath;

        this.fileCount = fileReader.ReadUInt16();
        for (int f = 0; f < fileCount; f++)
        {
            var file = files[f] = new RSDKFileInfo();

            byte[] b = new byte[4];
            file.hash.u1 = BitHelper.SwapBytes(fileReader.ReadUInt32());
            file.hash.u2 = BitHelper.SwapBytes(fileReader.ReadUInt32());
            file.hash.u3 = BitHelper.SwapBytes(fileReader.ReadUInt32());
            file.hash.u4 = BitHelper.SwapBytes(fileReader.ReadUInt32());

            file.offset = fileReader.ReadInt32();
            file.fileSize = fileReader.ReadInt32();
            file.encrypted = (file.fileSize & 0x80000000) != 0;
            file.fileSize &= 0x7FFFFFFF;
            file.packId = (byte)packCount;
        }
        return true;
    }
}
