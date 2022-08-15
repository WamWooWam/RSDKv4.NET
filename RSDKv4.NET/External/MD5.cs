using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSDKv4.External;
public class MD5
{
    private static byte[] md5_padding = new byte[64]
      {
  (byte) 128,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0,
  (byte) 0
      };

    private static void GET_UINT32(ref uint n, byte[] b, int dataIndex, int i)
    {
        n = (uint)((int)b[dataIndex + i] | (int)b[dataIndex + i + 1] << 8 | (int)b[dataIndex + i + 2] << 16 | (int)b[dataIndex + i + 3] << 24);
    }

    private static void PUT_UINT32(uint n, ref byte[] b, int i)
    {
        b[i] = (byte)n;
        b[i + 1] = (byte)(n >> 8);
        b[i + 2] = (byte)(n >> 16);
        b[i + 3] = (byte)(n >> 24);
    }

    private static uint S(uint x, uint n)
    {
        return x << (int)n | (x & uint.MaxValue) >> 32 - (int)n;
    }

    private static void P(
        ref uint a,
        uint b,
        uint c,
        uint d,
        uint k,
        uint s,
        uint t,
        uint[] X,
        MD5.FuncF F)
    {
        a += F(b, c, d) + X[k] + t;
        a = S(a, s) + b;
    }

    private static uint F_1(uint x, uint y, uint z)
    {
        return z ^ x & (y ^ z);
    }

    private static uint F_2(uint x, uint y, uint z)
    {
        return y ^ z & (x ^ y);
    }

    private static uint F_3(uint x, uint y, uint z)
    {
        return x ^ y ^ z;
    }

    private static uint F_4(uint x, uint y, uint z)
    {
        return y ^ (x | ~z);
    }

    private static void md5_starts(ref MD5.md5_context ctx)
    {
        ctx.total[0] = 0U;
        ctx.total[1] = 0U;
        ctx.state[0] = 1732584193U;
        ctx.state[1] = 4023233417U;
        ctx.state[2] = 2562383102U;
        ctx.state[3] = 271733878U;
    }
    private static void md5_process(ref MD5.md5_context ctx, byte[] data, int dataIndex)
    {
        uint[] X = new uint[16];
        GET_UINT32(ref X[0], data, dataIndex, 0);
        GET_UINT32(ref X[1], data, dataIndex, 4);
        GET_UINT32(ref X[2], data, dataIndex, 8);
        GET_UINT32(ref X[3], data, dataIndex, 12);
        GET_UINT32(ref X[4], data, dataIndex, 16);
        GET_UINT32(ref X[5], data, dataIndex, 20);
        GET_UINT32(ref X[6], data, dataIndex, 24);
        GET_UINT32(ref X[7], data, dataIndex, 28);
        GET_UINT32(ref X[8], data, dataIndex, 32);
        GET_UINT32(ref X[9], data, dataIndex, 36);
        GET_UINT32(ref X[10], data, dataIndex, 40);
        GET_UINT32(ref X[11], data, dataIndex, 44);
        GET_UINT32(ref X[12], data, dataIndex, 48);
        GET_UINT32(ref X[13], data, dataIndex, 52);
        GET_UINT32(ref X[14], data, dataIndex, 56);
        GET_UINT32(ref X[15], data, dataIndex, 60);
        uint a1 = ctx.state[0];
        uint a2 = ctx.state[1];
        uint a3 = ctx.state[2];
        uint a4 = ctx.state[3];
        MD5.FuncF F1 = new MD5.FuncF(F_1);
        P(ref a1, a2, a3, a4, 0U, 7U, 3614090360U, X, F1);
        P(ref a4, a1, a2, a3, 1U, 12U, 3905402710U, X, F1);
        P(ref a3, a4, a1, a2, 2U, 17U, 606105819U, X, F1);
        P(ref a2, a3, a4, a1, 3U, 22U, 3250441966U, X, F1);
        P(ref a1, a2, a3, a4, 4U, 7U, 4118548399U, X, F1);
        P(ref a4, a1, a2, a3, 5U, 12U, 1200080426U, X, F1);
        P(ref a3, a4, a1, a2, 6U, 17U, 2821735955U, X, F1);
        P(ref a2, a3, a4, a1, 7U, 22U, 4249261313U, X, F1);
        P(ref a1, a2, a3, a4, 8U, 7U, 1770035416U, X, F1);
        P(ref a4, a1, a2, a3, 9U, 12U, 2336552879U, X, F1);
        P(ref a3, a4, a1, a2, 10U, 17U, 4294925233U, X, F1);
        P(ref a2, a3, a4, a1, 11U, 22U, 2304563134U, X, F1);
        P(ref a1, a2, a3, a4, 12U, 7U, 1804603682U, X, F1);
        P(ref a4, a1, a2, a3, 13U, 12U, 4254626195U, X, F1);
        P(ref a3, a4, a1, a2, 14U, 17U, 2792965006U, X, F1);
        P(ref a2, a3, a4, a1, 15U, 22U, 1236535329U, X, F1);
        MD5.FuncF F2 = new MD5.FuncF(F_2);
        P(ref a1, a2, a3, a4, 1U, 5U, 4129170786U, X, F2);
        P(ref a4, a1, a2, a3, 6U, 9U, 3225465664U, X, F2);
        P(ref a3, a4, a1, a2, 11U, 14U, 643717713U, X, F2);
        P(ref a2, a3, a4, a1, 0U, 20U, 3921069994U, X, F2);
        P(ref a1, a2, a3, a4, 5U, 5U, 3593408605U, X, F2);
        P(ref a4, a1, a2, a3, 10U, 9U, 38016083U, X, F2);
        P(ref a3, a4, a1, a2, 15U, 14U, 3634488961U, X, F2);
        P(ref a2, a3, a4, a1, 4U, 20U, 3889429448U, X, F2);
        P(ref a1, a2, a3, a4, 9U, 5U, 568446438U, X, F2);
        P(ref a4, a1, a2, a3, 14U, 9U, 3275163606U, X, F2);
        P(ref a3, a4, a1, a2, 3U, 14U, 4107603335U, X, F2);
        P(ref a2, a3, a4, a1, 8U, 20U, 1163531501U, X, F2);
        P(ref a1, a2, a3, a4, 13U, 5U, 2850285829U, X, F2);
        P(ref a4, a1, a2, a3, 2U, 9U, 4243563512U, X, F2);
        P(ref a3, a4, a1, a2, 7U, 14U, 1735328473U, X, F2);
        P(ref a2, a3, a4, a1, 12U, 20U, 2368359562U, X, F2);
        MD5.FuncF F3 = new MD5.FuncF(F_3);
        P(ref a1, a2, a3, a4, 5U, 4U, 4294588738U, X, F3);
        P(ref a4, a1, a2, a3, 8U, 11U, 2272392833U, X, F3);
        P(ref a3, a4, a1, a2, 11U, 16U, 1839030562U, X, F3);
        P(ref a2, a3, a4, a1, 14U, 23U, 4259657740U, X, F3);
        P(ref a1, a2, a3, a4, 1U, 4U, 2763975236U, X, F3);
        P(ref a4, a1, a2, a3, 4U, 11U, 1272893353U, X, F3);
        P(ref a3, a4, a1, a2, 7U, 16U, 4139469664U, X, F3);
        P(ref a2, a3, a4, a1, 10U, 23U, 3200236656U, X, F3);
        P(ref a1, a2, a3, a4, 13U, 4U, 681279174U, X, F3);
        P(ref a4, a1, a2, a3, 0U, 11U, 3936430074U, X, F3);
        P(ref a3, a4, a1, a2, 3U, 16U, 3572445317U, X, F3);
        P(ref a2, a3, a4, a1, 6U, 23U, 76029189U, X, F3);
        P(ref a1, a2, a3, a4, 9U, 4U, 3654602809U, X, F3);
        P(ref a4, a1, a2, a3, 12U, 11U, 3873151461U, X, F3);
        P(ref a3, a4, a1, a2, 15U, 16U, 530742520U, X, F3);
        P(ref a2, a3, a4, a1, 2U, 23U, 3299628645U, X, F3);
        MD5.FuncF F4 = new MD5.FuncF(F_4);
        P(ref a1, a2, a3, a4, 0U, 6U, 4096336452U, X, F4);
        P(ref a4, a1, a2, a3, 7U, 10U, 1126891415U, X, F4);
        P(ref a3, a4, a1, a2, 14U, 15U, 2878612391U, X, F4);
        P(ref a2, a3, a4, a1, 5U, 21U, 4237533241U, X, F4);
        P(ref a1, a2, a3, a4, 12U, 6U, 1700485571U, X, F4);
        P(ref a4, a1, a2, a3, 3U, 10U, 2399980690U, X, F4);
        P(ref a3, a4, a1, a2, 10U, 15U, 4293915773U, X, F4);
        P(ref a2, a3, a4, a1, 1U, 21U, 2240044497U, X, F4);
        P(ref a1, a2, a3, a4, 8U, 6U, 1873313359U, X, F4);
        P(ref a4, a1, a2, a3, 15U, 10U, 4264355552U, X, F4);
        P(ref a3, a4, a1, a2, 6U, 15U, 2734768916U, X, F4);
        P(ref a2, a3, a4, a1, 13U, 21U, 1309151649U, X, F4);
        P(ref a1, a2, a3, a4, 4U, 6U, 4149444226U, X, F4);
        P(ref a4, a1, a2, a3, 11U, 10U, 3174756917U, X, F4);
        P(ref a3, a4, a1, a2, 2U, 15U, 718787259U, X, F4);
        P(ref a2, a3, a4, a1, 9U, 21U, 3951481745U, X, F4);
        ctx.state[0] += a1;
        ctx.state[1] += a2;
        ctx.state[2] += a3;
        ctx.state[3] += a4;
    }

    private static void md5_update(ref MD5.md5_context ctx, byte[] input, uint length)
    {
        if (length == 0U)
            return;
        uint destinationIndex = ctx.total[0] & 63U;
        uint length1 = 64U - destinationIndex;
        ctx.total[0] += length;
        ctx.total[0] &= uint.MaxValue;
        if (ctx.total[0] < length)
            ++ctx.total[1];
        int num = 0;
        if (destinationIndex != 0U && length >= length1)
        {
            Array.Copy((Array)input, num, (Array)ctx.buffer, (int)destinationIndex, (int)length1);
            md5_process(ref ctx, ctx.buffer, 0);
            length -= length1;
            num += (int)length1;
            destinationIndex = 0U;
        }
        while (length >= 64U)
        {
            md5_process(ref ctx, input, num);
            length -= 64U;
            num += 64;
        }
        if (length == 0U)
            return;
        Array.Copy((Array)input, num, (Array)ctx.buffer, (int)destinationIndex, (int)length);
    }

    private static void md5_finish(ref MD5.md5_context ctx, out MD5Digest digest)
    {
        byte[] b = new byte[8];
        uint n = ctx.total[0] >> 29 | ctx.total[1] << 3;
        PUT_UINT32(ctx.total[0] << 3, ref b, 0);
        PUT_UINT32(n, ref b, 4);
        uint num = ctx.total[0] & 63U;
        uint length = num < 56U ? 56U - num : 120U - num;
        md5_update(ref ctx, md5_padding, length);
        md5_update(ref ctx, b, 8U);
        //PUT_UINT32(ctx.state[0], ref digest, 0);
        //PUT_UINT32(ctx.state[1], ref digest, 4);
        //PUT_UINT32(ctx.state[2], ref digest, 8);
        //PUT_UINT32(ctx.state[3], ref digest, 12);

        digest = new MD5Digest() { u1 = ctx.state[0], u2 = ctx.state[1], u3 = ctx.state[2], u4 = ctx.state[3] };
    }

    private class md5_context
    {
        public uint[] total;
        public uint[] state;
        public byte[] buffer;

        public md5_context()
        {
            this.total = new uint[2];
            this.state = new uint[4];
            this.buffer = new byte[64];
        }
    }

    private delegate uint FuncF(uint x, uint y, uint z);

    public static void GenerateFromString(string data, out MD5Digest digest)
    {
        byte[] input = new byte[data.Length];
        for (int index = 0; index < data.Length; ++index)
        {
            input[index] = (byte)data[index];
        }
        md5_context ctx = new md5_context();
        md5_starts(ref ctx);
        md5_update(ref ctx, input, (uint)input.Length);
        md5_finish(ref ctx, out digest);
    }
}

public struct MD5Digest
{
    public uint u1;
    public uint u2;
    public uint u3;
    public uint u4;

    public static bool operator ==(MD5Digest lhs, MD5Digest rhs)
    {
        return lhs.u1 == rhs.u1 && lhs.u2 == rhs.u2 && lhs.u3 == rhs.u3 && lhs.u4 == rhs.u4;
    }

    public static bool operator !=(MD5Digest lhs, MD5Digest rhs)
    {
        return lhs.u1 != rhs.u1 || lhs.u2 != rhs.u2 || lhs.u3 != rhs.u3 || lhs.u4 != rhs.u4;
    }

    public override bool Equals(object obj)
    {
        if (obj is not MD5Digest digest) return false;
        return digest == this;
    }

    public override int GetHashCode()
    {
        return (int)(u1 ^ u2 ^ u3 ^ u4);
    }

    public override string ToString()
    {
        var chars = new char[32];
        var bytes = new byte[16];
        BitHelper.CopyTo(u1, bytes, 0);
        BitHelper.CopyTo(u2, bytes, 4);
        BitHelper.CopyTo(u3, bytes, 8);
        BitHelper.CopyTo(u4, bytes, 12);

        for (int i = 0; i < bytes.Length; i++)
        {
            var nb1 = bytes[i] / 16;
            var nb2 = bytes[i] & 16;

            chars[2 * i] = BitHelper.ToHexDigit((bytes[i] >> 4) & 0xF);
            chars[2 * i + 1] = BitHelper.ToHexDigit(bytes[i] & 0xF);
        }

        return new string(chars);
    }
}
