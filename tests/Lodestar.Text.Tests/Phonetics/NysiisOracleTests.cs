using Lodestar.Text.Phonetics;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Phonetics;

public sealed class NysiisOracleTests
{
    private static readonly OracleFile<PhoneticCase> Corpus =
        OracleCorpus.Load<PhoneticCase>("phonetics.json");

    [Fact]
    public void Nysiis_matches_jellyfish()
    {
        OracleAsserts.ExactString(Corpus.Cases,
            c => c.Nysiis,
            c => Nysiis.Encode(c.Word),
            c => $"[#{c.Id}] \"{c.Word}\"");
    }

    [Theory]
    [InlineData("Robert", "RABAD")]
    [InlineData("Honeyman", "HANAYNAN")]
    [InlineData("Knuth", "NAT")]
    [InlineData("MacDonald", "MCDANALD")]
    [InlineData("Brown", "BRAON")]
    [InlineData("", "")]
    public void Nysiis_known_values(string word, string expected)
    {
        Assert.Equal(expected, Nysiis.Encode(word));
    }

    [Fact]
    public void Encode_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Nysiis.Encode(null!));
    }
}
