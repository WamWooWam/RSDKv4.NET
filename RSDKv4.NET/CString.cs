using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSDKv4;

public static class CString
{
    public static void StrCopy(ref char[] strA, ref char[] strB)
    {
        int index = 0;
        bool flag = true;
        if (index == strB.Length || index == strA.Length)
            flag = false;
        while (flag)
        {
            strA[index] = strB[index];
            if (strB[index] == char.MinValue)
                flag = false;
            ++index;
            if (index == strB.Length || index == strA.Length)
                flag = false;
        }
        for (; index < strA.Length; ++index)
            strA[index] = char.MinValue;
    }

    public static void StrClear(ref char[] strA)
    {
        for (int index = 0; index < strA.Length; ++index)
            strA[index] = char.MinValue;
    }

    public static void StrCopy2D(ref char[,] strA, ref char[] strB, int strPos)
    {
        int index = 0;
        bool flag = true;
        if (index == strB.Length || index == strA.GetLength(1))
            flag = false;
        while (flag)
        {
            strA[strPos, index] = strB[index];
            if (strB[index] == char.MinValue)
                flag = false;
            ++index;
            if (index == strB.Length || index == strA.GetLength(1))
                flag = false;
        }
        for (; index < strA.GetLength(1); ++index)
            strA[strPos, index] = char.MinValue;
    }

    public static void StrAdd(ref char[] strA, ref char[] strB)
    {
        int index1 = 0;
        int index2 = 0;
        bool flag = true;
        while (strA[index1] != char.MinValue)
            ++index1;
        if (index2 == strB.Length || index1 == strA.Length)
            flag = false;
        while (flag)
        {
            strA[index1] = strB[index2];
            if (strB[index2] == char.MinValue)
                flag = false;
            ++index1;
            ++index2;
            if (index2 == strB.Length || index1 == strA.Length)
                flag = false;
        }
        for (; index1 < strA.Length; ++index1)
            strA[index1] = char.MinValue;
    }

    public static bool StringComp(ref char[] strA, ref char[] strB)
    {
        bool flag = true;
        int num = 0;
        int index1 = 0;
        int index2 = 0;
        while (num < 1)
        {
            if ((int)strA[index1] != (int)strB[index2] && (int)strA[index1] != (int)strB[index2] + 32 && (int)strA[index1] != (int)strB[index2] - 32)
            {
                flag = false;
                num = 1;
            }
            else if (strA[index1] == char.MinValue)
            {
                num = 1;
            }
            else
            {
                ++index1;
                ++index2;
            }
        }
        return flag;
    }

    public static int StrLen(ref char[] strA)
    {
        int index = 0;
        if (strA.Length == 0)
            return index;
        while (strA[index] != char.MinValue && index < strA.Length)
            ++index;
        return index;
    }

    public static int FindStringToken(ref char[] strA, ref char[] token, char instance)
    {
        int index1 = 0;
        int num1 = -1;
        int num2 = 0;
        for (; strA[index1] != char.MinValue; ++index1)
        {
            int index2 = 0;
            bool flag = true;
            for (; token[index2] != char.MinValue; ++index2)
            {
                if (strA[index1 + index2] == char.MinValue)
                    return num1;
                if ((int)strA[index1 + index2] != (int)token[index2])
                    flag = false;
            }
            if (flag)
            {
                ++num2;
                if (num2 == (int)instance)
                    return index1;
            }
        }
        return num1;
    }

    public static bool ConvertStringToInteger(string strA, int index1, out int sValue)
    {
        bool flag = false;
        sValue = 0;
        if ((strA[index1] <= '/' || strA[index1] >= ':') && (strA[index1] != '-' && strA[index1] != '+'))
            return false;
        int num1 = strA.Length - 1;
        if (strA[index1] == '-')
        {
            flag = true;
            ++index1;
            --num1;
        }
        else if (strA[index1] == '+')
        {
            ++index1;
            --num1;
        }
        while (num1 > -1)
        {
            if (strA[index1] <= '/' || strA[index1] >= ':')
                return false;
            if (num1 > 0)
            {
                int num2 = (int)strA[index1] - 48;
                for (int index2 = num1; index2 > 0; --index2)
                    num2 *= 10;
                sValue += num2;
            }
            else
                sValue += (int)strA[index1] - 48;
            --num1;
            ++index1;
        }
        if (flag)
            sValue = -sValue;
        return true;
    }

    public static void StringLowerCase(ref char[] dest, ref char[] src)
    {
        int index = 0;
        bool flag = true;
        if (index == src.Length || index == dest.Length)
            flag = false;
        while (flag)
        {
            dest[index] = char.ToLowerInvariant(src[index]);

            if (src[index] == char.MinValue)
                flag = false;

            ++index;

            if (index == src.Length || index == dest.Length)
                flag = false;
        }
        for (; index < dest.Length; ++index)
            dest[index] = char.MinValue;
    }
}
