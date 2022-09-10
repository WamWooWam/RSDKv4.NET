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
    public byte packId;

    public byte[] encryptionKeyA = new byte[0x10];
    public byte[] encryptionKeyB = new byte[0x10];

    public void Clone()
    {
        var clone = (FileInfo)this.MemberwiseClone();
        clone.encryptionKeyA = new byte[0x10];
        clone.encryptionKeyB = new byte[0x10];

        Buffer.BlockCopy(encryptionKeyA, 0, clone.encryptionKeyA, 0, 0x10);
        Buffer.BlockCopy(encryptionKeyB, 0, clone.encryptionKeyB, 0, 0x10);
    }
}
