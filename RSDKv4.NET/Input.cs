using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RSDKv4.Native;

namespace RSDKv4;

public partial class Input
{
    public static InputData inputDown = new InputData();
    public static InputData inputPress = new InputData();

    public static int touchWidth;
    public static int touchHeight;

    public static int touches = 0;
    public static int[] touchDown = new int[8];
    public static int[] touchX = new int[8];
    public static int[] touchY = new int[8];
    public static float[] touchXF = new float[8];
    public static float[] touchYF = new float[8];
    public static int[] touchId = new int[8];

    private static bool wasPressed;

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

        ClearTouchData();

        int pointerID = 0;
        var state = TouchPanel.GetState().ToList();
        var mouseState = Mouse.GetState();

        if (wasPressed)
            state.Add(new TouchLocation(0, mouseState.LeftButton == ButtonState.Released ?
                (wasPressed ? TouchLocationState.Released : TouchLocationState.Moved) : TouchLocationState.Pressed,
                new Vector2(mouseState.X, mouseState.Y), TouchLocationState.Invalid, new Vector2()));
        wasPressed = mouseState.LeftButton == ButtonState.Pressed;

        foreach (TouchLocation touchLocation in state)
        {
            switch (touchLocation.State)
            {
                case TouchLocationState.Pressed:
                    AddTouch(touchLocation.Position.X, touchLocation.Position.Y, pointerID);
                    break;
                case TouchLocationState.Moved:
                    AddTouch(touchLocation.Position.X, touchLocation.Position.Y, pointerID);
                    break;
            }
            ++pointerID;
        }
    }

    public static void AddTouch(float touchPosX, float touchPosY, int pointerID)
    {
        for (int index = 0; index < 8; ++index)
        {
            if (touchDown[index] == 0)
            {
                var normalizedX = touchPosX / touchWidth;
                var normalizedY = touchPosY / touchHeight;
                touchDown[index] = 1;
                touchX[index] = (int)(normalizedX * Drawing.SCREEN_XSIZE);
                touchY[index] = (int)(normalizedY * Drawing.SCREEN_YSIZE);
                touchXF[index] = (normalizedX * NativeRenderer.SCREEN_XSIZE_F) - NativeRenderer.SCREEN_CENTERX_F;
                touchYF[index] = -((normalizedY * NativeRenderer.SCREEN_YSIZE_F) - NativeRenderer.SCREEN_CENTERY_F);
                touchId[index] = pointerID;

                break;
            }
        }

        touches++;
    }

    public static int CheckTouchRect(float x, float y, float w, float h)
    {
        //NativeRenderer.RenderRect(x, y, 500, w, h, 255, 0, 0, 32);

        for (int f = 0; f < touches; ++f)
        {
            if (touchDown[f] != 0 && touchXF[f] > (x - w) && touchYF[f] > (y - h) && touchXF[f] <= (x + w) && touchYF[f] <= (y + h))
            {
                return f;
            }
        }
        return -1;
    }

    public static void ClearTouchData()
    {
        touches = 0;
        touchDown[0] = 0;
        touchDown[1] = 0;
        touchDown[2] = 0;
        touchDown[3] = 0;
        touchDown[4] = 0;
        touchDown[5] = 0;
        touchDown[6] = 0;
        touchDown[7] = 0;
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
