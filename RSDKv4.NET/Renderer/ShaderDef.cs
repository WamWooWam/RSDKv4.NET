using Microsoft.Xna.Framework.Graphics;

namespace RSDKv4.Render;

public class ShaderDef
{
    public ShaderDef(Effect effect, SamplerState samplerState)
    {
        this.effect = effect;
        this.samplerState = samplerState;
    }

    public readonly Effect effect;
    public readonly SamplerState samplerState;
}
