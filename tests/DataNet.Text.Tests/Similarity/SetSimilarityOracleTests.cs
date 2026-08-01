using DataNet.Text;
using DataNet.Text.Similarity;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.SetSim;

public sealed class SetSimilarityOracleTests
{
    private static readonly OracleFile<SetSimilarityCase> Corpus =
        OracleCorpus.Load<SetSimilarityCase>("set_similarity.json");

    [Fact]
    public void Metadata_is_textdistance()
    {
        Assert.Equal("textdistance", Corpus.Metadata.Library);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void Jaccard_matches_textdistance()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.Jaccard,
            c => Jaccard.Similarity(c.A, c.B, qval: 1, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void Dice_matches_textdistance()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.Dice,
            c => SorensenDice.Similarity(c.A, c.B, qval: 1, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void Overlap_matches_textdistance()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.Overlap,
            c => Overlap.Similarity(c.A, c.B, qval: 1, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void Tversky_matches_textdistance()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.Tversky,
            c => Tversky.Similarity(c.A, c.B, element: TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Fact]
    public void Cosine_matches_textdistance()
    {
        OracleAsserts.Approx(Corpus.Cases,
            c => c.Cosine,
            c => Cosine.Similarity(c.A, c.B, qval: 1, TextElement.CodePoint),
            c => $"[#{c.Id}] {OracleAsserts.Escape(c.A)}/{OracleAsserts.Escape(c.B)}");
    }

    [Theory]
    [InlineData("", "", 1.0)]      // both empty -> identical
    [InlineData("abc", "", 0.0)]   // one empty -> disjoint
    [InlineData("cat", "cot", 0.5)]
    public void Jaccard_edge_and_known(string a, string b, double expected)
    {
        Assert.Equal(expected, Jaccard.Similarity(a, b), 12);
    }

    [Fact]
    public void Cosine_bigrams_dupont_dupond()
    {
        // Character bigrams: "Dupont"/"Dupond" share Du,up,po,on = 4 of 5 -> 0.8.
        Assert.Equal(0.8, Cosine.Similarity("Dupont", "Dupond", qval: 2), 12);
    }
}
