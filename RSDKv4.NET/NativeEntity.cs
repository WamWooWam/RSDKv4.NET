using RSDKv4.Native;

namespace RSDKv4;

public abstract class NativeEntity
{
    internal Engine Engine;
    protected Audio Audio => Engine.Audio;
    protected Collision Collision => Engine.Collision;
    protected DevMenu DevMenu => Engine.DevMenu;
    protected FileIO FileIO => Engine.FileIO;
    protected Font Font => Engine.Font;
    protected Input Input => Engine.Input;
    protected Objects Objects => Engine.Objects;
    protected Palette Palette => Engine.Palette;
    protected Drawing Drawing => Engine.Drawing;
    protected NativeRenderer Renderer => Engine.Renderer;
    protected SaveData SaveData => Engine.SaveData;
    protected Scene Scene => Engine.Scene;
    protected Scene3D Scene3D => Engine.Scene3D;
    protected Script Script => Engine.Script;
    protected Strings Strings => Engine.Strings;
    protected Text Text => Engine.Text;

    protected float SCREEN_CENTERX_F => Renderer.SCREEN_CENTERX_F;
    protected float SCREEN_XSIZE_F => Renderer.SCREEN_XSIZE_F;

    public int slotId;
    public int objectId;

    public abstract void Create();
    public abstract void Main();
}
