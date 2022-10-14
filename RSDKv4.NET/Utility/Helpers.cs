using NVorbis;
using System;
using System.Diagnostics;

namespace RSDKv4.Utility;

public static class Helpers
{
    public static void Memset<T>(T[] arr, T value)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = value;
        }
    }

    public static void Memset<T>(T[] arr, Func<T> factory)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = factory();
        }
    }

    public static TimeSpan GetPosition(this VorbisReader reader)
    {
#if SILVERLIGHT
        return reader.DecodedTime;
#else
        return reader.TimePosition;
#endif
    }

    public static void SetPosition(this VorbisReader reader, TimeSpan value)
    {
        try
        {
#if SILVERLIGHT
            if (reader.DecodedTime != value)
                reader.DecodedTime = value;
#else
            if (reader.TimePosition != value)
                reader.TimePosition = value;
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine("NVorbis sucks {0}", ex);
        }
    }
}
