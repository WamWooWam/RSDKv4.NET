namespace RSDKv4.Native;

public class RetroGameLoop : NativeEntity
{
    public override void Create()
    {
        Objects.CreateNativeObject(() => new VirtualDPad());
    }

    public override void Main()
    {
        switch (Engine.engineState)
        {
            case ENGINE_STATE.DEVMENU:
                break;

            case ENGINE_STATE.MAINGAME:
                //Scene.ProcessStage();
                Drawing.Draw();
                Drawing.Present();
                //NativeRenderer.RenderRetroBuffer(64, 160);
                break;
        }
    }
}
