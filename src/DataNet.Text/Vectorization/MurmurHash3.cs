namespace DataNet.Text.Vectorization;

// SonarLint S907: the canonical MurmurHash3 tail fall-through, kept verbatim from the reference.
#pragma warning disable S907

/// <summary>
/// MurmurHash3 x86 32-bit, matching the hash scikit-learn uses for feature hashing
/// (<c>sklearn.utils.murmurhash.murmurhash3_32</c>) with seed 0.
/// </summary>
internal static class MurmurHash3
{
    /// <summary>Computes the 32-bit MurmurHash3 of <paramref name="data"/> as a signed integer.</summary>
    public static int Hash32(ReadOnlySpan<byte> data, uint seed = 0)
    {
        unchecked
        {
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            uint h1 = seed;
            int len = data.Length;
            int blocks = len / 4;

            for (int i = 0; i < blocks; i++)
            {
                int b = i * 4;
                uint k1 = (uint)(data[b] | (data[b + 1] << 8) | (data[b + 2] << 16) | (data[b + 3] << 24));
                k1 *= c1;
                k1 = Rotl(k1, 15);
                k1 *= c2;
                h1 ^= k1;
                h1 = Rotl(h1, 13);
                h1 = (h1 * 5) + 0xe6546b64;
            }

            uint k = 0;
            int tail = blocks * 4;
            switch (len & 3)
            {
                case 3:
                    k ^= (uint)data[tail + 2] << 16;
                    goto case 2;
                case 2:
                    k ^= (uint)data[tail + 1] << 8;
                    goto case 1;
                case 1:
                    k ^= data[tail];
                    k *= c1;
                    k = Rotl(k, 15);
                    k *= c2;
                    h1 ^= k;
                    break;
            }

            h1 ^= (uint)len;
            h1 = Fmix(h1);
            return (int)h1;
        }
    }

    private static uint Rotl(uint x, int r) => (x << r) | (x >> (32 - r));

    private static uint Fmix(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }
    }
}
