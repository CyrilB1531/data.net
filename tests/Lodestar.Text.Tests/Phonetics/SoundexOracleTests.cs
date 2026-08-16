using Lodestar.Text.Phonetics;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Phonetics;

public sealed class SoundexOracleTests
{
    private static readonly OracleFile<PhoneticCase> Corpus =
        OracleCorpus.Load<PhoneticCase>("phonetics.json");

    [Fact]
    public void Metadata_is_jellyfish()
    {
        Assert.Equal("jellyfish", Corpus.Metadata.Library);
        Assert.NotEmpty(Corpus.Cases);
    }

    [Fact]
    public void Soundex_matches_jellyfish()
    {
        OracleAsserts.ExactString(Corpus.Cases,
            c => c.Soundex,
            c => Soundex.Encode(c.Word),
            c => $"[#{c.Id}] \"{c.Word}\"");
    }

    [Theory]
    [InlineData("Robert", "R163")]
    [InlineData("Rupert", "R163")]
    [InlineData("Ashcraft", "A261")]
    [InlineData("Tymczak", "T522")]
    [InlineData("Pfister", "P236")]
    [InlineData("Lee", "L000")]
    [InlineData("", "")]
    public void Soundex_known_values(string word, string expected)
    {
        Assert.Equal(expected, Soundex.Encode(word));
    }
}
