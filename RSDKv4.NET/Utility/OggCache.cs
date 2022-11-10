using System.IO;
using System.Text;
using RSDKv4.External;

#if SILVERLIGHT
using System.IO.IsolatedStorage;
#elif WINDOWS_UWP
using Windows.Storage;
#endif

namespace RSDKv4.Utility;

internal class OggCache
{
    public static bool TryGetCachedSfx(string name, out Stream stream)
    {
        MD5Hash.GenerateFromString(name, out var digest);

        try
        {
#if SILVERLIGHT
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                stream = storage.OpenFile($"{digest}.wav", FileMode.Open);
                return true;
            }
#elif WINDOWS_UWP
            stream = File.OpenRead(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, $"{digest}.wav"));
            return true;
#else
            stream = File.OpenRead($"Cache/{digest}.wav");
            return true;
#endif
        }
        catch { };
        stream = null;
        return false;
    }

    public static void CacheSfx(string name, float[] data, byte[] pcmData, int sampleRate, int channels)
    {
        MD5Hash.GenerateFromString(name, out var digest);

        try
        {
#if SILVERLIGHT
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = storage.OpenFile($"{digest}.wav", FileMode.Create))
#elif WINDOWS_UWP
            using (var stream = File.Create(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, $"{digest}.wav")))
#else
            if (!Directory.Exists("Cache"))
                Directory.CreateDirectory("Cache");

            using (var stream = File.Create($"Cache/{digest}.wav"))
#endif
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
#if !NETCOREAPP
                // XNA on Windows Phone doesn't support floating point PCM
                const ushort wFormat = 1; // 16 bit signed 
                const ushort bitsPerSample = 16;
                const int bytesPerSample = 2;
#else
                const ushort wFormat = 3; // 32 bit floating point
                const ushort bitsPerSample = 32;
                const int bytesPerSample = 4;
#endif

                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(0);
                writer.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write(wFormat);
                writer.Write((ushort)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * bytesPerSample * channels);
                writer.Write((ushort)(bytesPerSample * channels));
                writer.Write(bitsPerSample);
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(data.Length * bytesPerSample);
#if !NETCOREAPP
                writer.Write(pcmData);
#else
                for (int i = 0; i < data.Length; i++)
                    writer.Write(data[i]);
#endif

                stream.Seek(4, SeekOrigin.Begin);
                writer.Write((int)stream.Length - 8);
            }
        }
        catch { };
    }
}
