using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4.Native;

internal class RetroGameLoop : NativeEntity
{
    public override void Create()
    {

    }

    public override void Main()
    {
        switch (Engine.gameMode)
        {
            case ENGINE.DEVMENU:
                break;

            case ENGINE.MAINGAME:
                Scene.ProcessStage();
                Drawing.Draw();
                //NativeRenderer.RenderRetroBuffer(64, 160);
                break;
        }
    }
}
