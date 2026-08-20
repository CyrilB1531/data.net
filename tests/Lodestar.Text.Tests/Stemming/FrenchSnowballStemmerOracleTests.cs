using Lodestar.Text.Stemming;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Stemming;

public sealed class FrenchSnowballStemmerOracleTests
{
    private static readonly OracleFile<PorterCase> Corpus = OracleCorpus.Load<PorterCase>("snowball_fr.json");

    [Fact]
    public void Metadata_is_nltk()
    {
        Assert.Equal("nltk", Corpus.Metadata.Library);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void Stem_matches_nltk()
    {
        OracleAsserts.ExactString(Corpus.Cases,
            c => c.Stem,
            c => FrenchSnowballStemmer.Stem(c.Word),
            c => $"[#{c.Id}] \"{c.Word}\"");
    }

    [Theory]
    [InlineData("continuellement", "continuel")]
    [InlineData("amoureusement", "amour")]
    [InlineData("chevaux", "cheval")]
    [InlineData("finissait", "fin")]
    [InlineData("gentiment", "gent")]
    [InlineData("prière", "prier")]
    public void Stem_known_values(string word, string expected)
    {
        Assert.Equal(expected, FrenchSnowballStemmer.Stem(word));
    }

    [Fact]
    public void Stem_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FrenchSnowballStemmer.Stem(null!));
    }
}
