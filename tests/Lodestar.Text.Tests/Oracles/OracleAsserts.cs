using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Xunit;

namespace Lodestar.Text.Tests.Oracles;

/// <summary>
/// A reference case shaped like an edit-distance oracle entry: two operands and
/// the distance plus its normalized forms. Reused across Levenshtein / OSA /
/// Damerau-Levenshtein / Hamming corpora (unused numeric fields stay at 0).
/// </summary>
public sealed record EditDistanceCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("a")] public string A { get; init; } = "";
    [JsonPropertyName("b")] public string B { get; init; } = "";
    [JsonPropertyName("distance")] public int Distance { get; init; }
    [JsonPropertyName("normalized_distance")] public double NormalizedDistance { get; init; }
    [JsonPropertyName("normalized_similarity")] public double NormalizedSimilarity { get; init; }
}

/// <summary>A reference case for a similarity metric: two operands and a similarity in [0, 1].</summary>
public sealed record SimilarityCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("a")] public string A { get; init; } = "";
    [JsonPropertyName("b")] public string B { get; init; } = "";
    [JsonPropertyName("similarity")] public double Similarity { get; init; }
}

/// <summary>A reference case carrying the five set-similarity values at qval=1.</summary>
public sealed record SetSimilarityCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("a")] public string A { get; init; } = "";
    [JsonPropertyName("b")] public string B { get; init; } = "";
    [JsonPropertyName("jaccard")] public double Jaccard { get; init; }
    [JsonPropertyName("dice")] public double Dice { get; init; }
    [JsonPropertyName("overlap")] public double Overlap { get; init; }
    [JsonPropertyName("tversky")] public double Tversky { get; init; }
    [JsonPropertyName("cosine")] public double Cosine { get; init; }
}

/// <summary>A reference case for LCS: two operands, subsequence and substring lengths.</summary>
public sealed record LcsCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("a")] public string A { get; init; } = "";
    [JsonPropertyName("b")] public string B { get; init; } = "";
    [JsonPropertyName("subsequence")] public int Subsequence { get; init; }
    [JsonPropertyName("substring")] public int Substring { get; init; }
}

/// <summary>A reference case for a phonetic encoder: a word and its jellyfish codes.</summary>
public sealed record PhoneticCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("word")] public string Word { get; init; } = "";
    [JsonPropertyName("soundex")] public string Soundex { get; init; } = "";
    [JsonPropertyName("metaphone")] public string Metaphone { get; init; } = "";
    [JsonPropertyName("nysiis")] public string Nysiis { get; init; } = "";
}

/// <summary>Aggregating oracle assertions: report every mismatch, not just the first.</summary>
public static class OracleAsserts
{
    private const double DefaultTolerance = 1e-9;
    private const int ReportCap = 4000;

    /// <summary>Asserts a string computation matches the oracle exactly for every case.</summary>
    public static void ExactString<T>(
        IReadOnlyList<T> cases,
        Func<T, string> expected,
        Func<T, string> actual,
        Func<T, string> describe)
    {
        var failures = new StringBuilder();
        foreach (T c in cases)
        {
            string e = expected(c);
            string a = actual(c);
            if (!string.Equals(e, a, StringComparison.Ordinal) && failures.Length < ReportCap)
            {
                failures.Append(CultureInfo.InvariantCulture, $"  {describe(c)}: expected \"{e}\", got \"{a}\"\n");
            }
        }

        Assert.True(failures.Length == 0, $"{cases.Count} cases checked; mismatches:\n{failures}");
    }

    /// <summary>Asserts an integer computation matches the oracle exactly for every case.</summary>
    public static void ExactInt<T>(
        IReadOnlyList<T> cases,
        Func<T, int> expected,
        Func<T, int> actual,
        Func<T, string> describe)
    {
        var failures = new StringBuilder();
        foreach (T c in cases)
        {
            int e = expected(c);
            int a = actual(c);
            if (e != a && failures.Length < ReportCap)
            {
                failures.Append(CultureInfo.InvariantCulture, $"  {describe(c)}: expected {e}, got {a}\n");
            }
        }

        Assert.True(failures.Length == 0, $"{cases.Count} cases checked; mismatches:\n{failures}");
    }

    /// <summary>Asserts a floating computation matches the oracle within tolerance for every case.</summary>
    public static void Approx<T>(
        IReadOnlyList<T> cases,
        Func<T, double> expected,
        Func<T, double> actual,
        Func<T, string> describe,
        double tolerance = DefaultTolerance)
    {
        var failures = new StringBuilder();
        foreach (T c in cases)
        {
            double e = expected(c);
            double a = actual(c);
            if (Math.Abs(e - a) > tolerance && failures.Length < ReportCap)
            {
                failures.Append(CultureInfo.InvariantCulture, $"  {describe(c)}: expected {e:R}, got {a:R}\n");
            }
        }

        Assert.True(failures.Length == 0, $"{cases.Count} cases checked; mismatches:\n{failures}");
    }

    /// <summary>Renders a string with non-ASCII characters escaped, for readable failure output.</summary>
    public static string Escape(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char ch in s)
        {
            sb.Append(char.IsControl(ch) || ch > 0x7E ? $"\\u{(int)ch:X4}" : ch.ToString());
        }
        return sb.Append('"').ToString();
    }
}

/// <summary>One frozen radius query: the corpus, the query, the radius, and the hits a
/// linear scan returns.</summary>
public sealed record BkTreeCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("corpus")] public IReadOnlyList<string> Corpus { get; init; } = [];
    [JsonPropertyName("query")] public string Query { get; init; } = "";
    [JsonPropertyName("radius")] public int Radius { get; init; }
    [JsonPropertyName("hits")] public IReadOnlyList<BkTreeHit> Hits { get; init; } = [];
}

/// <summary>One expected hit inside a <see cref="BkTreeCase"/>.</summary>
public sealed record BkTreeHit
{
    [JsonPropertyName("item")] public string Item { get; init; } = "";
    [JsonPropertyName("distance")] public int Distance { get; init; }
}
