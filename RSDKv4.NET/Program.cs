#if !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4;

public partial class Program
{
    public static void Main(string[] args)
    {
#if FNA
        //DllMap.Initialise();
#endif
        using (var game = new RSDKv4Game())
            game.Run();
    }
}
#endif