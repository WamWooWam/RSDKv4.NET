using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using NVorbis;

#if NO_THREADS
#endif

namespace RSDKv4;

public class StreamInfo
{
    public Stream stream;
    public VorbisReader reader;
    public DynamicSoundEffectInstance effect;
    public bool loop;
    public TimeSpan loopPoint;

#if !FNA // FNA does not require format conversion
    public byte[] buffer;
#endif
    public float[] floatBuffer;

    public bool isPlaying;

    public void Reset()
    {
        if (effect != null)
        {
            effect.Stop();
            effect.Dispose();
        }

        if (reader != null)
        {
            reader.Dispose();
        }

        effect = null;
        reader = null;
        stream = null;
        isPlaying = false;
    }
}
