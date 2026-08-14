using DataNet.Text.Phonetics;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Phonetics;

public sealed class MetaphoneOracleTests
{
    // Real words, which is Metaphone's domain: decision 0007 scopes out the
    // letter-soup quirks jellyfish has and this does not reproduce.
    private static readonly OracleFile<PhoneticCase> Corpus =
        OracleCorpus.Load<PhoneticCase>("metaphone.json");

    [Fact]
    public void Metaphone_matches_jellyfish_on_real_words()
    {
        OracleAsserts.ExactString(Corpus.Cases,
            c => c.Metaphone,
            c => Metaphone.Encode(c.Word),
            c => $"[#{c.Id}] \"{c.Word}\"");
    }

    [Theory]
    [InlineData("Knuth", "N0")]
    [InlineData("Catherine", "K0RN")]
    [InlineData("Jackson", "JKSN")]
    [InlineData("MacDonald", "MKTNLT")]
    [InlineData("Knighted", "NTT")]
    [InlineData("Thomas", "0MS")]
    [InlineData("", "")]
    public void Metaphone_known_values(string word, string expected)
    {
        Assert.Equal(expected, Metaphone.Encode(word));
    }
}
