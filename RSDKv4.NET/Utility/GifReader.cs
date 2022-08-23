using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.External;
using System.IO;

namespace RSDKv4.Utility;

public class GifReader
{
    private static GifDecoder gifDecoder = new GifDecoder();

    private static int[] codeMasks = new int[13] { 0, 1, 3, 7, 15, 31, 63, 127, 255, 511, 1023, 2047, 4095 };
    public const int LOADING_IMAGE = 0;
    public const int LOAD_COMPLETE = 1;
    public const int LZ_MAX_CODE = 4095;
    public const int LZ_BITS = 12;
    public const int FLUSH_OUTPUT = 4096;
    public const int FIRST_CODE = 4097;
    public const int NO_SUCH_CODE = 4098;
    public const int HT_SIZE = 8192;
    public const int HT_KEY_MASK = 8191;

    private Stream fileStream;

    public void InitGifDecoder()
    {
        int num = (int)fileStream.ReadByte();
        gifDecoder.fileState = 0;
        gifDecoder.position = 0;
        gifDecoder.bufferSize = 0;
        gifDecoder.buffer[0] = (byte)0;
        gifDecoder.depth = num;
        gifDecoder.clearCode = 1 << num;
        gifDecoder.eofCode = gifDecoder.clearCode + 1;
        gifDecoder.runningCode = gifDecoder.eofCode + 1;
        gifDecoder.runningBits = num + 1;
        gifDecoder.maxCodePlusOne = 1 << gifDecoder.runningBits;
        gifDecoder.stackPtr = 0;
        gifDecoder.prevCode = 4098;
        gifDecoder.shiftState = 0;
        gifDecoder.shiftData = 0U;
        for (int index = 0; index <= 4095; ++index)
            gifDecoder.prefix[index] = 4098U;
    }

    public Texture2D LoadGIFFile(GraphicsDevice device, Stream fileStream)
    {
        byte[] byteP = new byte[3];
        bool interlaced = false;

        this.fileStream = fileStream;
        fileStream.Seek(6, SeekOrigin.Begin);

        byteP[0] = (byte)fileStream.ReadByte();
        int num1 = byteP[0];
        byteP[0] = (byte)fileStream.ReadByte();
        int width = num1 + (byteP[0] << 8);
        byteP[0] = (byte)fileStream.ReadByte();
        int num2 = byteP[0];
        byteP[0] = (byte)fileStream.ReadByte();
        int height = num2 + (byteP[0] << 8);
        byteP[0] = (byte)fileStream.ReadByte();
        byteP[0] = (byte)fileStream.ReadByte();
        byteP[0] = (byte)fileStream.ReadByte();

        var table = new Color[256];
        for (int index = 0; index < 256; ++index)
        {
            fileStream.Read(byteP, 0, 3);
            table[index] = new Color(byteP[0], byteP[1], byteP[2]);
        }

        table[0] = Color.Transparent;

        byteP[0] = (byte)fileStream.ReadByte();
        while (byteP[0] != 44)
            byteP[0] = (byte)fileStream.ReadByte();
        if (byteP[0] == 44)
        {
            fileStream.Read(byteP, 0, 2);
            fileStream.Read(byteP, 0, 2);
            fileStream.Read(byteP, 0, 2);
            fileStream.Read(byteP, 0, 2);
            byteP[0] = (byte)fileStream.ReadByte();
            if ((byteP[0] & 64) >> 6 == 1)
                interlaced = true;
            if ((byteP[0] & 128) >> 7 == 1)
            {
                for (int index = 128; index < 256; ++index)
                    fileStream.Read(byteP, 0, 3);
            }
            //_surfaces[surfaceNum].width = width;
            //_surfaces[surfaceNum].height = height;
            //_surfaces[surfaceNum].dataPosition = graphicsBufferPos;
            //graphicsBufferPos += _surfaces[surfaceNum].width * _surfaces[surfaceNum].height;
            //if (graphicsBufferPos >= 4194304)
            //    graphicsBufferPos = 0;
            //else

            var dataSize = width * height;
            var data = new byte[dataSize];
            ReadGifPictureData(width, height, interlaced, ref data, 0);

            var textureData = new Color[dataSize];
            for (int i = 0; i < dataSize; i++)            
                textureData[i] = table[data[i]];

            var texture = new Texture2D(device, width, height);
            texture.SetData(textureData);
            return texture;
        }

        return null;
    }

    public void ReadGifPictureData(
        int width,
        int height,
        bool interlaced,
        ref byte[] gfxData,
        int offset)
    {
        int[] numArray1 = new int[4] { 0, 4, 2, 1 };
        int[] numArray2 = new int[4] { 8, 8, 4, 2 };
        InitGifDecoder();
        if (interlaced)
        {
            for (int index1 = 0; index1 < 4; ++index1)
            {
                for (int index2 = numArray1[index1]; index2 < height; index2 += numArray2[index1])
                    ReadGifLine(ref gfxData, width, index2 * width + offset);
            }
        }
        else
        {
            for (int index = 0; index < height; ++index)
                ReadGifLine(ref gfxData, width, index * width + offset);
        }
    }

    public void ReadGifLine(ref byte[] line, int length, int offset)
    {
        int num1 = 0;
        int stackPtr = gifDecoder.stackPtr;
        int eofCode = gifDecoder.eofCode;
        int clearCode = gifDecoder.clearCode;
        int code1 = gifDecoder.prevCode;
        if (stackPtr != 0)
        {
            for (; stackPtr != 0 && num1 < length; ++num1)
                line[offset++] = gifDecoder.stack[--stackPtr];
        }
        while (num1 < length)
        {
            int code2 = ReadGifCode();
            if (code2 == eofCode)
            {
                if (num1 != length - 1 | gifDecoder.pixelCount != 0U)
                    return;
                ++num1;
            }
            else if (code2 == clearCode)
            {
                for (int index = 0; index <= 4095; ++index)
                    gifDecoder.prefix[index] = 4098U;
                gifDecoder.runningCode = gifDecoder.eofCode + 1;
                gifDecoder.runningBits = gifDecoder.depth + 1;
                gifDecoder.maxCodePlusOne = 1 << gifDecoder.runningBits;
                code1 = gifDecoder.prevCode = 4098;
            }
            else
            {
                if (code2 < clearCode)
                {
                    line[offset] = (byte)code2;
                    ++offset;
                    ++num1;
                }
                else
                {
                    if (code2 < 0 | code2 > 4095)
                        return;
                    int index;
                    if (gifDecoder.prefix[code2] == 4098U)
                    {
                        if (code2 != gifDecoder.runningCode - 2)
                            return;
                        index = code1;
                        gifDecoder.suffix[gifDecoder.runningCode - 2] = gifDecoder.stack[stackPtr++] = TracePrefix(ref gifDecoder.prefix, code1, clearCode);
                    }
                    else
                        index = code2;
                    int num2;
                    for (num2 = 0; num2++ <= 4095 && index > clearCode && index <= 4095; index = (int)gifDecoder.prefix[index])
                        gifDecoder.stack[stackPtr++] = gifDecoder.suffix[index];
                    if (num2 >= 4095 | index > 4095)
                        return;
                    for (gifDecoder.stack[stackPtr++] = (byte)index; stackPtr != 0 && num1 < length; ++num1)
                        line[offset++] = gifDecoder.stack[--stackPtr];
                }
                if (code1 != 4098)
                {
                    if (gifDecoder.runningCode < 2 | gifDecoder.runningCode > 4097)
                        return;
                    gifDecoder.prefix[gifDecoder.runningCode - 2] = (uint)code1;
                    gifDecoder.suffix[gifDecoder.runningCode - 2] = code2 != gifDecoder.runningCode - 2 ? TracePrefix(ref gifDecoder.prefix, code2, clearCode) : TracePrefix(ref gifDecoder.prefix, code1, clearCode);
                }
                code1 = code2;
            }
        }
        gifDecoder.prevCode = code1;
        gifDecoder.stackPtr = stackPtr;
    }

    private int ReadGifCode()
    {
        for (; gifDecoder.shiftState < gifDecoder.runningBits; gifDecoder.shiftState += 8)
        {
            byte num = ReadGifByte();
            gifDecoder.shiftData |= (uint)num << gifDecoder.shiftState;
        }
        int num1 = (int)((long)gifDecoder.shiftData & (long)codeMasks[gifDecoder.runningBits]);
        gifDecoder.shiftData >>= gifDecoder.runningBits;
        gifDecoder.shiftState -= gifDecoder.runningBits;
        if (++gifDecoder.runningCode > gifDecoder.maxCodePlusOne && gifDecoder.runningBits < 12)
        {
            gifDecoder.maxCodePlusOne <<= 1;
            ++gifDecoder.runningBits;
        }
        return num1;
    }

    private byte ReadGifByte()
    {
        char minValue = char.MinValue;
        if (gifDecoder.fileState == 1)
            return (byte)minValue;
        byte num1;
        if (gifDecoder.position == gifDecoder.bufferSize)
        {
            byte num2 = (byte)fileStream.ReadByte();
            gifDecoder.bufferSize = (int)num2;
            if (gifDecoder.bufferSize == 0)
            {
                gifDecoder.fileState = 1;
                return (byte)minValue;
            }
            fileStream.Read(gifDecoder.buffer, 0, gifDecoder.bufferSize);
            num1 = gifDecoder.buffer[0];
            gifDecoder.position = 1;
        }
        else
            num1 = gifDecoder.buffer[gifDecoder.position++];
        return num1;
    }

    private byte TracePrefix(ref uint[] prefix, int code, int clearCode)
    {
        int num = 0;
        while (code > clearCode && num++ <= 4095)
            code = (int)prefix[code];
        return (byte)code;
    }
}
