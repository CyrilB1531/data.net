using DataNet.Text.Distances;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Distances;

public sealed class OsaOracleTests
{
    private static readonly OracleFile<EditDistanceCase> Corpus =
        OracleCorpus.Load<EditDistanceCase>("osa.json");

    [Fact]
    public void Metadata_is_rapidfuzz_osa()
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
            c => Osa.Distance(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id} {c.Category}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void NormalizedDistance_matches_rapidfuzz()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.NormalizedDistance,
            c => Osa.NormalizedDistance(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void NormalizedSimilarity_matches_rapidfuzz()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.NormalizedSimilarity,
            c => Osa.NormalizedSimilarity(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Theory]
    [InlineData("ab", "ba", 1)]      // single adjacent transposition
    [InlineData("CA", "ABC", 3)]     // OSA restriction: differs from full Damerau (2)
    [InlineData("abcd", "acbd", 1)]
    [InlineData("kitten", "sitting", 3)]
    public void Distance_known_values(string a, string b, int expected)
    {
        Assert.Equal(expected, Osa.Distance(a, b));
    }
}
