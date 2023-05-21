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

            case ENGINE_STATE.MAINGAME:
                //Scene.ProcessStage();
                Drawing.Draw();
                NativeRenderer.RenderRetroBuffer(64, 160, true);
                break;

            case ENGINE_STATE.INITPAUSE:
                Audio.PauseSound();
                Objects.ClearNativeObjects();
                Objects.CreateNativeObject(() => new MenuBG());
                Objects.CreateNativeObject(() => new PauseMenu());
                break;

            case ENGINE_STATE.INITDEVMENU:
                Engine.LoadGameConfig("Data/Game/GameConfig.bin");
                DevMenu.InitDevMenu();
                //ResetCurrentStageFolder();
                break;
            case ENGINE_STATE.DEVMENU:
                DevMenu.ProcessStageSelect();
                Drawing.Draw();
                NativeRenderer.RenderRetroBuffer(64, 160, true);
                break;
        }
    }
}
