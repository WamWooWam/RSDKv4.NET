using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.Hashing;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.Render;

#if NET7_0
using System.IO.Hashing;
using System.Runtime.CompilerServices;
#endif

namespace RSDKv4.Patches;

public class PixelDiff
{
    public byte index { get; set; }
    public ushort oldValue { get; set; }
    public ushort newValue { get; set; }
}

public class PaletteDiff : IEquatable<PaletteDiff>
{
    public int paletteIndex { get; set; }
    public int frame { get; set; }

    public List<PixelDiff> diffs { get; set; }
        = new List<PixelDiff>();

    public bool Equals(PaletteDiff other)
    {
        if (other == null)
            return false;

        if (other.paletteIndex != paletteIndex || other.diffs.Count != diffs.Count)
            return false;

        for (int i = 0; i < diffs.Count; i++)
        {
            if ((diffs[i].index != other.diffs[i].index) || (diffs[i].newValue != other.diffs[i].newValue) || (diffs[i].oldValue != other.diffs[i].newValue))
                return false;
        }

        return true;
    }
}

internal class PaletteHack : IPatch
{
    private bool isProfiling = true;

    private int frame;
    private short currentStageId = -1;
    private ushort[][] previousPalettes = new ushort[Drawing.PALETTE_COUNT][];

    private List<PaletteDiff> diffs = new List<PaletteDiff>();
    private List<int> indices = new List<int>();
    private int index;

    private Dictionary<uint, PaletteEntry> palettes = new Dictionary<uint, PaletteEntry>();

    private Scene Scene;
    private Drawing Drawing;
    private Engine Engine;
    private Palette Palette;

#if NET7_0
    private Crc32 Crc32 = new Crc32();
#endif

    public PaletteHack()
    {
        for (int i = 0; i < Drawing.PALETTE_COUNT; i++)
            previousPalettes[i] = new ushort[Drawing.PALETTE_SIZE];
    }

    public void Install(Engine engine)
    {
        Scene = engine.Scene;
        Drawing = engine.Drawing;
        Palette = engine.Palette;
        Engine = engine;

        engine.hooks.StageDidLoad += OnStageLoaded;
        engine.hooks.WillDraw += OnWillDraw;
    }

    private void OnStageLoaded(object sender, EventArgs e)
    {
#if NET8_0
        if (diffs.Count > 0)
        {
            File.WriteAllText($"{currentStageId}.json", System.Text.Json.JsonSerializer.Serialize(diffs));
            File.WriteAllText($"{currentStageId}.ind.json", System.Text.Json.JsonSerializer.Serialize(indices));
        }
#endif

        var activeStageList = Scene.activeStageList;
        var stageListPosition = Scene.stageListPosition;

        index = -1;
        frame = 0;
        currentStageId = (short)((((byte)activeStageList) << 8) | (byte)stageListPosition);
        diffs.Clear();
        indices.Clear();

        var stageList = Engine.stageList;
        Debug.WriteLine("Current scene: {0} {1} ({2:x2})", stageList[activeStageList][stageListPosition].folder, stageList[activeStageList][stageListPosition].name, currentStageId);

        for (int i = 0; i < Drawing.PALETTE_COUNT; i++)
        {
            for (int j = 0; j < Drawing.PALETTE_SIZE; j++)
            {
                previousPalettes[i][j] = Drawing.fullPalette[i][j];
            }
        }
    }

    int x = 0;
    private void OnWillDraw(object sender, EventArgs e)
    {
        if (!Palette.paletteDirty)
            return;
        var activeStageList = Scene.activeStageList;
        var stageListPosition = Scene.stageListPosition;
        var stageList = Engine.stageList;

        var folder = stageList[activeStageList][stageListPosition].folder;
        var name = stageList[activeStageList][stageListPosition].name;

#if NET8_0
        for (int i = 0; i < Palette.activePaletteCount; i++)
        {
            var palette = Palette.activePalettes[i].paletteNum;
            var hash = Crc32.Hash(MemoryMarshal.Cast<ushort, byte>(Drawing.fullPalette[palette]));
            var hashInt = BitConverter.ToUInt32(hash, 0);
            if (palettes.ContainsKey(hashInt))
                continue;

            var palTexture = new Texture2D(((HardwareDrawing)Drawing).GraphicsDevice, 16, 16, false, SurfaceFormat.Color);
            palTexture.SetData(Drawing.fullPalette32[palette]);

            using (var file = File.Create($"{x} - {folder} - {name} - {i} - {hashInt:x2}.png"))
            {
                palTexture.SaveAsPng(file, 16, 16);
            }

            x++;
        }
#endif
    }
}
