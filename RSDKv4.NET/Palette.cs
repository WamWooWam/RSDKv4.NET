using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using static RSDKv4.Drawing;

namespace RSDKv4;

public class Palette
{
    public const int PALETTE_COUNT = (0x8);
    public const int PALETTE_SIZE = (0x100);

    public static int fadeMode;
    public static byte fadeA = 0;
    public static byte fadeR = 0;
    public static byte fadeG = 0;
    public static byte fadeB = 0;

    public static void SetPaletteEntry(byte paletteIndex, byte index, byte r, byte g, byte b)
    {
        Console.WriteLine($"{paletteIndex} {index}: {r} {g} {b} ({RGB888_TO_RGB5551(r, g, b)})");

        if (paletteIndex != 0xFF)
        {
            fullPalette[paletteIndex][index] = RGB888_TO_RGB5551(r, g, b);
            //if (paletteIndex != 0)
            //    fullPalette[paletteIndex][index] |= 1;
            fullPalette32[paletteIndex][index] = new Color(r, g, b);
        }
        else
        {
            fullPalette[texPaletteNum][index] = RGB888_TO_RGB5551(r, g, b);
            //if (paletteIndex != 0)
            //    fullPalette[texPaletteNum][index] |= 1;
            fullPalette32[texPaletteNum][index] = new Color(r, g, b);
        }
    }

    internal static void LoadPalette(string filePath, int paletteID, int startPaletteIndex, int startIndex, int endIndex)
    {
        string fullPath = "Data/Palettes/" + filePath;

        if (FileIO.LoadFile(fullPath, out var info))
        {
            FileIO.SetFilePosition(3 * startIndex);
            if (paletteID >= PALETTE_COUNT || paletteID < 0)
                paletteID = 0;

            byte[] colour = new byte[3];
            if (paletteID != 0)
            {
                for (int i = startIndex; i < endIndex; ++i)
                {
                    FileIO.ReadFile(colour, 0, 3);
                    SetPaletteEntry((byte)paletteID, (byte)startPaletteIndex++, colour[0], colour[1], colour[2]);
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; ++i)
                {
                    FileIO.ReadFile(colour, 0, 3);
                    SetPaletteEntry(0xFF, (byte)startPaletteIndex++, colour[0], colour[1], colour[2]);
                }
            }

            FileIO.CloseFile();
        }
    }


    internal static void RotatePalette(int palID, byte startIndex, byte endIndex, bool right)
    {
        if (right)
        {
            var startClr = fullPalette[palID][endIndex];
            var startClr32 = fullPalette32[palID][endIndex];
            for (int i = endIndex; i > startIndex; --i)
            {
                fullPalette[palID][i] = fullPalette[palID][i - 1];
                fullPalette32[palID][i] = fullPalette32[palID][i - 1];
            }
            fullPalette[palID][startIndex] = startClr;
            fullPalette32[palID][startIndex] = startClr32;
        }
        else
        {
            var startClr = fullPalette[palID][startIndex];
            var startClr32 = fullPalette32[palID][startIndex];
            for (int i = startIndex; i < endIndex; ++i)
            {
                fullPalette[palID][i] = fullPalette[palID][i + 1];
                fullPalette32[palID][i] = fullPalette32[palID][i + 1];
            }
            fullPalette[palID][endIndex] = startClr;
            fullPalette32[palID][endIndex] = startClr32;
        }
    }

    internal static void SetActivePalette(byte newActivePal, int startLine, int endLine)
    {
        if (newActivePal < PALETTE_COUNT)
            texPaletteNum = newActivePal;
    }

    internal static void SetPaletteFade(byte destPaletteID, byte srcPaletteA, byte srcPaletteB, ushort blendAmount, int startIndex, int endIndex)
    {
        if (destPaletteID >= PALETTE_COUNT || srcPaletteA >= PALETTE_COUNT || srcPaletteB >= PALETTE_COUNT)
            return;

        if (blendAmount >= 0x100)
            blendAmount = 0xFF;

        if (startIndex >= endIndex)
            return;

        var blendA = (uint)(0xFF - blendAmount);
        var idx = startIndex;
        for (int l = startIndex; l < endIndex; ++l)
        {
            var srcB = fullPalette32[srcPaletteB][l];
            var srcA = fullPalette32[srcPaletteA][l];

            fullPalette[destPaletteID][idx] = RGB888_TO_RGB5551(
                (byte)((ushort)(srcB.R * blendAmount + blendA * srcA.R) >> 8),
                (byte)((ushort)(srcB.G * blendAmount + blendA * srcA.G) >> 8),
                (byte)((ushort)(srcB.B * blendAmount + blendA * srcA.B) >> 8));
            fullPalette32[destPaletteID][idx].R = (byte)((ushort)(srcB.R * blendAmount + blendA * srcA.R) >> 8);
            fullPalette32[destPaletteID][idx].G = (byte)((ushort)(srcB.G * blendAmount + blendA * srcA.G) >> 8);
            fullPalette32[destPaletteID][idx].B = (byte)((ushort)(srcB.B * blendAmount + blendA * srcA.B) >> 8);
            //#if RETRO_HARDWARE_RENDER
            fullPalette[destPaletteID][idx] |= 1;
            //#endif

            idx++;
        }

        if (destPaletteID < PALETTE_COUNT)
            texPaletteNum = destPaletteID;
    }

    internal static void SetPaletteEntryPacked(byte paletteIndex, byte index, uint colour)
    {
        var fullPalette = Drawing.fullPalette;
        var fullPalette32 = Drawing.fullPalette32;

        fullPalette[paletteIndex][index] = RGB888_TO_RGB5551((byte)(colour >> 16), (byte)(colour >> 8), (byte)(colour >> 0));
        if (index != 0)
            fullPalette[paletteIndex][index] |= 1;
        fullPalette32[paletteIndex][index].R = (byte)(colour >> 16);
        fullPalette32[paletteIndex][index].G = (byte)(colour >> 8);
        fullPalette32[paletteIndex][index].B = (byte)(colour >> 0);
    }

    internal static int GetPaletteEntryPacked(byte paletteIndex, byte index)
    {
        var clr = fullPalette32[paletteIndex][index];
        return (clr.R << 16) | (clr.G << 8) | (clr.B);
    }

    internal static void CopyPalette(byte sourcePalette, byte srcPaletteStart, byte destinationPalette, byte destPaletteStart, ushort count)
    {
        var fullPalette = Drawing.fullPalette;
        var fullPalette32 = Drawing.fullPalette32;

        if (sourcePalette < PALETTE_COUNT && destinationPalette < PALETTE_COUNT)
        {
            for (int i = 0; i < count; ++i)
            {
                fullPalette[destinationPalette][destPaletteStart + i] = fullPalette[sourcePalette][srcPaletteStart + i];
                fullPalette32[destinationPalette][destPaletteStart + i] = fullPalette32[sourcePalette][srcPaletteStart + i];
            }
        }
    }
}
