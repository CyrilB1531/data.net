using System.Text.Json;
using DataNet.Embeddings.Pooling;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class PoolingTests
{
    private const double Tolerance = 1e-5;

    [Fact]
    public void MeanPoolAndNormalize_matches_reference()
    {
        using JsonDocument doc = OracleLoader.Load("pooling.json");
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int seq = c.GetProperty("seq").GetInt32();
            int dim = c.GetProperty("dim").GetInt32();

            var flat = new float[seq * dim];
            int k = 0;
            foreach (JsonElement row in c.GetProperty("embeddings").EnumerateArray())
            {
                foreach (JsonElement v in row.EnumerateArray())
                {
                    flat[k++] = (float)v.GetDouble();
                }
            }

            long[] mask = c.GetProperty("mask").EnumerateArray().Select(e => (long)e.GetInt32()).ToArray();
            double[] expected = c.GetProperty("pooled_normalized").EnumerateArray().Select(e => e.GetDouble()).ToArray();

            float[] actual = Pooler.MeanPoolAndNormalize(flat, seq, dim, mask);
            for (int d = 0; d < dim; d++)
            {
                Assert.True(Math.Abs(expected[d] - actual[d]) < Tolerance,
                    $"case #{c.GetProperty("id").GetInt32()} dim {d}: expected {expected[d]:R}, got {actual[d]:R}");
            }
        }
    }

    [Fact]
    public void L2Normalize_gives_unit_length()
    {
        float[] v = [3f, 4f];
        Pooler.L2Normalize(v);
        Assert.Equal(0.6f, v[0], 5);
        Assert.Equal(0.8f, v[1], 5);
    }

    [Fact]
    public void Masked_tokens_are_excluded()
    {
        // Two tokens, second is padding: pooled == first token (then normalized).
        float[] emb = [1f, 0f, 0f, 999f, 999f, 999f];
        long[] mask = [1, 0];
        float[] pooled = Pooler.MeanPool(emb, seqLen: 2, dim: 3, mask);
        Assert.Equal(1f, pooled[0], 5);
        Assert.Equal(0f, pooled[1], 5);
        Assert.Equal(0f, pooled[2], 5);
    }
}
