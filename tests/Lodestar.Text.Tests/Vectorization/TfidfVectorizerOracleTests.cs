using Lodestar.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lodestar.Text.Vectorization;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Vectorization;

public sealed record TfidfVectorizerCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("config")] public JsonElement Config { get; init; }
    [JsonPropertyName("docs")] public IReadOnlyList<string> Docs { get; init; } = [];
    [JsonPropertyName("feature_names")] public IReadOnlyList<string> FeatureNames { get; init; } = [];
    [JsonPropertyName("idf")] public IReadOnlyList<double>? Idf { get; init; }
    [JsonPropertyName("matrix")] public IReadOnlyList<IReadOnlyList<double>> Matrix { get; init; } = [];
}

public sealed class TfidfVectorizerOracleTests
{
    private const double Tolerance = 1e-9;

    private static readonly OracleFile<TfidfVectorizerCase> Corpus =
        OracleCorpus.Load<TfidfVectorizerCase>("tfidfvectorizer.json");

    [Fact]
    public void Metadata_is_sklearn()
    {
        Assert.Equal("scikit-learn", Corpus.Metadata.Library);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void All_configs_match_sklearn()
    {
        foreach (TfidfVectorizerCase c in Corpus.Cases)
        {
            var tv = new TfidfVectorizer(BuildOptions(c.Config));
            CsrMatrix x = tv.FitTransform(c.Docs);

            Assert.True(
                c.FeatureNames.SequenceEqual(tv.GetFeatureNames()),
                $"case #{c.Id}: feature names differ.");

            if (c.Idf is not null)
            {
                for (int j = 0; j < c.Idf.Count; j++)
                {
                    Assert.True(Math.Abs(c.Idf[j] - tv.Idf[j]) < Tolerance,
                        $"case #{c.Id}: idf[{j}] expected {c.Idf[j]:R}, got {tv.Idf[j]:R}");
                }
            }

            double[,] dense = x.ToDense();
            for (int i = 0; i < c.Matrix.Count; i++)
            {
                for (int j = 0; j < c.FeatureNames.Count; j++)
                {
                    Assert.True(Math.Abs(c.Matrix[i][j] - dense[i, j]) < Tolerance,
                        $"case #{c.Id}: cell [{i},{j}] expected {c.Matrix[i][j]:R}, got {dense[i, j]:R}");
                }
            }
        }
    }

    private static TfidfVectorizerOptions BuildOptions(JsonElement config)
    {
        int ngramMin = config.TryGetProperty("ngram_min", out JsonElement nmin) ? nmin.GetInt32() : 1;
        int ngramMax = config.TryGetProperty("ngram_max", out JsonElement nmax) ? nmax.GetInt32() : 1;

        SparseNorm? norm = SparseNorm.L2;
        if (config.TryGetProperty("norm", out JsonElement nrm))
        {
            norm = nrm.ValueKind == JsonValueKind.Null
                ? null
                : nrm.GetString() switch { "l1" => SparseNorm.L1, "l2" => SparseNorm.L2, _ => (SparseNorm?)null };
        }

        return new TfidfVectorizerOptions
        {
            Count = new CountVectorizerOptions { NgramRange = (ngramMin, ngramMax) },
            Tfidf = new TfidfOptions
            {
                UseIdf = !config.TryGetProperty("use_idf", out JsonElement ui) || ui.GetBoolean(),
                SmoothIdf = !config.TryGetProperty("smooth_idf", out JsonElement si) || si.GetBoolean(),
                SublinearTf = config.TryGetProperty("sublinear_tf", out JsonElement st) && st.GetBoolean(),
                Norm = norm,
            },
        };
    }
}
