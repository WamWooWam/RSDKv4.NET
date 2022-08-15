using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RSDKv4.External;

internal static class BitHelper
{
    [StructLayout(LayoutKind.Explicit)]
    private struct UInt32ToBytes
    {
        [FieldOffset(0)]
        public uint Value;

        [FieldOffset(0)]
        public byte B0;
        [FieldOffset(1)]
        public byte B1;
        [FieldOffset(2)]
        public byte B2;
        [FieldOffset(3)]
        public byte B3;
    }

    public static uint SwapBytes(uint x)
    {
        return ((x & 0x000000ff) << 24) +
               ((x & 0x0000ff00) << 8) +
               ((x & 0x00ff0000) >> 8) +
               ((x & 0xff000000) >> 24);
    }

    public static char ToHexDigit(int i)
    {
        return (char)(i + (i < 10 ? 48 : 87));
    }

    public static void CopyTo(uint value, byte[] array, int location)
    {
        var bytes = new UInt32ToBytes() { Value = value };
        array[location + 0] = bytes.B0;
        array[location + 1] = bytes.B1;
        array[location + 2] = bytes.B2;
        array[location + 3] = bytes.B3;
    }

    public static void SwapAndCopyTo(uint value, byte[] array, int location)
    {
        var bytes = new UInt32ToBytes() { Value = value };
        array[location + 0] = bytes.B3;
        array[location + 1] = bytes.B2;
        array[location + 2] = bytes.B1;
        array[location + 3] = bytes.B0;
    }

}
