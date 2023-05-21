using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static RSDKv4.Drawing;
using static RSDKv4.Native.NativeRenderer;

namespace RSDKv4.Native;

public class FadeScreen : NativeEntity
{
    public enum STATE { MENUFADEIN, FADEOUT, GAMEFADEOUT, FADEIN };

    public STATE state;
    public float timer;
    public float fadeSpeed;
    public float delay;
    public byte fadeR;
    public byte fadeG;
    public byte fadeB;
    public int fadeA;
    public Matrix render, temp;

    public override void Create()
    {
        this.timer = 0.0f;
        this.delay = 1.5f;
        this.fadeSpeed = 2.0f;
        this.state = STATE.GAMEFADEOUT;
        Engine.nativeMenuFadeIn = true;
    }

    public override void Main()
    {
        Renderer.SetRenderBlendMode(RENDER_BLEND.ALPHA);
        this.timer += this.fadeSpeed * Engine.deltaTime;
        switch (this.state)
        {
            case STATE.MENUFADEIN:
                this.fadeA = (int)((this.delay - this.timer) * 256.0f);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, this.fadeR, this.fadeG, this.fadeB, this.fadeA);
                if (this.timer > this.delay)
                {
                    Objects.RemoveNativeObject(this);
                    Engine.nativeMenuFadeIn = false;
                    Audio.SetMusicTrack("MainMenu.ogg", 0, true, 106596);
                    Audio.PlayMusic(0, 0);
                }
                break;

            case STATE.FADEOUT:
                this.fadeA = (int)(this.timer * 256.0);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, this.fadeR, this.fadeG, this.fadeB,
                           this.fadeA);
                if (this.timer > this.delay)
                    Objects.RemoveNativeObject(this);
                break;

            case STATE.GAMEFADEOUT:
                this.fadeA = (int)(this.timer * 256.0);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, this.fadeR, this.fadeG, this.fadeB,
                           this.fadeA);
                Audio.SetMusicVolume(Audio.masterVolume - 2);

                if (this.timer > this.delay)
                {
                    Objects.ClearNativeObjects();
                    Objects.CreateNativeObject(() => new RetroGameLoop());
                    if (Engine.deviceType == DEVICE.MOBILE)
                        Objects.CreateNativeObject(() => new VirtualDPad());
                }
                break;

            case STATE.FADEIN:
                this.fadeA = (int)((this.delay - this.timer) * 256.0f);
                Renderer.RenderRect(-SCREEN_CENTERX_F, SCREEN_CENTERY_F, 160.0f, SCREEN_XSIZE_F, SCREEN_YSIZE_F, this.fadeR, this.fadeG, this.fadeB,
                           this.fadeA);
                if (this.timer > this.delay)
                {
                    Objects.RemoveNativeObject(this);
                    Engine.nativeMenuFadeIn = false;
                }
                break;
        }

        //NewRenderState();
        //MatrixScaleXYZF(&this.render, Engine.windowScale, Engine.windowScale, 1.0);
        //MatrixTranslateXYZF(&this.temp, 0.0, 0.0, 160.0);
        //MatrixMultiplyF(&this.render, &this.temp);
        //SetRenderMatrix(&this.render);
    }
}
