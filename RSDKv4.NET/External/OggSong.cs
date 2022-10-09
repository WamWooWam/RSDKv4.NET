using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using RSDKv4.Utility;

#if NETSTANDARD1_6 || WINDOWSPHONEAPP
using System.Threading.Tasks;
#endif

namespace RSDKv4.External;

public class OggSong : IDisposable
{
    private VorbisReader reader;
    private DynamicSoundEffectInstance effect;

#if NETSTANDARD1_6 || WINDOWSPHONEAPP
    private Task thread;
#else
    private Thread thread;
#endif
    private bool threadRun = false;
    private bool threadRunning = false;
    private bool needBuffer = false;
    private bool hasPlayed = false;

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
        effect = new DynamicSoundEffectInstance(reader.SampleRate, (AudioChannels)reader.Channels);
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
#else
        if (reader.TimePosition != timeSpan)
            reader.TimePosition = timeSpan;
#endif

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
#else
        try
        {
            if (reader.TimePosition != TimeSpan.Zero)
                reader.TimePosition = TimeSpan.Zero;
        }
        catch { }
#endif
        if (thread != null)
        {
            // set the handle to stop our thread
            threadRun = false;
            thread = null;

            while (threadRunning) { };
        }
    }

    private void StartThread()
    {
        if (thread == null)
        {
            threadRun = true;
            needBuffer = true;
            hasPlayed = false;
#if NETSTANDARD1_6 || WINDOWSPHONEAPP
            thread = Task.Run(StreamThread);
#else
            thread = new Thread(StreamThread);
            thread.IsBackground = true;
            thread.Start();
#endif
        }
    }

    private void StreamThread()
    {
        this.threadRunning = true;

#if NETCOREAPP1_0_OR_GREATER  || NETSTANDARD1_0_OR_GREATER
        var wait = new SpinWait();
#endif
        try
        {
            while (!effect.IsDisposed)
            {
                // sleep until we need a buffer
                while (!effect.IsDisposed && threadRun && !needBuffer)
                {
#if NETCOREAPP1_0_OR_GREATER || NETSTANDARD1_0_OR_GREATER
                    wait.SpinOnce();
#else
                    Thread.Sleep(25);
#endif
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
                    if (reader.TimePosition != LoopPoint)
                        reader.TimePosition = LoopPoint;
#endif
                    samplesRead = reader.ReadSamples(nvBuffer, 0, nvBuffer.Length);
                }

                if (samplesRead > 0)
                {
#if NETCOREAPP
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

                        effect.SubmitBuffer(buffer, 0, samplesRead * 2);
                        //effect.SubmitBuffer(buffer, samplesRead, samplesRead);
                    }
#endif
                }
                else
                {
                    break;
                }

                if (!hasPlayed)
                {
                    hasPlayed = true;
                    lock (effect)
                    {
                        effect.Play();
                    }
                }

                // reset our handle
                needBuffer = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        this.threadRunning = false;
    }
}
