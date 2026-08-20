using Lodestar.Text.Stemming;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Stemming;

public sealed class EnglishSnowballStemmerOracleTests
{
    private static readonly OracleFile<PorterCase> Corpus = OracleCorpus.Load<PorterCase>("snowball_en.json");

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
            c => EnglishSnowballStemmer.Stem(c.Word),
            c => $"[#{c.Id}] \"{c.Word}\"");
    }

    [Theory]
    [InlineData("generously", "generous")] // gener- prefix keeps R1 correct
    [InlineData("national", "nation")]
    [InlineData("running", "run")]
    [InlineData("skis", "ski")]            // exception
    [InlineData("proceed", "proceed")]     // exception 2
    public void Stem_known_values(string word, string expected)
    {
        Assert.Equal(expected, EnglishSnowballStemmer.Stem(word));
    }

    [Fact]
    public void Stem_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EnglishSnowballStemmer.Stem(null!));
    }
}
