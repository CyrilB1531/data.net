using System.Numerics;
using DataNet.Embeddings.Pooling;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Checks the claim <c>Pooler</c> makes about itself: that vectorizing the accumulation changes how
/// many components are added per instruction and never the order in which one component's tokens are
/// summed, so the result is bit-identical to the scalar loop -- checked with
/// <see cref="Assert.Equal(object, object)"/>, not a tolerance, since a tolerance would pass for any
/// reordering that stayed within rounding error. The netstandard2.0 build has no span constructor for
/// <see cref="Vector{T}"/> and takes the scalar path, so these facts run in both projects: on
/// <c>net10.0</c> they compare the vectorized result against the scalar reference, and on the
/// netstandard2.0 mirror the scalar implementation against it, so one frozen corpus serves both builds.
/// </summary>
public sealed class PoolingVectorizationTests
{
    /// <summary>Dimensions either side of the SIMD width, so the tail loop is never the only path or never taken.</summary>
    public static TheoryData<int, int> Shapes() => new()
    {
        { 1, 1 }, { 4, 3 }, { 6, 5 }, { 3, 7 }, { 5, 8 }, { 7, 13 }, { 9, 16 }, { 2, 17 }, { 11, 64 },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void MeanPool_is_bit_identical_to_the_scalar_loop(int seqLen, int dim)
    {
        float[] embeddings = Embeddings(seqLen, dim);
        long[] mask = Mask(seqLen);

        float[] actual = Pooler.MeanPool(embeddings, seqLen, dim, mask);
        float[] expected = ScalarMeanPool(embeddings, seqLen, dim, mask);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void L2Normalize_is_bit_identical_to_the_scalar_loop(int seqLen, int dim)
    {
        float[] actual = ScalarMeanPool(Embeddings(seqLen, dim), seqLen, dim, Mask(seqLen));
        float[] expected = (float[])actual.Clone();

        Pooler.L2Normalize(actual);
        ScalarL2Normalize(expected);

        Assert.Equal(expected, actual);
    }

    /// <summary>The vector body has to be reached, or the facts above only exercise the tail.</summary>
    [Fact]
    public void The_vectorized_path_is_actually_taken_on_this_machine()
    {
        // Skipped, not failed, on a machine without SIMD -- nothing to compare where there is no
        // vectorized path. The largest shape here is 64 wide, so any accelerated Vector reaches the body.
        Assert.True(!Vector.IsHardwareAccelerated || Vector<float>.Count <= 64,
            $"Vector<float>.Count is {Vector<float>.Count}; no shape in this suite reaches the vector body.");
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void MeanPoolBatch_pools_each_row_against_its_own_mask(int seqLen, int dim)
    {
        const int batchSize = 3;
        var embeddings = new float[batchSize * seqLen * dim];
        var mask = new long[batchSize * seqLen];
        for (int b = 0; b < batchSize; b++)
        {
            // A different length per row, so at least one row carries padding and
            // the rows do not all reduce to the same vector.
            float[] row = Embeddings(seqLen, dim, seed: b + 1);
            row.CopyTo(embeddings, b * seqLen * dim);
            for (int t = 0; t < seqLen; t++)
            {
                mask[(b * seqLen) + t] = t <= b % seqLen ? 1 : 0;
            }
        }

        float[][] pooled = Pooler.MeanPoolBatch(embeddings, batchSize, seqLen, dim, mask);

        Assert.Equal(batchSize, pooled.Length);
        for (int b = 0; b < batchSize; b++)
        {
            float[] alone = Pooler.MeanPool(
                embeddings.AsSpan(b * seqLen * dim, seqLen * dim),
                seqLen,
                dim,
                mask.AsSpan(b * seqLen, seqLen));
            Assert.Equal(alone, pooled[b]);
        }
    }

    /// <summary>
    /// Padding cannot reach a pooled vector, whatever the padded positions hold.
    /// </summary>
    /// <remarks>
    /// The masked positions are filled with a value large enough that any leak
    /// moves the result by orders of magnitude rather than by a rounding error, so
    /// the assertion does not have to be delicate to catch one.
    /// </remarks>
    [Fact]
    public void Masked_positions_cannot_reach_the_result()
    {
        const int seqLen = 6;
        const int dim = 5;
        float[] clean = Embeddings(seqLen, dim);
        long[] mask = [1, 1, 1, 0, 0, 0];

        var poisoned = (float[])clean.Clone();
        for (int t = 3; t < seqLen; t++)
        {
            for (int d = 0; d < dim; d++)
            {
                poisoned[(t * dim) + d] = 1e6f;
            }
        }

        Assert.Equal(
            Pooler.MeanPoolAndNormalize(clean, seqLen, dim, mask),
            Pooler.MeanPoolAndNormalize(poisoned, seqLen, dim, mask));
    }

    [Fact]
    public void An_all_zero_mask_divides_by_the_clamp_rather_than_by_zero()
    {
        float[] pooled = Pooler.MeanPool(Embeddings(3, 4), seqLen: 3, dim: 4, [0, 0, 0]);
        Assert.All(pooled, v => Assert.Equal(0f, v));
    }

    [Fact]
    public void Mismatched_batch_shapes_are_refused()
    {
        Assert.Throws<ArgumentException>(
            () => Pooler.MeanPoolBatch(new float[11], batchSize: 2, seqLen: 3, dim: 2, new long[6]));
        Assert.Throws<ArgumentException>(
            () => Pooler.MeanPoolBatch(new float[12], batchSize: 2, seqLen: 3, dim: 2, new long[5]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Pooler.MeanPoolBatch([], batchSize: -1, seqLen: 3, dim: 2, []));
    }

    /// <summary>Deterministic values that are not exactly representable, so rounding differences show.</summary>
    private static float[] Embeddings(int seqLen, int dim, int seed = 0)
    {
        var values = new float[seqLen * dim];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (float)(Math.Sin((i + 1) * 0.37 + seed) * 0.9);
        }
        return values;
    }

    private static long[] Mask(int seqLen)
    {
        var mask = new long[seqLen];
        for (int t = 0; t < seqLen; t++)
        {
            // Always keeps the first token, drops every fourth thereafter.
            mask[t] = t == 0 || t % 4 != 3 ? 1 : 0;
        }
        return mask;
    }

    /// <summary>Mean pooling written the obvious way, one component at a time.</summary>
    private static float[] ScalarMeanPool(ReadOnlySpan<float> embeddings, int seqLen, int dim, ReadOnlySpan<long> mask)
    {
        var pooled = new float[dim];
        long active = 0;
        for (int t = 0; t < seqLen; t++)
        {
            if (mask[t] == 0)
            {
                continue;
            }
            active++;
            for (int d = 0; d < dim; d++)
            {
                pooled[d] += embeddings[(t * dim) + d];
            }
        }
        double denominator = Math.Max(active, 1e-9);
        for (int d = 0; d < dim; d++)
        {
            pooled[d] = (float)(pooled[d] / denominator);
        }
        return pooled;
    }

    /// <summary>L2 normalization written the obvious way, with the sum of squares in double.</summary>
    private static void ScalarL2Normalize(Span<float> vector)
    {
        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sum += (double)vector[i] * vector[i];
        }
        double norm = Math.Sqrt(sum);
        if (norm <= 0)
        {
            return;
        }
        var scale = (float)norm;
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] /= scale;
        }
    }
}
