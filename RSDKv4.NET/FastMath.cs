using System;

namespace RSDKv4;

public class FastMath
{
    public static int[] sinValM7 = new int[0x200];
    public static int[] cosValM7 = new int[0x200];

    public static int[] sinVal512 = new int[0x200];
    public static int[] cosVal512 = new int[0x200];

    public static int[] sinVal256 = new int[0x100];
    public static int[] cosVal256 = new int[0x100];

    public static byte[] atanVal256 = new byte[0x100 * 0x100];

    private static Random _random = new Random();

    public static void SetRandomSeed(int x)
    {
        _random = new Random(x);
    }

    public static void CalculateTrigAngles()
    {
        for (int i = 0; i < 0x200; ++i)
        {
#if NETCOREAPP3_1
            sinValM7[i] = (int)(MathF.Sin((i / 256.0f) * MathF.PI) * 4096.0f);
            cosValM7[i] = (int)(MathF.Cos((i / 256.0f) * MathF.PI) * 4096.0f);
#else
            sinValM7[i] = (int)(Math.Sin((i / 256.0) * Math.PI) * 4096.0);
            cosValM7[i] = (int)(Math.Cos((i / 256.0) * Math.PI) * 4096.0);
#endif
        }

        cosValM7[0] = 0x1000;
        cosValM7[128] = 0;
        cosValM7[256] = -0x1000;
        cosValM7[384] = 0;
        sinValM7[0] = 0;
        sinValM7[128] = 0x1000;
        sinValM7[256] = 0;
        sinValM7[384] = -0x1000;

        for (int i = 0; i < 0x200; ++i)
        {
#if NETCOREAPP3_1
            sinVal512[i] = (int)(MathF.Sin((i / 256.0f) * MathF.PI) * 512.0f);
            cosVal512[i] = (int)(MathF.Cos((i / 256.0f) * MathF.PI) * 512.0f);
#else
            sinVal512[i] = (int)(Math.Sin((i / 256.0) * Math.PI) * 512.0);
            cosVal512[i] = (int)(Math.Cos((i / 256.0) * Math.PI) * 512.0);
#endif
        }

        cosVal512[0] = 0x200;
        cosVal512[128] = 0;
        cosVal512[256] = -0x200;
        cosVal512[384] = 0;
        sinVal512[0] = 0;
        sinVal512[128] = 0x200;
        sinVal512[256] = 0;
        sinVal512[384] = -0x200;

        for (int i = 0; i < 0x100; i++)
        {
            sinVal256[i] = (sinVal512[i * 2] >> 1);
            cosVal256[i] = (cosVal512[i * 2] >> 1);
        }

        for (int Y = 0; Y < 0x100; ++Y)
        {
            var offset = Y;
            for (int X = 0; X < 0x100; ++X)
            {
                float angle = (float)Math.Atan2(Y, X);
                atanVal256[offset] = (byte)(angle * 40.743664f);
                offset += 0x100;
            }
        }
    }

    public static byte ArcTan(int X, int Y)
    {
        int x = Math.Abs(X);
        int y = Math.Abs(Y);

        if (x <= y)
        {
            while (y > 0xFF)
            {
                x >>= 4;
                y >>= 4;
            }
        }
        else
        {
            while (x > 0xFF)
            {
                x >>= 4;
                y >>= 4;
            }
        }
        if (X <= 0)
        {
            if (Y <= 0)
                return (byte)(atanVal256[(x << 8) + y] + -0x80);
            else
                return (byte)(-0x80 - atanVal256[(x << 8) + y]);
        }
        else if (Y <= 0)
            return (byte)(-atanVal256[(x << 8) + y]);
        else
            return (byte)(atanVal256[(x << 8) + y]);
    }

    public static int Sin512(int angle)
    {
        if (angle < 0)
            angle = 0x200 - angle;
        angle &= 0x1FF;
        return sinVal512[angle];
    }

    public static int Cos512(int angle)
    {
        if (angle < 0)
            angle = 0x200 - angle;
        angle &= 0x1FF;
        return cosVal512[angle];
    }

    public static int Sin256(int angle)
    {
        if (angle < 0)
            angle = 0x100 - angle;
        angle &= 0xFF;
        return sinVal256[angle];
    }

    public static int Cos256(int angle)
    {
        if (angle < 0)
            angle = 0x100 - angle;
        angle &= 0xFF;
        return cosVal256[angle];
    }

    public static int Rand(int v)
    {
        return _random.Next(v);
    }
}
