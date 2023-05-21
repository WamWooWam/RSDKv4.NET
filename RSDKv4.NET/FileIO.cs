using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using RSDKv4.Utility;

namespace RSDKv4;

public class FileIO
{
    public RSDKContainer rsdkContainer = new RSDKContainer();

    public string fileName;
    public Stream fileHandle;
    public BinaryReader fileReader;

    private RSDKFileStream rsdkStream;
    private byte[] readBuffer = new byte[8];

    public Engine Engine { get; }

    public FileIO(Engine engine)
    {
        Engine = engine;
    }

    public bool CheckRSDKFile(string filePath)
    {
        try
        {
            fileName = $"Content/{filePath}";
            fileHandle = TitleContainer.OpenStream(fileName);
            fileReader = new BinaryReader(fileHandle);

            if (!rsdkContainer.LoadRSDK(fileHandle, fileReader, fileName))
                return false;

            if (rsdkContainer.dataVersion == RSDKVersion.vU)
                Engine.engineType = ENGINE_TYPE.ULTIMATE;

            Engine.usingDataFile = true;
            Engine.usingBytecode = false;
            rsdkStream = new RSDKFileStream(rsdkContainer, fileHandle);

            if (!LoadFile("Bytecode/GlobalCode.bin", out var fData))
                return false;

            Engine.usingBytecode = true;
            CloseFile();
            return true;
        }
        catch (Exception)
        {
            Engine.usingDataFile = false;
            Engine.usingBytecode = false;
            return false;
        }
    }

    public void CloseRSDKContainers()
    {
    }

    public bool LoadFile(string filePath, out FileInfo fileInfo)
    {
        // TODO: fix
        return rsdkStream.LoadFile(filePath, out fileInfo);
    }

    public bool CloseFile()
    {
        return false;
    }

    public byte ReadByte()
    {
        rsdkStream.Read(readBuffer, 0, 1);
        return readBuffer[0];
    }

    public sbyte ReadSByte()
    {
        rsdkStream.Read(readBuffer, 0, 1);
        return (sbyte)readBuffer[0];
    }

    public int ReadInt32()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToInt32(readBuffer, 0);
    }

    public uint ReadUInt32()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToUInt32(readBuffer, 0);
    }

    public short ReadInt16()
    {
        rsdkStream.Read(readBuffer, 0, 2);
        return BitConverter.ToInt16(readBuffer, 0);
    }

    public ushort ReadUInt16()
    {
        rsdkStream.Read(readBuffer, 0, 2);
        return BitConverter.ToUInt16(readBuffer, 0);
    }

    public float ReadFloat()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToSingle(readBuffer, 0);
    }

    public string ReadLengthPrefixedString()
    {
        return ReadString(ReadByte());
    }

    public string ReadString(int length)
    {
        var buff = new char[length];
        for (int i = 0; i < length; i++)
            buff[i] = (char)ReadByte();

        return new string(buff);
    }

    public string ReadStringLine()
    {
        char curChar;
        var builder = new StringBuilder();

        //int textPos = 0;
        while (true)
        {
            curChar = (char)ReadByte();
            if (curChar == '\r' || curChar == '\n')
                break;
            if (curChar != ';' && curChar != '\t' && curChar != ' ')
                builder.Append(curChar);

            if (ReachedEndOfFile())
            {
                return builder.ToString();
            }
        }

        if (curChar != '\n' && curChar != '\r')
        {
            if (ReachedEndOfFile())
            {
                return builder.ToString();
            }
        }

        return builder.ToString();
    }

    public void ReadFile(byte[] buffer, int offset, int count)
    {
        rsdkStream.Read(buffer, offset, count);
    }

    public void GetFileInfo(out FileInfo fileInfo)
    {
        rsdkStream.GetFileInfo(out fileInfo);
    }

    public void SetFileInfo(FileInfo fileInfo)
    {
        // TODO: fix
        Debug.Assert(Engine.usingDataFile);
        rsdkStream.SetFileInfo(fileInfo);
    }

    public int GetFilePosition()
    {
        return (int)rsdkStream.Position;
    }

    public void SetFilePosition(int newPos)
    {
        rsdkStream.Position = newPos;
    }

    public bool ReachedEndOfFile()
    {
        return rsdkStream.EndOfFile;
    }

    public Stream CreateFileStream()
    {
        var fileHandle = TitleContainer.OpenStream(fileName);
        var stream = new RSDKFileStream(rsdkContainer, fileHandle);
        GetFileInfo(out var info);
        stream.SetFileInfo(info);
        return stream;
    }

    public bool TryGetFileStream(string file, out Stream stream)
    {
        var fileHandle = TitleContainer.OpenStream(fileName);
        var rsdkStream = new RSDKFileStream(rsdkContainer, fileHandle);

        if (rsdkStream.LoadFile(file, out _))
        {
            stream = rsdkStream;
            return true;
        }
        else
        {
            rsdkStream.Dispose();
            stream = null;
            return false;
        }
    }
}
