using DataNet.Embeddings.Onnx;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class OnnxTextEmbedderTests
{
    // tiny_encoder.onnx maps every token to a multiple of one fixed direction (0.1, 0.2, 0.3, 0.4), so
    // mean-pooling and L2-normalizing any input returns that direction -- exercising the full ONNX Runtime path.
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
