using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_added_token_flags.json</c>: GPT-2 with one added token per
/// matching flag — <c>&lt;mask&gt;</c> <c>lstrip</c>, <c>&lt;pad&gt;</c>
/// <c>rstrip</c>, <c>&lt;m&gt;</c> <c>single_word</c> — over the inputs each flag
/// is visible on.
/// </summary>
/// <remarks>
/// <para>
/// The corpus carries the whole <c>tokenizer.json</c> in its metadata, so the
/// bytes parsed here are the ones <c>tokenizers</c> 0.23.1 was handed. Everything
/// issue #104 added to <see cref="AddedTokenScanner"/> is hand-written unit tests
/// otherwise; this is the replayed evidence.
/// </para>
/// <para>
/// A strip changes no id. What it changes is the piece the absorbed whitespace
/// would have produced — a <c>Ġ</c> on a byte-level model — and the token string,
/// which is the matched slice rather than the entry's content:
/// <c>"a &lt;mask&gt; b"</c> is <c>['a', ' &lt;mask&gt;', 'Ġb']</c>, the space
/// swallowed into the match. So the token strings are asserted too, not the ids
/// alone: ids alone would pass with every strip ignored.
/// </para>
/// </remarks>
public sealed class BpeAddedTokenFlagsTests
{
    [Fact]
    public void Encode_matches_tokenizers_for_every_matching_flag()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_added_token_flags.json");
        var tokenizer = new BpeTokenizer(Vocabulary(doc));

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expectedTokens)}] [{string.Join(", ", expectedIds)}]\n  got: [{string.Join(" | ", actual.Tokens)}] [{string.Join(", ", actual.Ids)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The decode direction. <c>&lt;m&gt;</c> is the one entry the file leaves
    /// non-special, so <c>skipSpecialTokens</c> drops the other two and keeps it —
    /// which is what tells <see cref="AddedToken.Special"/> apart from a flag that
    /// decides where an entry matches.
    /// </summary>
    [Fact]
    public void Decode_matches_tokenizers_in_both_modes()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_added_token_flags.json");
        var tokenizer = new BpeTokenizer(Vocabulary(doc));

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int[] ids = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            string expected = c.GetProperty("decoded").GetString()!;
            string expectedSkipping = c.GetProperty("decoded_skip_specials").GetString()!;

            string actual = tokenizer.Decode(ids);
            string actualSkipping = tokenizer.Decode(ids, skipSpecialTokens: true);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"decode {JsonSerializer.Serialize(expected)} got {JsonSerializer.Serialize(actual)}");
            }
            if (!string.Equals(expectedSkipping, actualSkipping, StringComparison.Ordinal))
            {
                failures.Add($"decode-skipping {JsonSerializer.Serialize(expectedSkipping)} got {JsonSerializer.Serialize(actualSkipping)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The flags reach <see cref="BpeVocabulary.AddedTokens"/> from the corpus's own
    /// file. Asserted separately because every encode above would also pass with the
    /// flags read as <see langword="false"/> and the corpus generated the same way —
    /// this is what pins the corpus to a file that actually declares them.
    /// </summary>
    [Fact]
    public void The_file_the_corpus_carries_declares_one_token_per_flag()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_added_token_flags.json");
        IReadOnlyList<AddedToken> added = Vocabulary(doc).AddedTokens;

        AddedToken mask = Assert.Single(added, t => t.Content == "<mask>");
        Assert.True(mask.Lstrip);
        Assert.False(mask.Rstrip);
        Assert.False(mask.SingleWord);
        Assert.True(mask.Special);

        AddedToken pad = Assert.Single(added, t => t.Content == "<pad>");
        Assert.True(pad.Rstrip);
        Assert.False(pad.Lstrip);
        Assert.True(pad.Special);

        AddedToken single = Assert.Single(added, t => t.Content == "<m>");
        Assert.True(single.SingleWord);
        Assert.False(single.Lstrip);
        Assert.False(single.Rstrip);
        Assert.False(single.Special);
    }

    private static BpeVocabulary Vocabulary(JsonDocument doc)
    {
        string json = doc.RootElement.GetProperty("metadata").GetProperty("tokenizer_json").GetString()!;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return TokenizerJsonLoader.LoadBpe(stream, new ArtifactLoadOptions
        {
            MaxTotalBytes = 8L * 1024 * 1024,
            MaxVocabularySize = 100_000,
            MaxArrayLength = 100_000,
            MaxTokenLength = 512,
        });
    }
}
