using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataNet.Text.Tests.Oracles;

/// <summary>Loads a frozen oracle corpus committed under <c>tests/oracles/</c>.</summary>
/// <remarks>
/// The JSON is captured from the canonical Python library (see the corpus
/// metadata) by <c>tools/generate_oracles.py</c>. These files are the ground
/// truth the C# implementation is validated against (§4 of the brief).
/// </remarks>
public static class OracleCorpus
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static OracleFile<T> Load<T>(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Oracle corpus '{fileName}' not found at '{path}'. " +
                "Run tools/generate_oracles.py to (re)generate it.",
                path);
        }

        using FileStream stream = File.OpenRead(path);
        OracleFile<T>? file = JsonSerializer.Deserialize<OracleFile<T>>(stream, Options);
        return file ?? throw new InvalidDataException($"Oracle corpus '{fileName}' deserialized to null.");
    }
}

/// <summary>Top-level shape of an oracle file: metadata plus a list of cases.</summary>
public sealed record OracleFile<TCase>
{
    [JsonPropertyName("metadata")]
    public OracleMetadata Metadata { get; init; } = new();

    [JsonPropertyName("cases")]
    public IReadOnlyList<TCase> Cases { get; init; } = [];
}

public sealed record OracleMetadata
{
    [JsonPropertyName("algorithm")] public string Algorithm { get; init; } = "";
    [JsonPropertyName("library")] public string Library { get; init; } = "";
    [JsonPropertyName("library_version")] public string LibraryVersion { get; init; } = "";
    [JsonPropertyName("semantics")] public string Semantics { get; init; } = "";
    [JsonPropertyName("count")] public int Count { get; init; }
}

/// <summary>A single Levenshtein reference case.</summary>
public sealed record LevenshteinCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("a")] public string A { get; init; } = "";
    [JsonPropertyName("b")] public string B { get; init; } = "";
    [JsonPropertyName("distance")] public int Distance { get; init; }
    [JsonPropertyName("normalized_distance")] public double NormalizedDistance { get; init; }
    [JsonPropertyName("normalized_similarity")] public double NormalizedSimilarity { get; init; }
}
