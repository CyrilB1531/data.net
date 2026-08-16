using System.Text.Json;
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests;

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

    [Fact]
    public void An_index_without_ids_reports_none()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f]);

        Assert.False(index.HasIds);
        Assert.Null(index.GetId(0));
    }

    [Fact]
    public void An_id_is_recalled_by_position()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], "doc-1");
        index.Add([0f, 1f], "documento-café");

        Assert.True(index.HasIds);
        Assert.Equal("doc-1", index.GetId(0));
        Assert.Equal("documento-café", index.GetId(1));
    }

    [Fact]
    public void A_null_id_is_the_same_as_no_id_at_all()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], null);

        Assert.False(index.HasIds);
        Assert.Null(index.GetId(0));
    }

    [Fact]
    public void Ids_and_anonymous_vectors_mix_in_one_index()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f]);
        index.Add([0f, 1f], "named");
        index.Add([1f, 1f]);

        Assert.True(index.HasIds);
        Assert.Null(index.GetId(0));
        Assert.Equal("named", index.GetId(1));
        Assert.Null(index.GetId(2));
    }

    [Fact]
    public void An_empty_id_is_kept_as_an_id()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], string.Empty);

        Assert.True(index.HasIds);
        Assert.Equal(string.Empty, index.GetId(0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void GetId_outside_the_index_is_rejected(int position)
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], "only");

        Assert.Throws<ArgumentOutOfRangeException>(() => index.GetId(position));
    }
}
