using System;

namespace RSDKv4;

public class FileInfo
{
    public string fileName;
    public int fileSize;
    public int vFileSize;
    public int readPos;
    public int bufferPosition;
    public int vFileOffset;

    public byte eStringPosA;
    public byte eStringPosB;
    public byte eStringNo;
    public byte eNybbleSwap;
    public bool useEncryption;
    public byte packID;

    public byte[] encryptionStringA = new byte[0x10];
    public byte[] encryptionStringB = new byte[0x10];

    public void Clone()
    {
        var clone = (FileInfo)this.MemberwiseClone();
        clone.encryptionStringA = new byte[0x10];
        clone.encryptionStringB = new byte[0x10];

        Buffer.BlockCopy(encryptionStringA, 0, clone.encryptionStringA, 0, 0x10);
        Buffer.BlockCopy(encryptionStringB, 0, clone.encryptionStringB, 0, 0x10);
    }
}
