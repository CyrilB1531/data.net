using System.Numerics;

namespace DataNet.Embeddings.Search;

/// <summary>SIMD-accelerated primitives over dense <see cref="float"/> vectors.</summary>
public static class VectorMath
{
    /// <summary>Computes the dot product of two equal-length vectors, vectorized via <see cref="Vector{T}"/>.</summary>
    public static float Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException($"length mismatch: {a.Length} vs {b.Length}.");
        }

        float sum = 0;
        int i = 0;
#if NET5_0_OR_GREATER
        // SIMD path (the span-based Vector<T> constructor is net-only).
        int width = Vector<float>.Count;
        var acc = Vector<float>.Zero;
        for (; i <= a.Length - width; i += width)
        {
            acc += new Vector<float>(a.Slice(i, width)) * new Vector<float>(b.Slice(i, width));
        }
        for (int j = 0; j < width; j++)
        {
            sum += acc[j];
        }
#endif
        for (; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }
        return sum;
    }

    /// <summary>Computes the Euclidean (L2) norm of a vector.</summary>
    public static float L2Norm(ReadOnlySpan<float> v) => (float)Math.Sqrt(Dot(v, v));
}
