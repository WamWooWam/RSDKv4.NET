using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using RSDKv4.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RSDKv4.External;

public class OggSong : IDisposable
{
    private VorbisReader reader;
    private DynamicSoundEffectInstance effect;

    private Thread thread;
    private bool threadRun = false;
    private bool needBuffer = false;

    //private WaitHandle threadRunHandle = new WaitHandle();
    //private EventWaitHandle needBufferHandle = new EventWaitHandle();
    private byte[] buffer;
    private float[] nvBuffer;

    public SoundState State
    {
        get { return effect.State; }
    }

    public int SampleRate
        => reader.SampleRate;

    public float Volume
    {
        get { return effect.Volume; }
        set { effect.Volume = MathHelper.Clamp(value, 0, 1); }
    }

    public bool IsLooped { get; set; }
    public TimeSpan LoopPoint { get; set; }
    public TimeSpan Position
    {
        get
        {
#if SILVERLIGHT
            return reader.DecodedTime;
#else
            return reader.TimePosition;
#endif
        }
    }

    public OggSong(Stream oggFile)
    {
        if (oggFile is RSDKFileStream rsdkStream && rsdkStream.useEncryption)
            Debug.WriteLine("RSDK file stream is encrypted!! This will be slow and shit!!");

        reader = new VorbisReader(oggFile, true);
#if NETCOREAPP3_1
        effect = new DynamicSoundEffectInstance(reader.SampleRate, (AudioChannels)reader.Channels, 3, 32);
#else
        effect = new DynamicSoundEffectInstance(reader.SampleRate, (AudioChannels)reader.Channels);
#endif
        buffer = new byte[effect.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(150))];
        nvBuffer = new float[buffer.Length / 2];

        // when a buffer is needed, set our handle so the helper thread will read in more data
        effect.BufferNeeded += (s, e) => needBuffer = true;
    }

    ~OggSong()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool isDisposing)
    {
        threadRun = false;
        effect.Dispose();
    }

    public void Play(TimeSpan timeSpan)
    {
        Stop();

#if SILVERLIGHT
        reader.DecodedTime = timeSpan;
#else
        if(reader.TimePosition != timeSpan)
            reader.TimePosition = timeSpan;
#endif

        lock (effect)
        {
            effect.Play();
        }

        StartThread();
    }

    public void Pause()
    {
        lock (effect)
        {
            effect.Pause();
        }
    }

    public void Resume()
    {
        lock (effect)
        {
            effect.Resume();
        }
    }

    public void Stop()
    {
        lock (effect)
        {
            if (!effect.IsDisposed)
            {
                effect.Stop();
            }
        }
#if SILVERLIGHT
        reader.DecodedTime = TimeSpan.Zero;
#else
        if (reader.TimePosition != TimeSpan.Zero)
            reader.TimePosition = TimeSpan.Zero;
#endif
        if (thread != null)
        {
            // set the handle to stop our thread
            threadRun = false;
            thread = null;
        }
    }

    private void StartThread()
    {
        if (thread == null)
        {
            threadRun = true;
            thread = new Thread(StreamThread);
            thread.IsBackground = true;
            thread.Start();
        }
    }

    private void StreamThread()
    {
        while (!effect.IsDisposed)
        {
            // sleep until we need a buffer
            while (!effect.IsDisposed && threadRun && !needBuffer)
            {
                Thread.Sleep(25);
            }

            // if the thread is waiting to exit, leave
            if (!threadRun)
            {
                break;
            }

            lock (effect)
            {
                // ensure the effect isn't disposed
                if (effect.IsDisposed) { break; }
            }

            // read the next chunk of data
            int samplesRead = reader.ReadSamples(nvBuffer, 0, nvBuffer.Length);

            // out of data and looping? reset the reader and read again
            if (samplesRead == 0 && IsLooped)
            {
#if SILVERLIGHT
                reader.DecodedTime = LoopPoint;
#else
                reader.TimePosition = LoopPoint;
#endif
                samplesRead = reader.ReadSamples(nvBuffer, 0, nvBuffer.Length);
            }

            if (samplesRead > 0)
            {
#if NETCOREAPP3_1
                // submit our buffers
                lock (effect)
                {
                    // ensure the effect isn't disposed
                    if (effect.IsDisposed) { break; }

                    effect.SubmitFloatBufferEXT(nvBuffer, 0, samplesRead);
                }
#else
                for (int i = 0; i < samplesRead; i++)
                {
                    //short sValue = (short)Math.Max(Math.Min(short.MaxValue * nvBuffer[i], short.MaxValue), short.MinValue);
                    short sValue = (short)(short.MaxValue * nvBuffer[i]);
                    buffer[i * 2] = (byte)(sValue & 0xff);
                    buffer[i * 2 + 1] = (byte)((sValue >> 8) & 0xff);
                }

                // submit our buffers
                lock (effect)
                {
                    // ensure the effect isn't disposed
                    if (effect.IsDisposed) { break; }

                    effect.SubmitBuffer(buffer, 0, samplesRead);
                    effect.SubmitBuffer(buffer, samplesRead, samplesRead);
                }
#endif
            }
            else
            {
                break;
            }

            // reset our handle
            needBuffer = false;
        }
    }
}
