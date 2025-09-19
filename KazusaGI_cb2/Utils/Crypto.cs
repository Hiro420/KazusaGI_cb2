using System;
using System.Security.Cryptography;
using System.Text;

namespace KazusaGI_cb2.Utils;

public class MT64
{
    private ulong[] mt = new ulong[312];
    private int mti = 313;

    public MT64()
    {
        for (int i = 0; i < 312; i++)
        {
            mt[i] = 0;
        }
    }

    public void Seed(ulong seed)
    {
        mt[0] = seed & 0xffffffffffffffff;
        for (int i = 1; i < 312; i++)
        {
            mt[i] = (6364136223846793005 * (mt[i - 1] ^ (mt[i - 1] >> 62)) + (ulong)i) & 0xffffffffffffffff;
        }
        mti = 312;
    }

    public void InitByArray(ulong[] key)
    {
        Seed(19650218);
        int i = 1;
        int j = 0;
        int k = Math.Max(312, key.Length);
        for (int ki = 0; ki < k; ki++)
        {
            mt[i] = ((mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 62)) * 3935559000370003845)) + key[j] + (ulong)j) & 0xffffffffffffffff;
            i++;
            j++;
            if (i >= 312)
            {
                mt[0] = mt[311];
                i = 1;
            }
            if (j >= key.Length)
            {
                j = 0;
            }
        }
        for (int ki = 0; ki < 312; ki++)
        {
            mt[i] = ((mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 62)) * 2862933555777941757)) - (ulong)i) & 0xffffffffffffffff;
            i++;
            if (i >= 312)
            {
                mt[0] = mt[311];
                i = 1;
            }
        }
        mt[0] = 1UL << 63;
    }

    public ulong Int64()
    {
        if (mti >= 312)
        {
            if (mti == 313)
            {
                Seed(5489);
            }

            for (int k = 0; k < 311; k++)
            {
                ulong y = (mt[k] & 0xFFFFFFFF80000000) | (mt[k + 1] & 0x7fffffff);
                if (k < 312 - 156)
                {
                    mt[k] = mt[k + 156] ^ (y >> 1) ^ ((y & 1) != 0 ? 0xB5026F5AA96619E9 : 0);
                }
                else
                {
                    mt[k] = mt[k + 156 - 312] ^ (y >> 1) ^ ((y & 1) != 0 ? 0xB5026F5AA96619E9 : 0);
                }
            }

            ulong lastY = (mt[311] & 0xFFFFFFFF80000000) | (mt[0] & 0x7fffffff);
            mt[311] = mt[155] ^ (lastY >> 1) ^ ((lastY & 1) != 0 ? 0xB5026F5AA96619E9 : 0);
            mti = 0;
        }

        ulong result = mt[mti];
        mti++;

        result ^= (result >> 29) & 0x5555555555555555;
        result ^= (result << 17) & 0x71D67FFFEDA60000;
        result ^= (result << 37) & 0xFFF7EEE000000000;
        result ^= (result >> 43);

        return result;
    }

    public ulong Int64b()
    {
        if (mti == 313)
        {
            Seed(5489);
        }

        int k = mti;

        if (k == 312)
        {
            k = 0;
            mti = 0;
        }

        ulong y;
        if (k == 311)
        {
            y = (mt[311] & 0xFFFFFFFF80000000) | (mt[0] & 0x7fffffff);
            mt[311] = mt[155] ^ (y >> 1) ^ ((y & 1) != 0 ? 0xB5026F5AA96619E9 : 0);
        }
        else
        {
            y = (mt[k] & 0xFFFFFFFF80000000) | (mt[k + 1] & 0x7fffffff);
            if (k < 312 - 156)
            {
                mt[k] = mt[k + 156] ^ (y >> 1) ^ ((y & 1) != 0 ? 0xB5026F5AA96619E9 : 0);
            }
            else
            {
                mt[k] = mt[k + 156 - 624] ^ (y >> 1) ^ ((y & 1) != 0 ? 0xB5026F5AA96619E9 : 0);
            }
        }

        ulong result = mt[mti];
        mti++;

        result ^= (result >> 29) & 0x5555555555555555;
        result ^= (result << 17) & 0x71D67FFFEDA60000;
        result ^= (result << 37) & 0xFFF7EEE000000000;
        result ^= (result >> 43);

        return result;
    }
}

public class Crypto
{
    public static byte[] NewKey(ulong seed)
    {
        MT64 mt = new MT64();
        mt.Seed(seed);
        byte[] key = new byte[512 * 8];
        int index = 0;
        for (int i = 0; i < 512; i++)
        {
            ulong value = mt.Int64();
            byte[] valueBytes = BitConverter.GetBytes(value);

            // Convert to big-endian if necessary
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(valueBytes);
            }

            Array.Copy(valueBytes, 0, key, index, valueBytes.Length);
            index += valueBytes.Length;
        }
        return key;
    }

    public static byte[] Xor(byte[] data, byte[] key)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        return result;
    }
}
