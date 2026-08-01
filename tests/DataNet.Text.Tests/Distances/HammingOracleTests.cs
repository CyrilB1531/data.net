using DataNet.Text.Distances;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Distances;

public sealed class HammingOracleTests
{
    private static readonly OracleFile<EditDistanceCase> Corpus =
        OracleCorpus.Load<EditDistanceCase>("hamming.json");

    [Fact]
    public void Metadata_is_standard_hamming()
    {
        // Standard code-point Hamming; jellyfish matches for normal inputs but has
        // a documented quirk on combining marks (decision 0005). The InlineData
        // below are jellyfish's own values, anchoring parity for sane inputs.
        Assert.Equal("reference-standard", Corpus.Metadata.Library);
        Assert.Equal("code_point", Corpus.Metadata.Semantics);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void Distance_matches_jellyfish()
    {
        OracleAsserts.ExactInt(Corpus.Cases,
            c => c.Distance,
            c => Hamming.Distance(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id} {c.Category}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Theory]
    [InlineData("abc", "abd", 1)]
    [InlineData("abc", "ab", 1)]     // extra char counts as a mismatch
    [InlineData("a", "abc", 2)]      // length difference counts
    [InlineData("abcd", "axcy", 2)]
    [InlineData("", "", 0)]
    public void Distance_known_values(string a, string b, int expected)
    {
        Assert.Equal(expected, Hamming.Distance(a, b));
    }

    [Fact]
    public void Distance_is_symmetric()
    {
        Assert.Equal(Hamming.Distance("abcd", "ax"), Hamming.Distance("ax", "abcd"));
    }
}
