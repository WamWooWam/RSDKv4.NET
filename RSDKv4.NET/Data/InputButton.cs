using Microsoft.Xna.Framework.Input;

namespace RSDKv4;

public struct InputButton
{
    public Keys key;

    public bool press;
    public bool hold;

    public InputButton(Keys key)
    {
        this.key = key;
        this.press = false;
        this.hold = false;
    }

    public void setHeld()
    {
        press = !hold;
        hold = true;
    }

    public void setReleased()
    {
        press = false;
        hold = false;
    }

    public bool down() { return press || hold; }
}
