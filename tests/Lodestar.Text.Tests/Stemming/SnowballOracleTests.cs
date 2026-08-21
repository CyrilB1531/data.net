using Lodestar.Text.Stemming;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Stemming;

/// <summary>Replays one frozen nltk corpus per language added by #176, from a table.</summary>
/// <remarks>
/// The six languages that predate #176 each have their own class, and those classes are
/// identical bar two identifiers. Nine more copies is the duplication that shape invites,
/// so the languages arriving here contribute a row instead — one per lot.
/// </remarks>
public sealed class SnowballOracleTests
{
    public static TheoryData<string, string> Languages => new()
    {
        { "snowball_nl.json", "DutchSnowballStemmer" },
    };

    private static string Stem(string algorithm, string word) => algorithm switch
    {
        "DutchSnowballStemmer" => DutchSnowballStemmer.Stem(word),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "no stemmer for it"),
    };

    [Theory]
    [MemberData(nameof(Languages))]
    public void Metadata_is_nltk(string corpus, string algorithm)
    {
        OracleFile<PorterCase> file = OracleCorpus.Load<PorterCase>(corpus);
        Assert.Equal("nltk", file.Metadata.Library);
        Assert.Equal(algorithm, file.Metadata.Algorithm);
        Assert.NotEmpty(file.Cases);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Stem_matches_nltk(string corpus, string algorithm)
    {
        OracleFile<PorterCase> file = OracleCorpus.Load<PorterCase>(corpus);
        OracleAsserts.ExactString(file.Cases,
            c => c.Stem,
            c => Stem(algorithm, c.Word),
            c => $"[#{c.Id}] \"{c.Word}\"");
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Stem_NullArgument_ThrowsArgumentNullException(string corpus, string algorithm)
    {
        Assert.NotNull(corpus);
        Assert.Throws<ArgumentNullException>(() => Stem(algorithm, null!));
    }
}
