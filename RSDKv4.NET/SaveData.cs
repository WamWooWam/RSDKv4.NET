using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4
{
    internal class SaveData
    {
        public static byte[] saveRAM = new byte[1024];

        internal static int ReadSaveRAMData()
        {
            return 1;
        }

        internal static int WriteSaveRAMData()
        {
            return 1;
        }
    }
}
