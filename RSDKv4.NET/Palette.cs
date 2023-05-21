using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace RSDKv4;

public class Palette
{
    public PaletteEntry[] activePalettes = new PaletteEntry[8];
    public int activePaletteCount = 0;

    public int fadeMode;
    public byte fadeA = 0;
    public byte fadeR = 0;
    public byte fadeG = 0;
    public byte fadeB = 0;

    public bool paletteDirty = true;

    private Drawing Drawing;
    private FileIO FileIO;

    public void Initialize(Engine engine)
    {
        FileIO = engine.FileIO;
        Drawing = engine.Drawing;
    }

    public void SetFade(byte R, byte G, byte B, ushort A)
    {
        fadeMode = 1;
        fadeR = R;
        fadeG = G;
        fadeB = B;
        fadeA = (byte)(A > 0xff ? 0xff : A);
    }

    public void SetPaletteEntry(byte paletteIndex, byte index, byte r, byte g, byte b)
    {
        // Debug.WriteLine($"{paletteIndex},{index} (#{r:X2}{g:X2}{b:X2}, {(RGB_16BIT5551(r, g, b, index != 0 ? (byte)1 : (byte)0))})");

        if (paletteIndex != 0xFF)
        {
            Drawing.fullPalette[paletteIndex][index] = Drawing.RGB_16BIT5551(r, g, b, index != 0 ? (byte)1 : (byte)0);
            Drawing.fullPalette32[paletteIndex][index] = new Color(r, g, b);
        }
        else
        {
            Drawing.fullPalette[Drawing.texPaletteNum][index] = Drawing.RGB_16BIT5551(r, g, b, index != 0 ? (byte)1 : (byte)0);
            Drawing.fullPalette32[Drawing.texPaletteNum][index] = new Color(r, g, b);
        }

        paletteDirty = true;
    }

    public void LoadPalette(string filePath, int paletteID, int startPaletteIndex, int startIndex, int endIndex)
    {
        string fullPath = "Data/Palettes/" + filePath;

        if (FileIO.LoadFile(fullPath, out var info))
        {
            FileIO.SetFilePosition(3 * startIndex);
            if (paletteID >= Drawing.PALETTE_COUNT || paletteID < 0)
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


    public void RotatePalette(int palID, byte startIndex, byte endIndex, bool right)
    {
        if (right)
        {
            var startClr = Drawing.fullPalette[palID][endIndex];
            var startClr32 = Drawing.fullPalette32[palID][endIndex];
            for (int i = endIndex; i > startIndex; --i)
            {
                Drawing.fullPalette[palID][i] = Drawing.fullPalette[palID][i - 1];
                Drawing.fullPalette32[palID][i] = Drawing.fullPalette32[palID][i - 1];
            }
            Drawing.fullPalette[palID][startIndex] = startClr;
            Drawing.fullPalette32[palID][startIndex] = startClr32;
        }
        else
        {
            var startClr = Drawing.fullPalette[palID][startIndex];
            var startClr32 = Drawing.fullPalette32[palID][startIndex];
            for (int i = startIndex; i < endIndex; ++i)
            {
                Drawing.fullPalette[palID][i] = Drawing.fullPalette[palID][i + 1];
                Drawing.fullPalette32[palID][i] = Drawing.fullPalette32[palID][i + 1];
            }
            Drawing.fullPalette[palID][endIndex] = startClr;
            Drawing.fullPalette32[palID][endIndex] = startClr32;
        }

        paletteDirty = true;
    }

    public void SetActivePalette(byte newActivePal, int startLine, int endLine)
    {
        if (newActivePal >= Drawing.PALETTE_COUNT)
            return;

        startLine = Math.Max(0, Math.Min(startLine, Drawing.SCREEN_YSIZE));
        endLine = Math.Max(0, Math.Min(endLine, Drawing.SCREEN_YSIZE));

        //Console.WriteLine($"{newActivePal}: {startLine}-{endLine}");

        if (activePaletteCount < 8)
        {
            activePalettes[activePaletteCount] = new PaletteEntry(newActivePal, startLine, endLine);
            activePaletteCount++;
        }

        if (startLine == 0)
            Drawing.texPaletteNum = newActivePal;
    }

    public void SetPaletteFade(byte destPaletteID, byte srcPaletteA, byte srcPaletteB, ushort blendAmount, int startIndex, int endIndex)
    {
        if (destPaletteID >= Drawing.PALETTE_COUNT || srcPaletteA >= Drawing.PALETTE_COUNT || srcPaletteB >= Drawing.PALETTE_COUNT)
            return;

        if (blendAmount >= 0x100)
            blendAmount = 0xFF;

        if (startIndex >= endIndex)
            return;

        var blendA = (uint)(0xFF - blendAmount);
        var idx = startIndex;
        for (int l = startIndex; l < endIndex; ++l)
        {
            var srcB = Drawing.fullPalette32[srcPaletteB][l];
            var srcA = Drawing.fullPalette32[srcPaletteA][l];

            Drawing.fullPalette[destPaletteID][idx] = Drawing.RGB_16BIT5551(
                (byte)((ushort)(srcB.R * blendAmount + blendA * srcA.R) >> 8),
                (byte)((ushort)(srcB.G * blendAmount + blendA * srcA.G) >> 8),
                (byte)((ushort)(srcB.B * blendAmount + blendA * srcA.B) >> 8),
                idx != 0 ? (byte)1 : (byte)0);
            Drawing.fullPalette32[destPaletteID][idx].R = (byte)((ushort)(srcB.R * blendAmount + blendA * srcA.R) >> 8);
            Drawing.fullPalette32[destPaletteID][idx].G = (byte)((ushort)(srcB.G * blendAmount + blendA * srcA.G) >> 8);
            Drawing.fullPalette32[destPaletteID][idx].B = (byte)((ushort)(srcB.B * blendAmount + blendA * srcA.B) >> 8);

            idx++;
        }

        if (destPaletteID < Drawing.PALETTE_COUNT)
            Drawing.texPaletteNum = destPaletteID;

        paletteDirty = true;
    }

    public void SetPaletteEntryPacked(byte paletteIndex, byte index, uint colour)
    {
        var fullPalette = Drawing.fullPalette;
        var fullPalette32 = Drawing.fullPalette32;

        var col = Drawing.RGB_16BIT5551((byte)(colour >> 16), (byte)(colour >> 8), (byte)(colour >> 0), index != 0 ? (byte)1 : (byte)0);
        if (fullPalette[paletteIndex][index] != col)
        {
            fullPalette[paletteIndex][index] = col;
            fullPalette32[paletteIndex][index].R = (byte)(colour >> 16);
            fullPalette32[paletteIndex][index].G = (byte)(colour >> 8);
            fullPalette32[paletteIndex][index].B = (byte)(colour >> 0);

            paletteDirty = true;
        }
    }

    public int GetPaletteEntryPacked(byte paletteIndex, byte index)
    {
        var clr = Drawing.fullPalette32[paletteIndex][index];
        return (clr.R << 16) | (clr.G << 8) | (clr.B);
    }

    public void CopyPalette(byte sourcePalette, byte srcPaletteStart, byte destinationPalette, byte destPaletteStart, ushort count)
    {
        var fullPalette = Drawing.fullPalette;
        var fullPalette32 = Drawing.fullPalette32;

        if (sourcePalette < Drawing.PALETTE_COUNT && destinationPalette < Drawing.PALETTE_COUNT)
        {
            for (int i = 0; i < count; ++i)
            {
                fullPalette[destinationPalette][destPaletteStart + i] = fullPalette[sourcePalette][srcPaletteStart + i];
                fullPalette32[destinationPalette][destPaletteStart + i] = fullPalette32[sourcePalette][srcPaletteStart + i];
            }
        }

        paletteDirty = true;
    }

    internal void SetLimitedFade(byte v1, byte v2, byte v3, ushort v4, int v5, int v6, int v7)
    {
        throw new NotImplementedException();
    }
}
