using RSDKv4.External;
using System.IO;
using System.Text;

namespace RSDKv4;

public class RSDKContainer
{
    public RSDKFileInfo[] files = new RSDKFileInfo[0x1000];
    public int fileCount = 0;

    public string[] packNames = new string[0x10];
    public int packCount = 0;

    private static readonly byte[] rsdkMagic = Encoding.UTF8.GetBytes("RSDKvB");

    public bool LoadRSDK(Stream fileHandle, BinaryReader fileReader, string filePath)
    {
        for (int i = 0; i < 6; i++)
        {
            if (fileHandle.ReadByte() != rsdkMagic[i]) return false;
        }

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
            file.packID = (byte)packCount;
        }
        return true;
    }
}
