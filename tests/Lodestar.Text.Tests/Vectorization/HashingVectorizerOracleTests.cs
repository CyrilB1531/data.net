using Lodestar.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lodestar.Text.Vectorization;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Vectorization;

public sealed record HashingCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("config")] public JsonElement Config { get; init; }
    [JsonPropertyName("docs")] public IReadOnlyList<string> Docs { get; init; } = [];
    [JsonPropertyName("matrix")] public IReadOnlyList<IReadOnlyList<double>> Matrix { get; init; } = [];
}

public sealed record HashingMetadata
{
    [JsonPropertyName("murmur3")] public Dictionary<string, int> Murmur3 { get; init; } = new();
}

public sealed class HashingVectorizerOracleTests
{
    private const double Tolerance = 1e-9;

    private static readonly OracleFile<HashingCase> Corpus =
        OracleCorpus.Load<HashingCase>("hashingvectorizer.json");

    [Fact]
    public void Murmur3_matches_sklearn()
    {
        // The murmur3 map lives in the file metadata; reload it directly for the keys.
        using FileStream fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "oracles", "hashingvectorizer.json"));
        using JsonDocument doc = JsonDocument.Parse(fs);
        JsonElement murmur = doc.RootElement.GetProperty("metadata").GetProperty("murmur3");
        foreach (JsonProperty p in murmur.EnumerateObject())
        {
            int expected = p.Value.GetInt32();
            int actual = MurmurHash3.Hash32(Encoding.UTF8.GetBytes(p.Name));
            Assert.True(expected == actual, $"murmur3(\"{p.Name}\") expected {expected}, got {actual}");
        }
    }

    [Fact]
    public void All_configs_match_sklearn()
    {
        foreach (HashingCase c in Corpus.Cases)
        {
            var hv = new HashingVectorizer(BuildOptions(c.Config));
            CsrMatrix x = hv.Transform(c.Docs);
            double[,] dense = x.ToDense();

            for (int i = 0; i < c.Matrix.Count; i++)
            {
                for (int j = 0; j < c.Matrix[i].Count; j++)
                {
                    Assert.True(Math.Abs(c.Matrix[i][j] - dense[i, j]) < Tolerance,
                        $"case #{c.Id}: cell [{i},{j}] expected {c.Matrix[i][j]:R}, got {dense[i, j]:R}");
                }
            }
        }
    }

    private static HashingVectorizerOptions BuildOptions(JsonElement config)
    {
        int ngramMin = config.TryGetProperty("ngram_min", out JsonElement nmin) ? nmin.GetInt32() : 1;
        int ngramMax = config.TryGetProperty("ngram_max", out JsonElement nmax) ? nmax.GetInt32() : 1;

        SparseNorm? norm = SparseNorm.L2;
        if (config.TryGetProperty("norm", out JsonElement nrm))
        {
            norm = nrm.ValueKind == JsonValueKind.Null ? null
                : nrm.GetString() switch { "l1" => SparseNorm.L1, _ => SparseNorm.L2 };
        }

        return new HashingVectorizerOptions
        {
            Count = new CountVectorizerOptions { NgramRange = (ngramMin, ngramMax) },
            NumFeatures = config.GetProperty("n_features").GetInt32(),
            AlternateSign = !config.TryGetProperty("alternate_sign", out JsonElement asg) || asg.GetBoolean(),
            Norm = norm,
        };
    }
}
