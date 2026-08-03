using System.Text.Json;
using DataNet.Embeddings.Search;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class EmbeddingIndexTests
{
    private const float Tolerance = 1e-4f;

    [Fact]
    public void Search_matches_bruteforce_reference()
    {
        using JsonDocument doc = OracleLoader.Load("knn.json");
        JsonElement root = doc.RootElement;
        int dim = root.GetProperty("metadata").GetProperty("dim").GetInt32();

        var index = new EmbeddingIndex(dim);
        foreach (JsonElement row in root.GetProperty("corpus").EnumerateArray())
        {
            float[] v = row.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
            index.Add(v);
        }

        foreach (JsonElement c in root.GetProperty("cases").EnumerateArray())
        {
            float[] query = c.GetProperty("query").EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
            int k = c.GetProperty("k").GetInt32();
            JsonElement expected = c.GetProperty("results");

            IReadOnlyList<SearchResult> actual = index.Search(query, k);

            Assert.Equal(expected.GetArrayLength(), actual.Count);
            int r = 0;
            foreach (JsonElement e in expected.EnumerateArray())
            {
                int expIndex = e.GetProperty("index").GetInt32();
                double expScore = e.GetProperty("score").GetDouble();
                Assert.True(expIndex == actual[r].Index,
                    $"case #{c.GetProperty("id").GetInt32()} rank {r}: expected index {expIndex}, got {actual[r].Index}");
                Assert.True(Math.Abs(expScore - actual[r].Score) < Tolerance,
                    $"case #{c.GetProperty("id").GetInt32()} rank {r}: score expected {expScore:R}, got {actual[r].Score:R}");
                r++;
            }
        }
    }

    [Fact]
    public void Dot_matches_scalar()
    {
        float[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        float[] b = [9, 8, 7, 6, 5, 4, 3, 2, 1];
        float expected = 0;
        for (int i = 0; i < a.Length; i++)
        {
            expected += a[i] * b[i];
        }
        Assert.Equal(expected, VectorMath.Dot(a, b), 3);
    }

    [Fact]
    public void Identical_vector_scores_one()
    {
        var index = new EmbeddingIndex(3);
        index.Add([1f, 2f, 2f]);
        IReadOnlyList<SearchResult> hits = index.Search([1f, 2f, 2f], 1);
        Assert.Equal(0, hits[0].Index);
        Assert.Equal(1f, hits[0].Score, 5);
    }
}
