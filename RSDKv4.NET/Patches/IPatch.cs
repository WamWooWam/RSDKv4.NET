using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4.Patches
{
    public interface IPatch
    {
        public void Install(Hooks hooks);
    }
}
