using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RSDKv4.Patches
{
    public class InputPlayer : IPatch
    {
        private int frame;
        private short stageId;
        private List<int> data;

        public void Install(Hooks hooks)
        {
            hooks.StageDidLoad += OnStageLoaded;
            hooks.StageWillStep += OnStageStepped;
        }

        private void OnStageLoaded(object sender, EventArgs e)
        {
            FastMath.SetRandomSeed(0);

            frame = 0;
            data = new List<int>();
            stageId = (short)((((byte)Scene.activeStageList) << 8) | (byte)Scene.stageListPosition);
            try
            {
                using (var stream = File.OpenRead($"{stageId}.replay.bin"))
                using (var read = new BinaryReader(stream))
                {
                    var size = read.ReadInt32();
                    for (int i = 0; i < size; i++)
                    {
                        data.Add(read.ReadInt32());
                    }
                }
            }
            catch { }
        }

        private void OnStageStepped(object sender, EventArgs e)
        {
            if (data.Count > 0 && data.Count > frame)
            {
                var array = new BitArray(new int[1] { data[frame] });
                for (int i = 0; i < 15; i++)
                {
                    Input.buttons[i].press = array.Get(i * 2);
                    Input.buttons[i].hold = array.Get((i * 2) + 1);
                }
            }

            frame++;
        }
    }
}
