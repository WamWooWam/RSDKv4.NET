using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using RSDKv4.Utility;

#if NO_THREADS
using System.Threading.Tasks;
#endif

namespace RSDKv4;

public class Audio
{
    public const int TRACK_COUNT = 0x10;
    public static TrackInfo[] musicTracks = new TrackInfo[TRACK_COUNT];
    public static int currentTrack;

    public const int SFX_COUNT = 0x100;
    public static SfxInfo[] soundEffects = new SfxInfo[SFX_COUNT];
    public static byte stageSfxCount;

    public const int SFX_CHANNELS = 0x10;
    public static SfxChannel[] soundChannels = new SfxChannel[SFX_CHANNELS];
    public static int currentChannel = 0;

    public static int sfxVolume = 50;
    public static int bgmVolume = 100;
    public static int masterVolume = 100;
    public static int trackId = -1;
    public static int musicPosition = 0;
    public static int musicRatio = 0;
    public static int musicStartPos = 0;

    public static int musicStatus;

    public static StreamInfo[] streams = new StreamInfo[2];
    public static int currentStream = 0;

#if NO_THREADS
    private static Task musicThread;
#else
    private static Thread musicThread;
#endif

    static Audio()
    {
        Helpers.Memset(musicTracks, () => new TrackInfo());
        Helpers.Memset(soundEffects, () => new SfxInfo());
        Helpers.Memset(soundChannels, () => new SfxChannel());
        Helpers.Memset(streams, () => new StreamInfo());
    }

    public static bool InitAudioPlayback()
    {
        for (int i = 0; i < Engine.globalSfxCount; i++)
        {
            RSDKv4Game.loadPercent = 0.1f + (0.75f * ((i + 1) / (float)Engine.globalSfxCount));
            LoadSfx(Engine.globalSfxPaths[i], (byte)i);
        }

#if NO_THREADS
        musicThread = Task.Run(MusicThreadLoop);
#else
        musicThread = new Thread(MusicThreadLoop);
        musicThread.IsBackground = true;
        musicThread.Start();
#endif

        return true; // for now
    }

    public static void LoadSfx(string filePath, byte sfxId)
    {
        Debug.WriteLine("Load SFX ({0}) from {1}", sfxId, filePath);

        var fullPath = "Data/SoundFX/" + filePath;
        if (FileIO.LoadFile(fullPath, out var file))
        {
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".wav")
            {
                var stream = FileIO.CreateFileStream();
                soundEffects[sfxId].name = filePath;
                soundEffects[sfxId].soundEffect = SoundEffect.FromStream(stream);
            }
            else if (ext == ".ogg") // pain and suffering
            {
                soundEffects[sfxId].name = filePath;

                if (OggCache.TryGetCachedSfx(fullPath, out var stream))
                {
                    soundEffects[sfxId].soundEffect = SoundEffect.FromStream(stream);
                    return;
                }

                stream = FileIO.CreateFileStream();
                var vorbisReader = new VorbisReader(stream, true);
                var data = new float[vorbisReader.TotalSamples];
                vorbisReader.ReadSamples(data, 0, data.Length);

                var pcmData = new byte[data.Length * 2];
                for (int i = 0; i < data.Length; i++)
                {
                    short sValue = (short)Math.Max(Math.Min(short.MaxValue * data[i], short.MaxValue), short.MinValue);
                    pcmData[i * 2] = (byte)(sValue & 0xff);
                    pcmData[i * 2 + 1] = (byte)((sValue >> 8) & 0xff);
                }

                soundEffects[sfxId].soundEffect = new SoundEffect(pcmData, vorbisReader.SampleRate, (AudioChannels)vorbisReader.Channels);

                OggCache.CacheSfx(fullPath, data, pcmData, vorbisReader.SampleRate, vorbisReader.Channels);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    public static void StopAllSfx()
    {

    }

    public static void ReleaseStageSfx()
    {

    }

    public static void SetMusicTrack(string filePath, byte trackId, bool loop, uint loopPoint)
    {
        var nextTrack = "Data/Music/" + filePath;
        musicTracks[trackId] = new TrackInfo() { name = nextTrack, loop = loop, loopPoint = loopPoint };
    }

    public static void PlayMusic(int track, int musStartPos)
    {
        if (track < 0 || track >= TRACK_COUNT)
        {
            StopMusic(true);
            currentTrack = -1;
            return;
        }

        if (!string.IsNullOrEmpty(musicTracks[track].name))
        {
            if (musicStatus != MUSIC.LOADING)
            {
                musicStartPos = musStartPos;
                currentTrack = track;
                musicStatus = MUSIC.LOADING;
                //LoadAudio(); // todo: thread this
            }
            else
            {
                Debug.WriteLine("Trying to play music while loading??");
            }
        }
        else
        {
            StopMusic(true);
        }
    }

    public static void SwapMusicTrack(string filePath, byte trackId, uint loopPoint, int ratio)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            StopMusic(true);
        }
        else
        {
            musicTracks[trackId].name = "Data/Music/" + filePath;
            musicTracks[trackId].loop = true;
            musicTracks[trackId].loopPoint = loopPoint;
            musicRatio = ratio;

            PlayMusic(trackId, 1);
        }
    }

    private static void LoadAudio()
    {
        int oldStream = currentStream;
        int newStream = (currentStream + 1) % 2;

        if (streams[newStream].reader != null)
        {
            streams[newStream].Reset();
        }

        try
        {
            if (FileIO.TryGetFileStream(musicTracks[currentTrack].name, out var stream))
            {
                if ((stream is RSDKFileStream rsdkStream) && rsdkStream.useEncryption)
                    Debug.WriteLine("Loading music from an encrypted stream! This will be slow and shit!");

                var reader = new VorbisReader(stream, true);
                var effect = new DynamicSoundEffectInstance(reader.SampleRate, (AudioChannels)reader.Channels);
                var bufferSize = effect.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(150));

                if (musicStartPos != 0)
                {
                    var oldPosition = streams[oldStream].reader.GetPosition().TotalSeconds;
                    var newPosition = oldPosition * ((double)musicRatio * 0.0001);
                    reader.SetPosition(TimeSpan.FromSeconds(newPosition));
                }

                streams[newStream].reader = reader;
                streams[newStream].effect = effect;
#if !FNA
                streams[newStream].buffer = new byte[bufferSize];
#endif
                streams[newStream].floatBuffer = new float[bufferSize / 2];
                streams[newStream].loop = musicTracks[currentTrack].loop;
                streams[newStream].loopPoint = TimeSpan.FromSeconds((double)musicTracks[currentTrack].loopPoint / reader.SampleRate);

                trackId = currentTrack;
                currentTrack = -1;
                musicPosition = 0;
                musicStartPos = 0;
                currentStream = newStream;
                musicStatus = MUSIC.PLAYING;
            }
        }
        catch (Exception ex)
        {
            musicStatus = MUSIC.STOPPED;
            Debug.WriteLine("Failed to load music! {0}", ex);
        }
    }

    public static void StopMusic(bool setStatus)
    {
        if (setStatus)
            musicStatus = MUSIC.STOPPED;
        musicPosition = 0;
    }

    public static void StopAllMusic()
    {

    }

    public static void PauseSound()
    {
        if (musicStatus == MUSIC.PLAYING)
        {
            musicStatus = MUSIC.PAUSED;
        }
    }

    public static void ResumeSound()
    {
        if (musicStatus == MUSIC.PAUSED)
        {
            musicStatus = MUSIC.PLAYING;
        }
    }


    private static void MusicThreadLoop()
    {
#if NO_THREADS
        var wait = new SpinWait();
#endif

        while (true)
        {
#if NO_THREADS
            wait.SpinOnce();
#else
            Thread.Sleep(25);
#endif

            if (musicStatus == MUSIC.LOADING)
            {
                streams[currentStream].effect?.Stop();
                LoadAudio();
                continue;
            }

            var streamFile = streams[currentStream];
            if (streamFile.reader == null || streamFile.effect == null)
                continue;

            var effect = streamFile.effect;
            var reader = streamFile.reader;

            if (musicStatus == MUSIC.PLAYING)
            {
                // sleep until we need a buffer
                if (!effect.IsDisposed && (musicStatus == MUSIC.PLAYING) && effect.PendingBufferCount > 2)
                    continue;

                if (musicStatus != MUSIC.PLAYING)
                    continue;

                // read the next chunk of data
                int samplesRead = reader.ReadSamples(streamFile.floatBuffer, 0, streamFile.floatBuffer.Length);

                // out of data and looping? reset the reader and read again
                if (samplesRead == 0 && streamFile.loop)
                {
                    reader.SetPosition(streamFile.loopPoint);
                    samplesRead = reader.ReadSamples(streamFile.floatBuffer, 0, streamFile.floatBuffer.Length);
                }

                if (samplesRead > 0)
                {
#if FNA
                    // submit our buffers
                    lock (effect)
                    {
                        // ensure the effect isn't disposed
                        if (effect.IsDisposed) { break; }

                        effect.SubmitFloatBufferEXT(streamFile.floatBuffer, 0, samplesRead);
                    }
#else
                    var buffer = streamFile.buffer;
                    var floatBuffer = streamFile.floatBuffer;
                    for (int i = 0; i < samplesRead; i++)
                    {
                        short sValue = (short)(short.MaxValue * floatBuffer[i]);
                        buffer[i * 2] = (byte)(sValue & 0xff);
                        buffer[i * 2 + 1] = (byte)((sValue >> 8) & 0xff);
                    }

                    // submit our buffers
                    lock (effect)
                    {
                        // ensure the effect isn't disposed
                        if (effect.IsDisposed) { break; }

                        effect.SubmitBuffer(buffer, 0, samplesRead * 2);
                    }
#endif

                    musicPosition = (int)(reader.GetPosition().TotalSeconds * reader.SampleRate);
                }

                lock (effect)
                {
                    if (effect.State != SoundState.Playing)
                        effect.Play();
                }
            }
            else
            {
                lock (effect)
                {
                    if (effect.State == SoundState.Playing)
                        effect.Pause();
                }
            }
        }
    }

    public static void SetGameVolumes(int bgmVol, int sfxVol)
    {
        bgmVolume = bgmVol;
        sfxVolume = sfxVol;

        if (bgmVolume < 0)
            bgmVolume = 0;
        if (bgmVolume > 100)
            bgmVolume = 100;

        if (sfxVolume < 0)
            sfxVolume = 0;
        if (sfxVolume > 100)
            sfxVolume = 100;
    }

    public static void SetMusicVolume(int volume)
    {
        if (volume < 0)
            volume = 0;
        if (volume > 100)
            volume = 100;

        bgmVolume = volume;
    }

    public static void PlaySfx(int sfx, bool loop)
    {
        PlaySfxWithAttributes(sfx, sfxVolume, 0, loop);
    }

    public static void StopSfx(int sfx)
    {
        for (int i = 0; i < soundChannels.Length; i++)
        {
            if (soundChannels[i].sfx == sfx)
                soundChannels[i].instance?.Stop();
        }
    }

    public static bool PlaySfxByName(string sfx, bool loopCnt)
    {
        for (int s = 0; s < TRACK_COUNT; ++s)
        {
            if (musicTracks[s]?.name == sfx)
            {
                PlaySfx(s, loopCnt);
                return true;
            }
        }

        Debug.WriteLine("Sound effect {0} not found??", (object)sfx);
        return false;
    }

    public static bool StopSFXByName(string sfx)
    {
        for (int s = 0; s < TRACK_COUNT; ++s)
        {
            if (musicTracks[s]?.name == sfx)
            {
                StopSfx(s);
                return true;
            }
        }
        return false;
    }

    public static void SetSfxAttributes(int sfx, int volume, int pan)
    {
        PlaySfxWithAttributes(sfx, volume, pan, false);
    }

    private static void PlaySfxWithAttributes(int sfx, int volume, int pan, bool loop)
    {
        for (int index = 0; index < SFX_CHANNELS; ++index)
        {
            if (soundChannels[index].sfx == sfx)
            {
                if (soundChannels[index].instance != null && !soundChannels[index].instance.IsDisposed)
                    soundChannels[index].instance.Stop();
                currentChannel = index;
                break;
            }
        }

        if (pan > 100)
            pan = 100;
        if (pan < -100)
            pan = -100;

        var instance = soundEffects[sfx].soundEffect?.CreateInstance();
        if (instance == null) return;

        instance.IsLooped = loop;
        instance.Pan = pan * 0.01f;
        instance.Volume = sfxVolume * 0.01f;
        soundChannels[currentChannel].instance = instance;
        soundChannels[currentChannel].sfx = sfx;

        instance.Play();

        ++currentChannel;
        if (currentChannel != SFX_CHANNELS)
            return;
        currentChannel = 0;
    }

    public static void SetSfxName(string name, int index)
    {
        soundEffects[index].name = name;
        Debug.WriteLine("Set SFX ({0}) name to: {1}", index, name);
    }
}
