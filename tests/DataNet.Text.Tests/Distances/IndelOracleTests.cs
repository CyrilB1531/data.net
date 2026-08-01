using DataNet.Text.Distances;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Distances;

public sealed class IndelOracleTests
{
    private static readonly OracleFile<EditDistanceCase> Corpus =
        OracleCorpus.Load<EditDistanceCase>("indel.json");

    [Fact]
    public void Metadata_is_rapidfuzz_indel()
    {
        Assert.Equal("rapidfuzz", Corpus.Metadata.Library);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void Distance_matches_rapidfuzz()
    {
        OracleAsserts.ExactInt(Corpus.Cases,
            c => c.Distance,
            c => Indel.Distance(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id} {c.Category}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void NormalizedSimilarity_matches_rapidfuzz()
    {
        // This value ×100 is fuzz.ratio — the anchor for Lot 4.
        OracleAsserts.Approx(Corpus.Cases,
            c => c.NormalizedSimilarity,
            c => Indel.NormalizedSimilarity(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("abc", "abc", 0)]
    [InlineData("MARTHA", "MARHTA", 2)]  // LCS=5 -> 6+6-10
    [InlineData("kitten", "sitting", 5)]
    public void Distance_known_values(string a, string b, int expected)
    {
        Assert.Equal(expected, Indel.Distance(a, b));
    }
}
