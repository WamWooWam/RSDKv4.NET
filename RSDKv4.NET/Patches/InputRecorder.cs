#if !NETSTANDARD1_6 && !WINDOWSPHONEAPP

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace RSDKv4.Patches;

public class InputRecorder : IPatch
{
    private int frame;
    private short stageId;
    private List<int> data;

    public void Install(Hooks hooks)
    {
        hooks.StageDidLoad += OnStageLoaded;
        hooks.StageDidStep += OnStageStepped;
    }

    private void OnStageLoaded(object sender, EventArgs e)
    {
        FastMath.SetRandomSeed(0);

        if (frame > 0)
        {
            using (var stream = File.Create($"{stageId}.replay.bin"))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(frame);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
        }


        frame = 0;
        data = new List<int>();
        stageId = (short)((((byte)Scene.activeStageList) << 8) | (byte)Scene.stageListPosition);
    }

    private void OnStageStepped(object sender, EventArgs e)
    {
        var array = new BitArray(32);
        for (int i = 0; i < 15; i++)
        {
            array.Set(i * 2, Input.buttons[i].press);
            array.Set((i * 2) + 1, Input.buttons[i].hold);
        }

        int[] x = new int[1];
        array.CopyTo(x, 0);

        data.Add(x[0]);
        frame++;
    }
}

#endif