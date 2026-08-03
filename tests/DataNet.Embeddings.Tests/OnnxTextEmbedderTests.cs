using DataNet.Embeddings.Onnx;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class OnnxTextEmbedderTests
{
    // A 294-byte synthetic encoder committed under tests/oracles: it maps each
    // token to id * W where W = [0.1, 0.2, 0.3, 0.4], so the mean-pooled and
    // L2-normalized sentence embedding is exactly W / ||W|| for any input. This
    // exercises the full ONNX Runtime path (feeding int64 inputs, running the
    // session, extracting [1, seq, dim], pooling) without shipping real weights.
    private static readonly string ModelPath =
        Path.Combine(AppContext.BaseDirectory, "oracles", "tiny_encoder.onnx");

    [Fact]
    public void Embed_runs_model_and_pools()
    {
        using var embedder = new OnnxTextEmbedder(ModelPath);

        long[] ids = [101, 2054, 2003, 102];
        long[] mask = [1, 1, 1, 0];
        float[] embedding = embedder.Embed(ids, mask);

        Assert.Equal(4, embedding.Length);

        // Direction is W normalized; magnitude is unit length.
        double norm = Math.Sqrt(0.01 + 0.04 + 0.09 + 0.16);
        float[] expected =
        [
            (float)(0.1 / norm), (float)(0.2 / norm), (float)(0.3 / norm), (float)(0.4 / norm),
        ];
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(expected[i], embedding[i], 4);
        }

        double sumSq = embedding.Sum(x => (double)x * x);
        Assert.Equal(1.0, sumSq, 4);
    }

    [Fact]
    public void Dimension_is_read_from_model()
    {
        using var embedder = new OnnxTextEmbedder(ModelPath);
        Assert.Equal(4, embedder.Dimension);
    }
}
