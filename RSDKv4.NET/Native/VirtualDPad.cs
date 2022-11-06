using Microsoft.Xna.Framework.Input.Touch;
using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native
{
    public class VirtualDPad : NativeEntity
    {
        public int textureID;
        public float moveX;
        public float moveY;
        public float pivotX;
        public float pivotY;
        public float offsetX;
        public float offsetY;
        public float jumpX;
        public float jumpY;
        public float moveSize;
        public float jumpSize;
        public float pressedSize;
        public int alpha;
        public float pauseX;
        public float pauseX_S;
        public float pauseY;
        public int pauseAlpha;
        public float relativeX;
        public float relativeY;
        public sbyte moveFinger;
        public sbyte jumpFinger;
        public int useTouchControls;
        public int usePhysicalControls;
        public int vsMode;
        public byte editMode;

        public override void Create()
        {
            var saveGame = SaveData.saveGame;
            float screenXCenter = Drawing.SCREEN_CENTERX;
            float screenYCenter = Drawing.SCREEN_CENTERY;
            this.moveX = saveGame.vDPadX_Move - screenXCenter;
            this.moveY = -(saveGame.vDPadY_Move - screenYCenter);
            this.jumpX = saveGame.vDPadX_Jump + screenXCenter;
            this.pauseY = 104.0f;
            this.jumpY = -(saveGame.vDPadY_Jump - screenYCenter);
            this.pauseX = screenXCenter - 76.0f;
            this.pauseX_S = screenXCenter - 52.0f;
            this.moveFinger = -1;
            this.jumpFinger = -1;

            float dpadSize = saveGame.vDPadSize * (1 / 256.0f);
            this.moveSize = dpadSize;
            this.jumpSize = dpadSize;
            this.pressedSize = dpadSize * 0.85f;
            this.useTouchControls = Engine.GetGlobalVariableID("options.touchControls");
            this.usePhysicalControls = Engine.GetGlobalVariableID("options.physicalControls");
            this.vsMode = Engine.GetGlobalVariableID("options.vsMode");
            this.textureID = LoadTexture("Data/Game/Menu/VirtualDPad.png", TEXFMT.RGBA8888);
        }

        public override void Main()
        {
            //if (!TouchPanel.GetCapabilities().IsConnected)
            //    return;

            var saveGame = SaveData.saveGame;
            var inputDown = Input.inputDown;

            if (Engine.globalVariables[this.useTouchControls] != 0 && (Engine.globalVariables[this.usePhysicalControls] == 0 || this.editMode != 0))
            {
                if (this.alpha < saveGame.vDPadOpacity)
                {
                    this.alpha += 4;
                    if (this.pauseAlpha < 0xFF)
                    {
                        this.pauseAlpha = (this.alpha << 8) / saveGame.vDPadOpacity;
                    }
                }
            }
            else
            {
                if (this.alpha > 0)
                {
                    this.alpha -= 4;
                    this.pauseAlpha = (this.alpha << 8) / saveGame.vDPadOpacity;
                }
            }

            if (this.alpha > 0)
            {
                SetRenderBlendMode(RENDER_BLEND.ALPHA);
                RenderImage(this.moveX, this.moveY, 160.0f, this.moveSize, this.moveSize, 128.0f, 128.0f, 256.0f, 256.0f, 0.0f, 0.0f, this.alpha,
                            this.textureID);

                if (this.alpha != saveGame.vDPadOpacity)
                {
                    this.offsetX = 0.0f;
                    this.offsetY = 0.0f;
                }
                else if (inputDown.up != 0)
                {
                    RenderImage(this.moveX, this.moveY, 160.0f, this.moveSize, this.moveSize, 128.0f, 128.0f, 256.0f, 120.0f, 256.0f, 256.0f, this.alpha,
                                this.textureID);

                    this.offsetX = 0.0f;
                    this.offsetY = 20.0f;
                }
                else if (inputDown.down != 0)
                {
                    RenderImage(this.moveX, this.moveY, 160.0f, this.moveSize, this.moveSize, 128.0f, -8.0f, 256.0f, 120.0f, 256.0f, 392.0f, this.alpha,
                                this.textureID);

                    this.offsetX = 0.0f;
                    this.offsetY = -20.0f;
                }
                else if (inputDown.left != 0)
                {
                    RenderImage(this.moveX, this.moveY, 160.0f, this.moveSize, this.moveSize, 128.0f, 128.0f, 120.0f, 256.0f, 256.0f, 256.0f, this.alpha,
                                this.textureID);

                    this.offsetX = 20.0f;
                    this.offsetY = 0.0f;
                }
                else if (inputDown.right != 0)
                {
                    RenderImage(this.moveX, this.moveY, 160.0f, this.moveSize, this.moveSize, -8.0f, 128.0f, 120.0f, 256.0f, 392.0f, 256.0f, this.alpha,
                                this.textureID);

                    this.offsetX = -20.0f;
                    this.offsetY = 0.0f;
                }
                else
                {
                    this.offsetX = 0.0f;
                    this.offsetY = 0.0f;
                }

                this.pivotX += (this.offsetX - this.pivotX) * 0.25f;
                this.pivotY += (this.offsetY - this.pivotY) * 0.25f;
                RenderImage(this.moveX, this.moveY, 160.0f, this.moveSize, this.moveSize, this.pivotX + 84.0f, this.pivotY + 84.0f, 168.0f, 168.0f, 16.0f,
                            328.0f, this.alpha, this.textureID);
                RenderImage(this.jumpX, this.jumpY, 160.0f, this.pressedSize, this.pressedSize, 128.0f, 128.0f, 256.0f, 256.0f, 256.0f, 0.0f, this.alpha,
                            this.textureID);

                float size = 0.0f;
                if (this.alpha == saveGame.vDPadOpacity && (inputDown.C != 0 || inputDown.A != 0 || inputDown.B != 0))
                    size = this.pressedSize;
                else
                    size = this.jumpSize;
                RenderImage(this.jumpX, this.jumpY, 160.0f, size, size, 84.0f, 83.0f, 168.0f, 168.0f, 16.0f, 328.0f, this.alpha, this.textureID);

                if (Engine.engineState == ENGINE_STATE.MAINGAME)
                {
                    if (this.vsMode > 0 && Engine.globalVariables[this.vsMode] == 0)
                    {
                        if (Scene.activeStageList == STAGELIST.SPECIAL)
                            RenderImage(this.pauseX_S, this.pauseY, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 160.0f, 258.0f, this.pauseAlpha,
                                        this.textureID);
                        else
                            RenderImage(this.pauseX, this.pauseY, 160.0f, 0.25f, 0.25f, 32.0f, 32.0f, 64.0f, 64.0f, 160.0f, 258.0f, this.pauseAlpha,
                                        this.textureID);
                    }
                }
            }
        }
    }
}