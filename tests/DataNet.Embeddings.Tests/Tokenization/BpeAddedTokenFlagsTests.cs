using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_added_token_flags.json</c>: GPT-2 with one added token per matching flag --
/// <c>&lt;mask&gt;</c> <c>lstrip</c>, <c>&lt;pad&gt;</c> <c>rstrip</c>, <c>&lt;m&gt;</c>
/// <c>single_word</c> -- over the inputs each flag is visible on. The corpus carries the whole
/// <c>tokenizer.json</c> in its metadata, so the bytes parsed here are what <c>tokenizers</c> 0.23.1
/// was handed; everything issue #104 added to <see cref="AddedTokenScanner"/> is otherwise only
/// hand-written unit tests. A strip changes no id, only the piece the absorbed whitespace would have
/// produced (a <c>Ġ</c> on a byte-level model) and the matched slice: <c>"a &lt;mask&gt; b"</c> is
/// <c>['a', ' &lt;mask&gt;', 'Ġb']</c>. Token strings are asserted too, not just ids, which alone would pass with every strip ignored.
/// </summary>
public sealed class BpeAddedTokenFlagsTests
{
    [Fact]
    public void Encode_matches_tokenizers_for_every_matching_flag()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_added_token_flags.json");
        var tokenizer = new BpeTokenizer(Vocabulary(doc));

        OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens");
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

        OracleReplay.AssertDecodes(doc, (ids, skip) => tokenizer.Decode(ids, skipSpecialTokens: skip));
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
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }
}
