using System.Text.Json;
using System.Text.Json.Serialization;
using DataNet.Text.Vectorization;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Vectorization;

public sealed record CountVectorizerCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("config")] public JsonElement Config { get; init; }
    [JsonPropertyName("docs")] public IReadOnlyList<string> Docs { get; init; } = [];
    [JsonPropertyName("feature_names")] public IReadOnlyList<string> FeatureNames { get; init; } = [];
    [JsonPropertyName("matrix")] public IReadOnlyList<IReadOnlyList<int>> Matrix { get; init; } = [];
}

public sealed class CountVectorizerOracleTests
{
    private static readonly OracleFile<CountVectorizerCase> Corpus =
        OracleCorpus.Load<CountVectorizerCase>("countvectorizer.json");

    [Fact]
    public void Metadata_is_sklearn()
    {
        Assert.Equal("scikit-learn", Corpus.Metadata.Library);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void All_configs_match_sklearn()
    {
        foreach (CountVectorizerCase c in Corpus.Cases)
        {
            var cv = new CountVectorizer(BuildOptions(c.Config));
            CsrMatrix x = cv.FitTransform(c.Docs);

            Assert.True(
                c.FeatureNames.SequenceEqual(cv.GetFeatureNames()),
                $"case #{c.Id}: feature names differ.\n  expected: [{string.Join(", ", c.FeatureNames)}]\n  actual:   [{string.Join(", ", cv.GetFeatureNames())}]");

            double[,] dense = x.ToDense();
            for (int i = 0; i < c.Matrix.Count; i++)
            {
                for (int j = 0; j < c.FeatureNames.Count; j++)
                {
                    Assert.True(
                        c.Matrix[i][j] == (int)dense[i, j],
                        $"case #{c.Id}: cell [{i},{j}] expected {c.Matrix[i][j]}, got {(int)dense[i, j]}");
                }
            }
        }
    }

    private static CountVectorizerOptions BuildOptions(JsonElement config)
    {
        AnalyzerKind analyzer = config.TryGetProperty("analyzer", out JsonElement a)
            ? a.GetString() switch
            {
                "char" => AnalyzerKind.Char,
                "char_wb" => AnalyzerKind.CharWordBoundary,
                _ => AnalyzerKind.Word,
            }
            : AnalyzerKind.Word;

        int ngramMin = config.TryGetProperty("ngram_min", out JsonElement nmin) ? nmin.GetInt32() : 1;
        int ngramMax = config.TryGetProperty("ngram_max", out JsonElement nmax) ? nmax.GetInt32() : 1;

        IReadOnlyCollection<string>? stopWords = null;
        if (config.TryGetProperty("stop_words", out JsonElement sw))
        {
            stopWords = sw.ValueKind switch
            {
                JsonValueKind.Array => sw.EnumerateArray().Select(e => e.GetString()!).ToArray(),
                JsonValueKind.String when sw.GetString() == "english" => StopWords.English,
                _ => null,
            };
        }

        return new CountVectorizerOptions
        {
            Analyzer = analyzer,
            NgramRange = (ngramMin, ngramMax),
            MinDf = config.TryGetProperty("min_df", out JsonElement mdf) ? mdf.GetDouble() : 1.0,
            MaxDf = config.TryGetProperty("max_df", out JsonElement xdf) ? xdf.GetDouble() : 1.0,
            Binary = config.TryGetProperty("binary", out JsonElement b) && b.GetBoolean(),
            Lowercase = !config.TryGetProperty("lowercase", out JsonElement lc) || lc.GetBoolean(),
            StripAccents = config.TryGetProperty("strip_accents", out JsonElement sa) && sa.GetBoolean(),
            StopWords = stopWords,
        };
    }
}
