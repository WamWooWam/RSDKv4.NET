using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using static RSDKv4.Drawing;

namespace RSDKv4;

public struct PaletteEntry
{
    public int paletteNum;
    public int startLine;
    public int endLine;

    public PaletteEntry(int palette, int startLine, int endLine)
    {
        this.paletteNum = palette;
        this.startLine = startLine;
        this.endLine = endLine;
    }
}

public class Palette
{
    public static PaletteEntry[] activePalettes
        = new PaletteEntry[8];
    public static int activePaletteCount = 0;

    public static int fadeMode;
    public static byte fadeA = 0;
    public static byte fadeR = 0;
    public static byte fadeG = 0;
    public static byte fadeB = 0;

    public static bool paletteDirty = true;

    public static void SetPaletteEntry(byte paletteIndex, byte index, byte r, byte g, byte b)
    {
        Console.WriteLine($"{paletteIndex},{index} (#{r:X2}{g:X2}{b:X2}, {(RGB_16BIT5551(r, g, b, index != 0 ? (byte)1 : (byte)0))})");

        if (paletteIndex != 0xFF)
        {
            fullPalette[paletteIndex][index] = RGB_16BIT5551(r, g, b, index != 0 ? (byte)1 : (byte)0);
            fullPalette32[paletteIndex][index] = new Color(r, g, b);
        }
        else
        {
            fullPalette[texPaletteNum][index] = RGB_16BIT5551(r, g, b, index != 0 ? (byte)1 : (byte)0);
            fullPalette32[texPaletteNum][index] = new Color(r, g, b);
        }

        paletteDirty = true;
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

        paletteDirty = true;
    }

    internal static void SetActivePalette(byte newActivePal, int startLine, int endLine)
    {
        if (newActivePal >= PALETTE_COUNT)
            return;

        //Console.WriteLine($"{newActivePal}: {startLine}-{endLine}");

        if (activePaletteCount < 8)
        {
            activePalettes[activePaletteCount] = new PaletteEntry(newActivePal, startLine, endLine);
            activePaletteCount++;
        }

        if (startLine == 0)
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

            fullPalette[destPaletteID][idx] = RGB_16BIT5551(
                (byte)((ushort)(srcB.R * blendAmount + blendA * srcA.R) >> 8),
                (byte)((ushort)(srcB.G * blendAmount + blendA * srcA.G) >> 8),
                (byte)((ushort)(srcB.B * blendAmount + blendA * srcA.B) >> 8),
                idx != 0 ? (byte)1 : (byte)0);
            fullPalette32[destPaletteID][idx].R = (byte)((ushort)(srcB.R * blendAmount + blendA * srcA.R) >> 8);
            fullPalette32[destPaletteID][idx].G = (byte)((ushort)(srcB.G * blendAmount + blendA * srcA.G) >> 8);
            fullPalette32[destPaletteID][idx].B = (byte)((ushort)(srcB.B * blendAmount + blendA * srcA.B) >> 8);

            idx++;
        }

        if (destPaletteID < PALETTE_COUNT)
            texPaletteNum = destPaletteID;

        paletteDirty = true;
    }

    internal static void SetPaletteEntryPacked(byte paletteIndex, byte index, uint colour)
    {
        var fullPalette = Drawing.fullPalette;
        var fullPalette32 = Drawing.fullPalette32;

        var col = RGB_16BIT5551((byte)(colour >> 16), (byte)(colour >> 8), (byte)(colour >> 0), index != 0 ? (byte)1 : (byte)0);
        if (fullPalette[paletteIndex][index] != col)
        {
            fullPalette[paletteIndex][index] = col;
            fullPalette32[paletteIndex][index].R = (byte)(colour >> 16);
            fullPalette32[paletteIndex][index].G = (byte)(colour >> 8);
            fullPalette32[paletteIndex][index].B = (byte)(colour >> 0);

            paletteDirty = true;
        }
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

        paletteDirty = true;
    }
}
