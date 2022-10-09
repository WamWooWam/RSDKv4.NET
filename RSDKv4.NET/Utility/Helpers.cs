using System;

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
}
