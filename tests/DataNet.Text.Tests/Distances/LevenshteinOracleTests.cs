using System.Globalization;
using System.Text;
using DataNet.Text.Distances;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Distances;

/// <summary>
/// Replays the frozen rapidfuzz corpus against the C# implementation. rapidfuzz
/// works on code points, so parity is asserted with <see cref="TextElement.CodePoint"/>.
/// </summary>
public sealed class LevenshteinOracleTests
{
    private const double Tolerance = 1e-9;

    private static readonly OracleFile<LevenshteinCase> Corpus =
        OracleCorpus.Load<LevenshteinCase>("levenshtein.json");

    [Fact]
    public void Corpus_is_loaded_and_nonempty()
    {
        Assert.Equal("rapidfuzz", Corpus.Metadata.Library);
        Assert.Equal("code_point", Corpus.Metadata.Semantics);
        Assert.NotEmpty(Corpus.Cases);
        Assert.Equal(Corpus.Metadata.Count, Corpus.Cases.Count);
    }

    [Fact]
    public void Distance_matches_rapidfuzz_exactly()
    {
        var failures = new StringBuilder();
        int examined = 0;

        foreach (LevenshteinCase c in Corpus.Cases)
        {
            int actual = Levenshtein.Distance(c.A, c.B, TextElement.CodePoint);
            examined++;
            if (actual != c.Distance)
            {
                Append(failures, c, $"distance expected {c.Distance}, got {actual}");
            }
        }

        AssertNoFailures(failures, examined);
    }

    [Fact]
    public void Distance_default_utf16_matches_rapidfuzz_for_bmp_cases()
    {
        // For strings confined to the Basic Multilingual Plane, a UTF-16 code unit
        // *is* a code point, so the default (Utf16Unit) path — which routes through
        // the Myers bit-parallel fast path for short Latin-1 operands — must equal
        // the rapidfuzz value. This is what exercises Myers against the corpus.
        var failures = new StringBuilder();
        int examined = 0;

        foreach (LevenshteinCase c in Corpus.Cases)
        {
            if (ContainsSurrogate(c.A) || ContainsSurrogate(c.B))
            {
                continue; // supplementary plane: UTF-16 and code points diverge by design
            }

            int actual = Levenshtein.Distance(c.A, c.B); // default = Utf16Unit
            examined++;
            if (actual != c.Distance)
            {
                Append(failures, c, $"utf16 distance expected {c.Distance}, got {actual}");
            }
        }

        Assert.True(examined > 500, $"expected many BMP cases, only checked {examined}");
        AssertNoFailures(failures, examined);
    }

    private static bool ContainsSurrogate(string s)
    {
        foreach (char ch in s)
        {
            if (char.IsSurrogate(ch))
            {
                return true;
            }
        }
        return false;
    }

    [Fact]
    public void NormalizedDistance_matches_rapidfuzz_within_tolerance()
    {
        var failures = new StringBuilder();
        int examined = 0;

        foreach (LevenshteinCase c in Corpus.Cases)
        {
            double actual = Levenshtein.NormalizedDistance(c.A, c.B, TextElement.CodePoint);
            examined++;
            if (Math.Abs(actual - c.NormalizedDistance) > Tolerance)
            {
                Append(failures, c, $"normalized_distance expected {c.NormalizedDistance:R}, got {actual:R}");
            }
        }

        AssertNoFailures(failures, examined);
    }

    [Fact]
    public void NormalizedSimilarity_matches_rapidfuzz_within_tolerance()
    {
        var failures = new StringBuilder();
        int examined = 0;

        foreach (LevenshteinCase c in Corpus.Cases)
        {
            double actual = Levenshtein.NormalizedSimilarity(c.A, c.B, TextElement.CodePoint);
            examined++;
            if (Math.Abs(actual - c.NormalizedSimilarity) > Tolerance)
            {
                Append(failures, c, $"normalized_similarity expected {c.NormalizedSimilarity:R}, got {actual:R}");
            }
        }

        AssertNoFailures(failures, examined);
    }

    private static void Append(StringBuilder sb, LevenshteinCase c, string message)
    {
        if (sb.Length < 4000) // cap the report; first handful is enough to diagnose
        {
            sb.Append(CultureInfo.InvariantCulture, $"  [#{c.Id} {c.Category}] a={Escape(c.A)} b={Escape(c.B)}: {message}\n");
        }
    }

    private static void AssertNoFailures(StringBuilder failures, int examined)
    {
        Assert.True(
            failures.Length == 0,
            $"{examined} cases checked; mismatches:\n{failures}");
    }

    private static string Escape(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char ch in s)
        {
            sb.Append(char.IsControl(ch) || ch > 0x7E ? $"\\u{(int)ch:X4}" : ch.ToString());
        }
        return sb.Append('"').ToString();
    }
}
