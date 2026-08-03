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

        int width = Vector<float>.Count;
        var acc = Vector<float>.Zero;
        int i = 0;
        for (; i <= a.Length - width; i += width)
        {
            acc += new Vector<float>(a.Slice(i, width)) * new Vector<float>(b.Slice(i, width));
        }

        float sum = Vector.Sum(acc);
        for (; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }
        return sum;
    }

    /// <summary>Computes the Euclidean (L2) norm of a vector.</summary>
    public static float L2Norm(ReadOnlySpan<float> v) => MathF.Sqrt(Dot(v, v));
}
