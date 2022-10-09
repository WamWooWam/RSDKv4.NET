using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RSDKv4.Patches;

public class PixelDiff
{
    public byte index { get; set; }
    public ushort value { get; set; }
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
            if ((diffs[i].index != other.diffs[i].index) || (diffs[i].value != other.diffs[i].value))
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
    private List<int> indicies = new List<int>();
    private int index;

    public PaletteHack()
    {
        for (int i = 0; i < Drawing.PALETTE_COUNT; i++)
            previousPalettes[i] = new ushort[Drawing.PALETTE_SIZE];
    }

    public void Install(Hooks hooks)
    {
        hooks.StageDidLoad += OnStageLoaded;
        hooks.StageDidStep += OnStageStepped;
    }

    private void OnStageLoaded(object sender, EventArgs e)
    {
#if NET6_0
        if (diffs.Count > 0)
        {
            File.WriteAllText($"{currentStageId}.json", System.Text.Json.JsonSerializer.Serialize(diffs));
            File.WriteAllText($"{currentStageId}.ind.json", System.Text.Json.JsonSerializer.Serialize(indicies));
        }
#endif

        var activeStageList = Scene.activeStageList;
        var stageListPosition = Scene.stageListPosition;

        index = -1;
        frame = 0;
        currentStageId = (short)((((byte)activeStageList) << 8) | (byte)stageListPosition);
        diffs.Clear();
        indicies.Clear();

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

    private void OnStageStepped(object sender, EventArgs e)
    {
        PaletteDiff diff = null;
        for (int i = 0; i < Drawing.PALETTE_COUNT; i++)
        {
            for (int j = 0; j < Drawing.PALETTE_SIZE; j++)
            {
                if (previousPalettes[i][j] != Drawing.fullPalette[i][j])
                {
                    if (diff == null)
                        diff = new PaletteDiff() { frame = frame, paletteIndex = i };

                    diff.diffs.Add(new PixelDiff { index = (byte)j, value = Drawing.fullPalette[i][j] });
                    previousPalettes[i][j] = Drawing.fullPalette[i][j];
                }
            }
        }

        if (diff != null)
        {
            if (!diffs.Any(d => d.Equals(diff)))
                diffs.Add(diff);

#if !SILVERLIGHT
            index = diffs.FindIndex(d => d.Equals(diff));
#endif
        }

        indicies.Add(index);
        frame++;
    }
}
