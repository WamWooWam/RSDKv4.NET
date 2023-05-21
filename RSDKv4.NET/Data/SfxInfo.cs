using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;

namespace RSDKv4;

[DebuggerDisplay("{name} {soundEffect.Duration}")]
public struct SfxInfo
{
    public string name;
    public SoundEffect soundEffect;
}
