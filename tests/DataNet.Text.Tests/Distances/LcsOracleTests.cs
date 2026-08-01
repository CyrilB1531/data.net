using DataNet.Text.Distances;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Distances;

public sealed class LcsOracleTests
{
    private static readonly OracleFile<LcsCase> Corpus = OracleCorpus.Load<LcsCase>("lcs.json");

    [Fact]
    public void SubsequenceLength_matches_reference()
    {
        OracleAsserts.ExactInt(Corpus.Cases,
            c => c.Subsequence,
            c => Lcs.SubsequenceLength(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id} {c.Category}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void SubstringLength_matches_difflib()
    {
        OracleAsserts.ExactInt(Corpus.Cases,
            c => c.Substring,
            c => Lcs.SubstringLength(c.A, c.B, TextElement.CodePoint),
            c => $"[#{c.Id} {c.Category}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Theory]
    [InlineData("abcde", "ace", 3, 1)]
    [InlineData("Dupont", "Dupond", 5, 5)]
    [InlineData("kitten", "sitting", 4, 3)]
    [InlineData("", "abc", 0, 0)]
    public void Known_values(string a, string b, int subsequence, int substring)
    {
        Assert.Equal(subsequence, Lcs.SubsequenceLength(a, b));
        Assert.Equal(substring, Lcs.SubstringLength(a, b));
    }
}
