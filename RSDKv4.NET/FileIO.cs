using Microsoft.Xna.Framework;
using RSDKv4.External;
using RSDKv4.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RSDKv4;

public static class FileIO
{
    public static RSDKContainer rsdkContainer = new RSDKContainer();

    public static string fileName;
    public static Stream fileHandle;
    public static BinaryReader fileReader;

    private static RSDKFileStream rsdkStream;
    private static byte[] readBuffer = new byte[8];

    public static bool CheckRSDKFile(string filePath)
    {
        try
        {
            fileName = $"Content/{filePath}";
            fileHandle = TitleContainer.OpenStream(fileName);
            fileReader = new BinaryReader(fileHandle);

            if (!rsdkContainer.LoadRSDK(fileHandle, fileReader, fileName))
                return false;

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
            return true;
        }
    }

    public static void CloseRSDKContainers()
    {
    }

    public static bool LoadFile(string filePath, out FileInfo fileInfo)
    {
        // TODO: fix
        return rsdkStream.LoadFile(filePath, out fileInfo);
    }

    public static bool CloseFile()
    {
        return false;
    }


    public static byte ReadByte()
    {
        rsdkStream.Read(readBuffer, 0, 1);
        return readBuffer[0];
    }

    public static sbyte ReadSByte()
    {
        rsdkStream.Read(readBuffer, 0, 1);
        return (sbyte)readBuffer[0];
    }

    public static int ReadInt32()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToInt32(readBuffer, 0);
    }

    public static uint ReadUInt32()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToUInt32(readBuffer, 0);
    }

    public static short ReadInt16()
    {
        rsdkStream.Read(readBuffer, 0, 2);
        return BitConverter.ToInt16(readBuffer, 0);
    }

    public static ushort ReadUInt16()
    {
        rsdkStream.Read(readBuffer, 0, 2);
        return BitConverter.ToUInt16(readBuffer, 0);
    }

    public static float ReadFloat()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToSingle(readBuffer, 0);
    }

    public static string ReadLengthPrefixedString()
    {
        return ReadString(ReadByte());
    }

    public static string ReadString(int length)
    {
        var buff = new char[length];
        for (int i = 0; i < length; i++)
            buff[i] = (char)ReadByte();

        return new string(buff);
    }

    public static string ReadStringLine()
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

    public static void ReadFile(byte[] buffer, int offset, int count)
    {
        rsdkStream.Read(buffer, offset, count);
    }

    public static void GetFileInfo(out FileInfo fileInfo)
    {
        rsdkStream.GetFileInfo(out fileInfo);
    }

    public static void SetFileInfo(FileInfo fileInfo)
    {
        // TODO: fix
        Debug.Assert(Engine.usingDataFile);
        rsdkStream.SetFileInfo(fileInfo);
    }

    public static int GetFilePosition()
    {
        return (int)rsdkStream.Position;
    }

    public static void SetFilePosition(int newPos)
    {
        rsdkStream.Position = newPos;
    }

    public static bool ReachedEndOfFile()
    {
        return rsdkStream.EndOfFile;
    }

    public static Stream CreateFileStream()
    {
        var fileHandle = TitleContainer.OpenStream(fileName);
        var stream = new RSDKFileStream(rsdkContainer, fileHandle);
        GetFileInfo(out var info);
        stream.SetFileInfo(info);
        return stream;
    }
}
