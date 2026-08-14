using System.Numerics;

namespace DataNet.Embeddings.Pooling;

/// <summary>
/// Turns per-token model outputs into a single sentence embedding.
/// </summary>
/// <remarks>
/// Masked mean (padding excluded) then L2 normalization — sentence-transformers'
/// own recipe; see <c>docs/equivalence.md</c>'s pooling rows for the bit-identical
/// guarantee between the <see cref="Vector{T}"/> path and the
/// <c>netstandard2.0</c> scalar one.
/// </remarks>
public static class Pooler
{
    /// <summary>
    /// Mean-pools token embeddings using an attention mask.
    /// </summary>
    /// <remarks>
    /// Matches the <c>mean_pooling</c> function of the sentence-transformers
    /// README: <c>sum(token_embeddings * mask) / clamp(sum(mask), min=1e-9)</c>.
    /// </remarks>
    /// <param name="tokenEmbeddings">Row-major <c>[seqLen × dim]</c> token embeddings.</param>
    /// <param name="seqLen">Number of tokens.</param>
    /// <param name="dim">Embedding dimension.</param>
    /// <param name="attentionMask">Length <paramref name="seqLen"/>; non-zero marks a real token.</param>
    /// <returns>The pooled <c>dim</c>-length vector.</returns>
    public static float[] MeanPool(ReadOnlySpan<float> tokenEmbeddings, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
    {
        if (tokenEmbeddings.Length != seqLen * dim)
        {
            throw new ArgumentException($"tokenEmbeddings length {tokenEmbeddings.Length} != seqLen*dim {seqLen * dim}.", nameof(tokenEmbeddings));
        }
        if (attentionMask.Length != seqLen)
        {
            throw new ArgumentException($"attentionMask length {attentionMask.Length} != seqLen {seqLen}.", nameof(attentionMask));
        }

        var pooled = new float[dim];
        PoolInto(pooled, tokenEmbeddings, seqLen, dim, attentionMask);
        return pooled;
    }

    /// <summary>
    /// Mean-pools a whole batch of <c>[batchSize × seqLen × dim]</c> token
    /// embeddings, one pooled vector per sequence.
    /// </summary>
    /// <remarks>
    /// The batched form of <see cref="MeanPool(ReadOnlySpan{float}, int, int, ReadOnlySpan{long})"/>,
    /// over the tensor an encoder returns for a padded batch. Each row is pooled
    /// against its own slice of the mask, so the padding a shorter sequence carries
    /// cannot reach its vector.
    /// </remarks>
    /// <param name="tokenEmbeddings">Row-major <c>[batchSize × seqLen × dim]</c>.</param>
    /// <param name="batchSize">Number of sequences.</param>
    /// <param name="seqLen">Padded length of every sequence.</param>
    /// <param name="dim">Embedding dimension.</param>
    /// <param name="attentionMask">Row-major <c>[batchSize × seqLen]</c>.</param>
    public static float[][] MeanPoolBatch(ReadOnlySpan<float> tokenEmbeddings, int batchSize, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
    {
        ValidateBatch(tokenEmbeddings, batchSize, seqLen, dim, attentionMask);

        var pooled = new float[batchSize][];
        for (int b = 0; b < batchSize; b++)
        {
            var vector = new float[dim];
            PoolInto(
                vector,
                tokenEmbeddings.Slice(b * seqLen * dim, seqLen * dim),
                seqLen,
                dim,
                attentionMask.Slice(b * seqLen, seqLen));
            pooled[b] = vector;
        }
        return pooled;
    }

    /// <summary>Scales <paramref name="vector"/> in place to unit L2 norm (no-op for a zero vector).</summary>
    /// <remarks>
    /// Matches <c>torch.nn.functional.normalize(v, p=2, dim=1)</c>. The sum of
    /// squares is accumulated in <see cref="double"/> and deliberately not
    /// vectorized: a <c>Vector&lt;float&gt;</c> accumulator would both lose
    /// precision and make the result depend on the SIMD width of the machine that
    /// ran it. The scaling pass, which is exact whichever way it is done, is
    /// vectorized.
    /// </remarks>
    public static void L2Normalize(Span<float> vector)
    {
        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sum += (double)vector[i] * vector[i];
        }
        double norm = Math.Sqrt(sum);

        // SonarLint S1244: exact zero is the only norm that makes the division below
        // undefined; a tolerance compare would leave short vectors unnormalized instead.
#pragma warning disable S1244
        if (norm == 0)
#pragma warning restore S1244
        {
            return;
        }

        var scale = (float)norm;
        int j = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && vector.Length >= Vector<float>.Count)
        {
            int width = Vector<float>.Count;
            var divisor = new Vector<float>(scale);
            for (; j <= vector.Length - width; j += width)
            {
                Span<float> slice = vector.Slice(j, width);
                (new Vector<float>(slice) / divisor).CopyTo(slice);
            }
        }
#endif
        for (; j < vector.Length; j++)
        {
            vector[j] /= scale;
        }
    }

    /// <summary>Mean-pools then L2-normalizes — the full sentence-embedding recipe.</summary>
    /// <param name="tokenEmbeddings">Row-major <c>[seqLen × dim]</c> token embeddings.</param>
    /// <param name="seqLen">Number of tokens.</param>
    /// <param name="dim">Embedding dimension.</param>
    /// <param name="attentionMask">Length <paramref name="seqLen"/>; non-zero marks a real token.</param>
    public static float[] MeanPoolAndNormalize(ReadOnlySpan<float> tokenEmbeddings, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
    {
        float[] pooled = MeanPool(tokenEmbeddings, seqLen, dim, attentionMask);
        L2Normalize(pooled);
        return pooled;
    }

    /// <summary>Mean-pools then L2-normalizes every sequence of a padded batch.</summary>
    /// <remarks>
    /// The batched form of
    /// <see cref="MeanPoolAndNormalize(ReadOnlySpan{float}, int, int, ReadOnlySpan{long})"/>,
    /// matching what sentence-transformers' <c>encode</c> does to one forward pass.
    /// </remarks>
    /// <param name="tokenEmbeddings">Row-major <c>[batchSize × seqLen × dim]</c>.</param>
    /// <param name="batchSize">Number of sequences.</param>
    /// <param name="seqLen">Padded length of every sequence.</param>
    /// <param name="dim">Embedding dimension.</param>
    /// <param name="attentionMask">Row-major <c>[batchSize × seqLen]</c>.</param>
    public static float[][] MeanPoolAndNormalizeBatch(ReadOnlySpan<float> tokenEmbeddings, int batchSize, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
    {
        float[][] pooled = MeanPoolBatch(tokenEmbeddings, batchSize, seqLen, dim, attentionMask);
        foreach (float[] vector in pooled)
        {
            L2Normalize(vector);
        }
        return pooled;
    }

    private static void ValidateBatch(ReadOnlySpan<float> tokenEmbeddings, int batchSize, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
    {
        if (batchSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "batchSize must not be negative.");
        }
        if (tokenEmbeddings.Length != batchSize * seqLen * dim)
        {
            throw new ArgumentException(
                $"tokenEmbeddings length {tokenEmbeddings.Length} != batchSize*seqLen*dim {batchSize * seqLen * dim}.",
                nameof(tokenEmbeddings));
        }
        if (attentionMask.Length != batchSize * seqLen)
        {
            throw new ArgumentException(
                $"attentionMask length {attentionMask.Length} != batchSize*seqLen {batchSize * seqLen}.",
                nameof(attentionMask));
        }
    }

    /// <summary>Accumulates the masked tokens of one sequence into <paramref name="pooled"/> and divides.</summary>
    private static void PoolInto(Span<float> pooled, ReadOnlySpan<float> tokenEmbeddings, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
    {
        long active = 0;
        for (int t = 0; t < seqLen; t++)
        {
            if (attentionMask[t] == 0)
            {
                continue;
            }
            active++;
            Add(pooled, tokenEmbeddings.Slice(t * dim, dim));
        }

        // sentence-transformers clamps the denominator to avoid division by zero.
        double denom = Math.Max(active, 1e-9);
        for (int d = 0; d < dim; d++)
        {
            pooled[d] = (float)(pooled[d] / denom);
        }
    }

    /// <summary>Adds <paramref name="row"/> into <paramref name="accumulator"/>, component by component.</summary>
    private static void Add(Span<float> accumulator, ReadOnlySpan<float> row)
    {
        int d = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && accumulator.Length >= Vector<float>.Count)
        {
            int width = Vector<float>.Count;
            for (; d <= accumulator.Length - width; d += width)
            {
                Span<float> slice = accumulator.Slice(d, width);
                (new Vector<float>(slice) + new Vector<float>(row.Slice(d, width))).CopyTo(slice);
            }
        }
#endif
        for (; d < accumulator.Length; d++)
        {
            accumulator[d] += row[d];
        }
    }
}
