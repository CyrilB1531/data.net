using System.Text.Json.Serialization;
using DataNet.Text.Stemming;
using DataNet.Text.Tests.Oracles;
using Xunit;

namespace DataNet.Text.Tests.Stemming;

public sealed record PorterCase
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("word")] public string Word { get; init; } = "";
    [JsonPropertyName("stem")] public string Stem { get; init; } = "";
}

public sealed class PorterStemmerOracleTests
{
    private static readonly OracleFile<PorterCase> Corpus = OracleCorpus.Load<PorterCase>("porter.json");

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
            c => PorterStemmer.Stem(c.Word),
            c => $"[#{c.Id}] \"{c.Word}\"");
    }

    [Theory]
    [InlineData("caresses", "caress")]
    [InlineData("ponies", "poni")]
    [InlineData("relational", "relat")]
    [InlineData("running", "run")]
    [InlineData("happy", "happi")]
    public void Stem_known_values(string word, string expected)
    {
        Assert.Equal(expected, PorterStemmer.Stem(word));
    }
}
