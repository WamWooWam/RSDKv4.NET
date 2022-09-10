using RSDKv4.External;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace RSDKv4.Utility;

public class RSDKFileStream : Stream
{
    public string fileName;
    public byte[] fileBuffer = new byte[0x2000];
    public int fileSize;
    public int vFileSize;
    public int readPos;
    public int readSize;
    public int bufferPosition;
    public int vFileOffset;
    public bool useEncryption;
    public byte packId;
    public byte eStringPosA;
    public byte eStringPosB;
    public byte eStringNo;
    public byte eNybbleSwap;
    public byte[] encryptionStringA = new byte[0x10];
    public byte[] encryptionStringB = new byte[0x10];

    public bool usingDataFile = true;

    private const uint ENC_KEY_2 = 0x24924925;
    private const uint ENC_KEY_1 = 0xAAAAAAAB;

    private static int mulUnsignedHigh(uint arg1, int arg2) { return (int)(((ulong)arg1 * (ulong)arg2) >> 32); }

    private RSDKContainer rsdkContainer;
    private Stream fileHandle;

    public RSDKFileStream(RSDKContainer container, Stream fileHandle)
    {
        this.rsdkContainer = container;
        this.fileHandle = fileHandle;
        this.fileSize = (int)fileHandle.Length;
    }

    public override bool CanRead
        => true;
    public override bool CanSeek
        => true;
    public override bool CanWrite
        => false;
    public override long Length
        => vFileSize;
    public override long Position
    {
        get => GetPosition();
        set => SetPosition((int)value);
    }

    public bool EndOfFile
    {
        get
        {
            if (usingDataFile)
                return bufferPosition + readPos - readSize - vFileOffset >= vFileSize;
            else
                return bufferPosition + readPos - readSize >= fileSize;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long pos = 0;
        switch (origin)
        {
            case SeekOrigin.Begin:
                pos = offset;
                break;
            case SeekOrigin.Current:
                pos = Position + offset;
                break;
            case SeekOrigin.End:
                pos = Length - offset;
                break;
        }

        SetPosition((int)pos);
        return GetPosition();
    }

    public override int Read(byte[] dest, int offset, int count)
    {
        var data = dest;
        var cnt = count;
        var read = 0;

        if (readPos <= fileSize)
        {
            if (useEncryption)
            {
                while (count > 0)
                {
                    if (EndOfFile)
                        return read;

                    if (bufferPosition == readSize)
                        FillFileBuffer();

                    data[offset] = (byte)(encryptionStringB[eStringPosB] ^ eStringNo ^ fileBuffer[bufferPosition++]);
                    if (eNybbleSwap != 0)
                        data[offset] = (byte)(((data[offset] << 4) + (data[offset] >> 4)) & 0xFF);
                    data[offset] ^= encryptionStringA[eStringPosA];

                    ++eStringPosA;
                    ++eStringPosB;
                    if (eStringPosA <= 0x0F)
                    {
                        if (eStringPosB > 0x0C)
                        {
                            eStringPosB = 0;
                            eNybbleSwap ^= 0x01;
                        }
                    }
                    else if (eStringPosB <= 0x08)
                    {
                        eStringPosA = 0;
                        eNybbleSwap ^= 0x01;
                    }
                    else
                    {
                        eStringNo += 2;
                        eStringNo &= 0x7F;

                        if (eNybbleSwap != 0)
                        {
                            int key1 = mulUnsignedHigh(ENC_KEY_1, eStringNo);
                            int key2 = mulUnsignedHigh(ENC_KEY_2, eStringNo);
                            eNybbleSwap = 0;

                            int temp1 = key2 + (eStringNo - key2) / 2;
                            int temp2 = key1 / 8 * 3;

                            eStringPosA = (byte)(eStringNo - temp1 / 4 * 7);
                            eStringPosB = (byte)(eStringNo - temp2 * 4 + 2);
                        }
                        else
                        {
                            int key1 = mulUnsignedHigh(ENC_KEY_1, eStringNo);
                            int key2 = mulUnsignedHigh(ENC_KEY_2, eStringNo);
                            eNybbleSwap = 1;

                            int temp1 = key2 + (eStringNo - key2) / 2;
                            int temp2 = key1 / 8 * 3;

                            eStringPosB = (byte)(eStringNo - temp1 / 4 * 7);
                            eStringPosA = (byte)(eStringNo - temp2 * 4 + 3);
                        }
                    }

                    ++read;
                    ++offset;
                    --count;
                }
            }
            else
            {
                while (count > 0)
                {
                    if (bufferPosition == readSize)
                        FillFileBuffer();

                    var copyCount = Math.Min(count, readSize - bufferPosition);

                    Buffer.BlockCopy(fileBuffer, bufferPosition, dest, offset, copyCount);

                    read += copyCount;
                    offset += copyCount;
                    count -= copyCount;
                    bufferPosition += copyCount;
                }
            }

            return read;
        }

        return 0;
    }

    internal bool LoadFile(string filePath, out FileInfo fileInfo)
    {
        fileInfo = new FileInfo();

        if (usingDataFile)
        {
            fileName = fileInfo.fileName = filePath.ToLowerInvariant();
            MD5Hash.GenerateFromString(fileInfo.fileName, out var digest);

            for (int f = 0; f < rsdkContainer.fileCount; ++f)
            {
                var file = rsdkContainer.files[f];
                if (file.hash != digest)
                    continue;

                try
                {
                    packId = file.packId;
                    vFileSize = file.fileSize;
                    vFileOffset = file.offset;
                    readPos = file.offset;
                    readSize = 0;
                    bufferPosition = 0;
                    fileHandle.Seek(vFileOffset, SeekOrigin.Begin);

                    useEncryption = file.encrypted;
                    fileInfo.encryptionKeyA = new byte[0x10];
                    fileInfo.encryptionKeyB = new byte[0x10];
                    if (useEncryption)
                    {
                        GenerateELoadKeys(fileInfo, fileName, (uint)vFileSize);
                        eStringNo = (byte)((vFileSize & 0x1FC) >> 2);
                        eStringPosA = 0;
                        eStringPosB = 8;
                        eNybbleSwap = 0;
                    }

                    fileInfo.readPos = readPos;
                    fileInfo.fileSize = fileSize;
                    fileInfo.vFileSize = vFileSize;
                    fileInfo.vFileOffset = vFileOffset;
                    fileInfo.eStringNo = eStringNo;
                    fileInfo.eStringPosB = eStringPosB;
                    fileInfo.eStringPosA = eStringPosA;
                    fileInfo.eNybbleSwap = eNybbleSwap;
                    fileInfo.bufferPosition = bufferPosition;
                    fileInfo.useEncryption = useEncryption;
                    fileInfo.packId = packId;
                    Buffer.BlockCopy(fileInfo.encryptionKeyA, 0, encryptionStringA, 0, 0x10);
                    Buffer.BlockCopy(fileInfo.encryptionKeyB, 0, encryptionStringB, 0, 0x10);

                    Debug.WriteLine("Loaded data file {0}", (object)filePath);

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error while trying to read data file {0}\n{1}", filePath, ex);
                }
            }
        }

        fileInfo = null;
        return false;
    }

    public int GetPosition()
    {
        if (usingDataFile)
            return bufferPosition + readPos - readSize - vFileOffset;
        else
            return bufferPosition + readPos - readSize;
    }

    public void SetPosition(int newPos)
    {
        var oldPos = GetPosition();
        if (newPos == oldPos)
            return;

        if (useEncryption)
        {
            readPos = vFileOffset + newPos;
            eStringNo = (byte)((vFileSize & 0x1FC) >> 2);
            eStringPosA = 0;
            eStringPosB = 8;
            eNybbleSwap = 0;
            while (newPos != 0)
            {
                ++eStringPosA;
                ++eStringPosB;
                if (eStringPosA <= 0x0F)
                {
                    if (eStringPosB > 0x0C)
                    {
                        eStringPosB = 0;
                        eNybbleSwap ^= 0x01;
                    }
                }
                else if (eStringPosB <= 0x08)
                {
                    eStringPosA = 0;
                    eNybbleSwap ^= 0x01;
                }
                else
                {
                    eStringNo += 2;
                    eStringNo &= 0x7F;

                    if (eNybbleSwap != 0)
                    {
                        int key1 = mulUnsignedHigh(ENC_KEY_1, eStringNo);
                        int key2 = mulUnsignedHigh(ENC_KEY_2, eStringNo);
                        eNybbleSwap = 0;

                        int temp1 = key2 + (eStringNo - key2) / 2;
                        int temp2 = key1 / 8 * 3;

                        eStringPosA = (byte)(eStringNo - temp1 / 4 * 7);
                        eStringPosB = (byte)(eStringNo - temp2 * 4 + 2);
                    }
                    else
                    {
                        int key1 = mulUnsignedHigh(ENC_KEY_1, eStringNo);
                        int key2 = mulUnsignedHigh(ENC_KEY_2, eStringNo);
                        eNybbleSwap = 1;

                        int temp1 = key2 + (eStringNo - key2) / 2;
                        int temp2 = key1 / 8 * 3;

                        eStringPosB = (byte)(eStringNo - temp1 / 4 * 7);
                        eStringPosA = (byte)(eStringNo - temp2 * 4 + 3);
                    }
                }
                --newPos;
            }
        }
        else
        {
            if (usingDataFile)
                readPos = vFileOffset + newPos;
            else
                readPos = newPos;
        }

        fileHandle.Seek(readPos, SeekOrigin.Begin);
        FillFileBuffer();
    }

    public int FillFileBuffer()
    {
        if (readPos + 0x2000 <= fileSize)
            readSize = 0x2000;
        else
            readSize = fileSize - readPos;

        var result = fileHandle.Read(fileBuffer, 0, readSize);
        readPos += readSize;
        bufferPosition = 0;
        return result;
    }


    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public void GetFileInfo(out FileInfo fileInfo)
    {
        fileInfo = new FileInfo();
        fileInfo.fileName = fileName;
        fileInfo.bufferPosition = bufferPosition;
        fileInfo.readPos = readPos - readSize;
        fileInfo.fileSize = fileSize;
        fileInfo.vFileSize = vFileSize;
        fileInfo.vFileOffset = vFileOffset;
        fileInfo.eStringPosA = eStringPosA;
        fileInfo.eStringPosB = eStringPosB;
        fileInfo.eStringNo = eStringNo;
        fileInfo.eNybbleSwap = eNybbleSwap;
        fileInfo.useEncryption = useEncryption;
        fileInfo.packId = packId;
        Buffer.BlockCopy(encryptionStringA, 0, fileInfo.encryptionKeyA, 0, 0x10);
        Buffer.BlockCopy(encryptionStringB, 0, fileInfo.encryptionKeyB, 0, 0x10);
    }

    public void SetFileInfo(FileInfo fileInfo)
    {
        vFileOffset = fileInfo.vFileOffset;
        vFileSize = fileInfo.vFileSize;
        fileSize = (int)fileHandle.Length;
        readPos = fileInfo.readPos;

        fileHandle.Seek(readPos, SeekOrigin.Begin);
        FillFileBuffer();

        bufferPosition = fileInfo.bufferPosition;
        eStringPosA = fileInfo.eStringPosA;
        eStringPosB = fileInfo.eStringPosB;
        eStringNo = fileInfo.eStringNo;
        eNybbleSwap = fileInfo.eNybbleSwap;
        useEncryption = fileInfo.useEncryption;
        packId = fileInfo.packId;
        Buffer.BlockCopy(fileInfo.encryptionKeyA, 0, encryptionStringA, 0, 0x10);
        Buffer.BlockCopy(fileInfo.encryptionKeyB, 0, encryptionStringB, 0, 0x10);
    }

    public void GenerateELoadKeys(FileInfo info, string fileName, uint fileSize)
    {
        string key1;
        string key2;

        if (rsdkContainer.dataVersion == RSDKVersion.vU)
        {
            key1 = fileName.ToUpperInvariant();
            key2 = fileSize.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            key1 = fileSize.ToString(CultureInfo.InvariantCulture);
            key2 = ((uint)((vFileSize >> 1) + 1)).ToString(CultureInfo.InvariantCulture);
        }

        MD5Hash.GenerateFromString(key1, out var hash);
        BitHelper.SwapAndCopyTo(hash.u1, info.encryptionKeyA, 0);
        BitHelper.SwapAndCopyTo(hash.u2, info.encryptionKeyA, 4);
        BitHelper.SwapAndCopyTo(hash.u3, info.encryptionKeyA, 8);
        BitHelper.SwapAndCopyTo(hash.u4, info.encryptionKeyA, 12);


        MD5Hash.GenerateFromString(key2, out hash);
        BitHelper.SwapAndCopyTo(hash.u1, info.encryptionKeyB, 0);
        BitHelper.SwapAndCopyTo(hash.u2, info.encryptionKeyB, 4);
        BitHelper.SwapAndCopyTo(hash.u3, info.encryptionKeyB, 8);
        BitHelper.SwapAndCopyTo(hash.u4, info.encryptionKeyB, 12);
    }
}
