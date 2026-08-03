namespace DataNet.Embeddings.Pooling;

/// <summary>
/// Turns per-token model outputs into a single sentence embedding.
/// </summary>
/// <remarks>
/// Mean pooling with an attention mask, then L2 normalization, is the standard
/// recipe used by sentence-transformers: the masked token vectors are averaged
/// (padding tokens excluded) and the result is scaled to unit length so cosine
/// similarity reduces to a dot product.
/// </remarks>
public static class Pooler
{
    /// <summary>
    /// Mean-pools token embeddings using an attention mask.
    /// </summary>
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
        long active = 0;
        for (int t = 0; t < seqLen; t++)
        {
            if (attentionMask[t] == 0)
            {
                continue;
            }
            active++;
            int offset = t * dim;
            for (int d = 0; d < dim; d++)
            {
                pooled[d] += tokenEmbeddings[offset + d];
            }
        }

        // sentence-transformers clamps the denominator to avoid division by zero.
        double denom = Math.Max(active, 1e-9);
        for (int d = 0; d < dim; d++)
        {
            pooled[d] = (float)(pooled[d] / denom);
        }
        return pooled;
    }

    /// <summary>Scales <paramref name="vector"/> in place to unit L2 norm (no-op for a zero vector).</summary>
    public static void L2Normalize(Span<float> vector)
    {
        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sum += (double)vector[i] * vector[i];
        }
        double norm = Math.Sqrt(sum);
        if (norm == 0)
        {
            return;
        }
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / norm);
        }
    }

    /// <summary>Mean-pools then L2-normalizes — the full sentence-embedding recipe.</summary>
    public static float[] MeanPoolAndNormalize(ReadOnlySpan<float> tokenEmbeddings, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
    {
        float[] pooled = MeanPool(tokenEmbeddings, seqLen, dim, attentionMask);
        L2Normalize(pooled);
        return pooled;
    }
}
