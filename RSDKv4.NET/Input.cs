using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4;

public struct InputData
{
    public byte up;
    public byte down;
    public byte left;
    public byte right;
    public byte A;
    public byte B;
    public byte C;
    public byte X;
    public byte Y;
    public byte Z;
    public byte L;
    public byte R;
    public byte start;
    public byte select;
};

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

public partial class Input
{
    public static InputData inputDown = new InputData();
    public static InputData inputPress = new InputData();

    public static int[] touchDown = new int[8];
    public static int[] touchX = new int[8];
    public static int[] touchY = new int[8];
    public static int[] touchId = new int[8];

    public static InputButton[] buttons = new InputButton[15]
    {
        new InputButton(Keys.Up),
        new InputButton(Keys.Down),
        new InputButton(Keys.Left),
        new InputButton(Keys.Right),
        new InputButton(Keys.A),
        new InputButton(Keys.S),
        new InputButton(Keys.D),
        new InputButton(Keys.Q),
        new InputButton(Keys.W),
        new InputButton(Keys.E),
        new InputButton(Keys.P),
        new InputButton(Keys.O),
        new InputButton(Keys.Enter),
        new InputButton(Keys.RightShift),
        new InputButton(0),
    };

    public static void ProcessInput()
    {
        var keyboardState = Keyboard.GetState();
        var pressed = false;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (keyboardState.IsKeyDown(buttons[i].key))
            {
                buttons[i].setHeld();
                pressed = true;
            }
            else if (buttons[i].hold)
            {
                buttons[i].setReleased();
            }
        }

        if (pressed)
        {
            if (!buttons[buttons.Length - 1].hold)
                buttons[buttons.Length - 1].setHeld();
        }
        else
        {
            if (!buttons[buttons.Length - 1].hold)
                buttons[buttons.Length - 1].setReleased();
        }
    }

    public static void CheckKeyPress(ref InputData input)
    {
#if !RETRO_USE_ORIGINAL_CODE
        input.up = buttons[0].press ? (byte)1 : (byte)0;
        input.down = buttons[1].press ? (byte)1 : (byte)0;
        input.left = buttons[2].press ? (byte)1 : (byte)0;
        input.right = buttons[3].press ? (byte)1 : (byte)0;
        input.A = buttons[4].press ? (byte)1 : (byte)0;
        input.B = buttons[5].press ? (byte)1 : (byte)0;
        input.C = buttons[6].press ? (byte)1 : (byte)0;
        input.X = buttons[7].press ? (byte)1 : (byte)0;
        input.Y = buttons[8].press ? (byte)1 : (byte)0;
        input.Z = buttons[9].press ? (byte)1 : (byte)0;
        input.L = buttons[10].press ? (byte)1 : (byte)0;
        input.R = buttons[11].press ? (byte)1 : (byte)0;
        input.start = buttons[12].press ? (byte)1 : (byte)0;
        input.select = buttons[13].press ? (byte)1 : (byte)0;
#endif
    }

    public static void CheckKeyDown(ref InputData input)
    {
#if !RETRO_USE_ORIGINAL_CODE
        input.up = buttons[0].hold ? (byte)1 : (byte)0;
        input.down = buttons[1].hold ? (byte)1 : (byte)0;
        input.left = buttons[2].hold ? (byte)1 : (byte)0;
        input.right = buttons[3].hold ? (byte)1 : (byte)0;
        input.A = buttons[4].hold ? (byte)1 : (byte)0;
        input.B = buttons[5].hold ? (byte)1 : (byte)0;
        input.C = buttons[6].hold ? (byte)1 : (byte)0;
        input.X = buttons[7].hold ? (byte)1 : (byte)0;
        input.Y = buttons[8].hold ? (byte)1 : (byte)0;
        input.Z = buttons[9].hold ? (byte)1 : (byte)0;
        input.L = buttons[10].hold ? (byte)1 : (byte)0;
        input.R = buttons[11].hold ? (byte)1 : (byte)0;
        input.start = buttons[12].hold ? (byte)1 : (byte)0;
        input.select = buttons[13].hold ? (byte)1 : (byte)0;
#endif
    }
}
