using System.Collections.Generic;

namespace RSDKv4;

public class DrawListEntry
{
    //public int[] entityRefs = new int[Objects.ENTITY_COUNT];
    //public int listSize = 0;

    public List<int> entityRefs = new List<int>(Objects.ENTITY_COUNT);
}
