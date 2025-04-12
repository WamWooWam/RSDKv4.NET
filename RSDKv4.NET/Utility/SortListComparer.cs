using System.Collections.Generic;

namespace RSDKv4.Utility;

public class SortListComparer : IComparer<SortList>
{
    public int Compare(SortList x, SortList y)
    {
        return y.z - x.z != 0 ? y.z - x.z : y.index - x.index;
    }
}
