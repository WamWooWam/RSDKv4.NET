using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4;

public abstract class NativeEntity
{
    public int slotId;
    public int objectId;

    public abstract void Create();
    public abstract void Main();
}
