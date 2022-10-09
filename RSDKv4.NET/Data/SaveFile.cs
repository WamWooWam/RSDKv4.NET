using RSDKv4.Utility;

namespace RSDKv4;

public class SaveFile
{
    private ArraySlice<int> sramSegment;

    public SaveFile(int[] sramRef, int idx)
    {
        sramSegment = new ArraySlice<int>(sramRef, idx * 8, 8);
    }

    public int characterId { get => sramSegment[0]; set => sramSegment[0] = value; }
    public int lives { get => sramSegment[1]; set => sramSegment[1] = value; }
    public int score { get => sramSegment[2]; set => sramSegment[2] = value; }
    public int scoreBonus { get => sramSegment[3]; set => sramSegment[3] = value; }
    public int stageId { get => sramSegment[4]; set => sramSegment[4] = value; }
    public int emeralds { get => sramSegment[5]; set => sramSegment[5] = value; }
    public int specialStageId { get => sramSegment[6]; set => sramSegment[6] = value; }
}
