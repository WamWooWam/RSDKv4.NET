using Microsoft.Xna.Framework;
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

    /// <summary>
    /// Creates a new rotation <see cref="Matrix"/> around Y axis. Inverts the Y axis because RSDK.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The rotation <see cref="Matrix"/> around Y axis.</returns>
    public static Matrix CreateRotationY(float radians)
    {
        Matrix result;
        CreateRotationY(radians, out result);
        return result;
    }

    /// <summary>
    /// Creates a new rotation <see cref="Matrix"/> around Y axis. Inverts the Y axis because RSDK.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <param name="result">The rotation <see cref="Matrix"/> around Y axis as an output parameter.</param>
    public static void CreateRotationY(float radians, out Matrix result)
    {
        result = Matrix.Identity;

        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);

        result.M11 = cos;
        result.M13 = sin;
        result.M31 = -sin;
        result.M33 = cos;
    }
}
