using DataNet.Text.Distances;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Distances;

public sealed class RatcliffObershelpOracleTests
{
    private static readonly OracleFile<SimilarityCase> Corpus =
        OracleCorpus.Load<SimilarityCase>("ratcliff.json");

    [Fact]
    public void Metadata_is_difflib()
    {
        Assert.Equal("difflib", Corpus.Metadata.Library);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void Similarity_matches_difflib()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.Similarity,
            c => RatcliffObershelp.Similarity(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id} {c.Category}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Theory]
    [InlineData("", "", 1.0)]
    [InlineData("abc", "abc", 1.0)]
    [InlineData("kitten", "sitting", 0.6153846153846154)]
    [InlineData("Dupont", "Dupond", 0.8333333333333334)]
    public void Known_values(string a, string b, double expected)
    {
        Assert.Equal(expected, RatcliffObershelp.Similarity(a, b), 12);
    }
}
