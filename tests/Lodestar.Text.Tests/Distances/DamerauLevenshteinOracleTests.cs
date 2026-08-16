using Lodestar.Text.Distances;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Distances;

public sealed class DamerauLevenshteinOracleTests
{
    private static readonly OracleFile<EditDistanceCase> Corpus =
        OracleCorpus.Load<EditDistanceCase>("damerau.json");

    [Fact]
    public void Metadata_is_rapidfuzz_damerau()
    {
        Assert.Equal("rapidfuzz", Corpus.Metadata.Library);
        Assert.Equal("code_point", Corpus.Metadata.Semantics);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void Distance_matches_rapidfuzz()
    {
        OracleAsserts.ExactInt(Corpus.Cases,
            c => c.Distance,
            c => DamerauLevenshtein.Distance(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id} {c.Category}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void NormalizedDistance_matches_rapidfuzz()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.NormalizedDistance,
            c => DamerauLevenshtein.NormalizedDistance(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void NormalizedSimilarity_matches_rapidfuzz()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.NormalizedSimilarity,
            c => DamerauLevenshtein.NormalizedSimilarity(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Theory]
    [InlineData("CA", "ABC", 2)]     // unrestricted: transpose CA->AC then insert B (OSA needs 3)
    [InlineData("ca", "abc", 2)]
    [InlineData("ab", "ba", 1)]
    [InlineData("kitten", "sitting", 3)]
    public void Distance_known_values(string a, string b, int expected)
    {
        Assert.Equal(expected, DamerauLevenshtein.Distance(a, b));
    }

    [Fact]
    public void Differs_from_osa_on_reused_substring()
    {
        Assert.Equal(3, Osa.Distance("CA", "ABC"));
        Assert.Equal(2, DamerauLevenshtein.Distance("CA", "ABC"));
    }
}
