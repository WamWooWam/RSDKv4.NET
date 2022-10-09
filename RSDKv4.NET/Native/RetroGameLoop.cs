namespace RSDKv4.Native;

public class RetroGameLoop : NativeEntity
{
    public override void Create()
    {
        Objects.CreateNativeObject(() => new VirtualDPad());
    }

    public override void Main()
    {
        switch (Engine.gameMode)
        {
            case ENGINE.DEVMENU:
                break;

            case ENGINE.MAINGAME:
                //Scene.ProcessStage();
                Drawing.Draw();
                NativeRenderer.RenderRetroBuffer(64, 160);
                break;
        }
    }
}
