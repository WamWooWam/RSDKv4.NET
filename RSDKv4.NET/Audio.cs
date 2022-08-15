using Microsoft.Xna.Framework.Audio;
using NVorbis;
using RSDKv4.External;
using RSDKv4.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace RSDKv4;

internal class Audio
{
    public const int TRACK_COUNT = 0x10;
    public static TrackInfo[] musicTracks = new TrackInfo[TRACK_COUNT];
    public static int currentTrack;

    public const int SFX_COUNT = 0x100;
    public static SfxInfo[] soundEffects = new SfxInfo[SFX_COUNT];

    public const int SFX_CHANNELS = 0x10;
    public static SfxChannel[] soundChannels = new SfxChannel[SFX_CHANNELS];
    public static int currentChannel = 0;

    public static int sfxVolume = 50;
    public static int bgmVolume = 100;
    public static int masterVolume;
    public static int trackId;
    public static int musicPosition;
    public static int musicRatio;
    public static byte stageSfxCount;

    public static int musicStatus;

    private static TimeSpan lastTime;

    static Audio()
    {
        Helpers.Memset(musicTracks, () => new TrackInfo());
        Helpers.Memset(soundEffects, () => new SfxInfo());
        Helpers.Memset(soundChannels, () => new SfxChannel());
    }

    public static bool InitAudioPlayback()
    {
        for (int i = 0; i < Engine.globalSfxCount; i++)
        {
            RSDKv4Game.loadPercent = 0.1f + (0.75f * ((i + 1) / (float)Engine.globalSfxCount));
            LoadSfx(Engine.globalSfxPaths[i], (byte)i);
        }

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

    public static void StopAllMusic()
    {
        foreach (var item in musicTracks)
        {
            item?.song?.Stop();
        }
    }

    public static void StopAllSfx()
    {

    }

    public static void ReleaseStageSfx()
    {

    }

    internal static void SetMusicTrack(string filePath, byte trackId, bool loop, uint loopPoint)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            musicTracks[trackId].song?.Dispose();
            musicTracks[trackId] = new TrackInfo();
        }
        else
        {
            if (musicTracks[trackId].name == filePath) return;

            var nextTrack = "Data/Music/" + filePath;
            if (FileIO.LoadFile(nextTrack, out var file))
            {
                var stream = FileIO.CreateFileStream();
                var oggSound = new OggSong(stream);
                oggSound.IsLooped = loop;
                oggSound.LoopPoint = TimeSpan.FromSeconds((double)loopPoint / oggSound.SampleRate);

                musicTracks[trackId] = new TrackInfo() { name = filePath, song = oggSound };
            }
        }
    }

    internal static void PlayMusic(int track, int musStartPos)
    {
        StopAllMusic();

        if (!string.IsNullOrWhiteSpace(musicTracks[track].name))
        {
            currentTrack = track;
            musicStatus = MUSIC.PLAYING;

            var position = TimeSpan.Zero;
            if (musStartPos != 0)            
                position = TimeSpan.FromSeconds(lastTime.TotalSeconds * (musicRatio * 0.0001));            

            musicTracks[track].song.Play(position);
        }
        else
        {
            StopMusic(true);
        }
    }

    internal static void StopMusic(bool setStatus)
    {
        if (setStatus)
        {
            musicStatus = MUSIC.STOPPED;
        }

        musicPosition = 0;
        StopAllMusic();
    }

    internal static void PauseSound()
    {
        //throw new NotImplementedException();
    }

    internal static void ResumeSound()
    {
        //throw new NotImplementedException();
    }

    internal static void SwapMusicTrack(string filePath, byte trackId, uint loopPoint, int ratio)
    {
        lastTime = musicTracks[currentTrack].song?.Position ?? TimeSpan.Zero;
        musicRatio = ratio;

        StopAllMusic();

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            if (musicTracks[trackId].name != filePath)
            {
                SetMusicTrack(filePath, trackId, true, loopPoint);
                PlayMusic(trackId, 1);
            }
        }
        else
        {
            StopMusic(true);
        }
    }

    internal static void PlaySfx(int sfx, bool loop)
    {
        var sfxChannel = currentChannel;
        if (sfxChannel >= SFX_CHANNELS)
            sfxChannel = 0;
        currentChannel = sfxChannel + 1;

        var channel = soundChannels[sfxChannel];
        if (channel.instance != null)
            channel.instance.Dispose();

        channel.instance = soundEffects[sfx].soundEffect?.CreateInstance();
        channel.instance.IsLooped = loop;
        channel.instance.Volume = sfxVolume / 100.0f;
        channel.instance.Play();
    }

    internal static void StopSfx(int sfx)
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
    internal static void SetSfxAttributes(int v1, int v2, int v3)
    {
        //throw new NotImplementedException();
    }

    internal static void SetGameVolumes(int bgmVolume, int sfxVolume)
    {
        //throw new NotImplementedException();
    }

    internal static void SetMusicVolume(int v)
    {

    }

    internal static void SetSfxName(string name, int index)
    {
        Debug.WriteLine("Set SFX ({0}) name to: {1}", index, name);
    }
}
