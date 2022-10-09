using RSDKv4.Utility;

namespace RSDKv4;

public class SaveGame
{
    private ArraySlice<int> sramSegment;

    public SaveFile[] files = new SaveFile[4];
    public bool saveInitialized { get => sramSegment[0] != 0; set => sramSegment[0] = value ? 1 : 0; }
    public int musVolume { get => sramSegment[1]; set => sramSegment[1] = value; }
    public int sfxVolume { get => sramSegment[2]; set => sramSegment[2] = value; }
    public bool spindashEnabled { get => sramSegment[3] != 0; set => sramSegment[3] = value ? 1 : 0; }
    public int boxRegion { get => sramSegment[4]; set => sramSegment[4] = value; }
    public int vDPadSize { get => sramSegment[5]; set => sramSegment[5] = value; }
    public int vDPadOpacity { get => sramSegment[6]; set => sramSegment[6] = value; }
    public int vDPadX_Move { get => sramSegment[7]; set => sramSegment[7] = value; }
    public int vDPadY_Move { get => sramSegment[8]; set => sramSegment[8] = value; }
    public int vDPadX_Jump { get => sramSegment[9]; set => sramSegment[9] = value; }
    public int vDPadY_Jump { get => sramSegment[10]; set => sramSegment[10] = value; }
    public bool tailsUnlocked { get => sramSegment[11] != 0; set => sramSegment[11] = value ? 1 : 0; }
    public bool knuxUnlocked { get => sramSegment[12] != 0; set => sramSegment[12] = value ? 1 : 0; }
    public int unlockedActs { get => sramSegment[13]; set => sramSegment[13] = value; }
    public bool unlockedHPZ { get => sramSegment[14] != 0; set => sramSegment[14] = value ? 1 : 0; }

    public ArraySlice<int> records;

    public SaveGame(int[] sramRef)
    {
        for (int i = 0; i < 4; i++)
        {
            files[i] = new SaveFile(sramRef, i);
        }

        sramSegment = new ArraySlice<int>(sramRef, 32, 15);
        records = new ArraySlice<int>(sramRef, 64, 0x80);
    }
}
