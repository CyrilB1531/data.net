using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>wordpiece_added_tokens.json</c>: a lowercasing WordPiece model whose
/// <c>added_tokens</c> table uses all four matching flags.
/// </summary>
/// <remarks>
/// <para>
/// No other committed WordPiece corpus adds a token at all — <c>tokenizer_json.json</c>
/// and <c>vocab_txt.json</c> both carry an empty table — so this is the only
/// replayed evidence that <see cref="WordPieceTokenizer"/> reads one, and the only
/// place the <c>normalized</c> flag can be seen at all: it decides which of two
/// passes an entry runs in, and a model without a normalizer cannot tell the
/// passes apart.
/// </para>
/// <para>
/// The four cases the corpus is named for are cases 0-7: a raw entry
/// (<c>[CLS]</c>) matched against the un-lowercased text, a normalized one
/// (<c>&lt;MASK&gt;</c>) matched against the lowercased text and emitting the
/// lowercased form, <c>[SEP]</c> declared <c>special</c> <em>and</em>
/// <c>normalized</c> — the combination that proves the discriminator is
/// <c>normalized</c> rather than <c>special</c> — and the overlapping pair
/// <c>&lt;R&gt;</c> (raw) / <c>A&lt;R&gt;</c> (normalized), where the raw pass wins
/// over a normalized match that starts further left.
/// </para>
/// </remarks>
public sealed class WordPieceAddedTokenTests
{
    [Fact]
    public void Encode_matches_tokenizers_for_every_case()
    {
        using JsonDocument doc = OracleLoader.Load("wordpiece_added_tokens.json");
        var tokenizer = new WordPieceTokenizer(Vocabulary(doc));

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"[{name}] {JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expectedTokens)}] [{string.Join(", ", expectedIds)}]\n  got: [{string.Join(" | ", actual.Tokens)}] [{string.Join(", ", actual.Ids)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The table the corpus's own file declares, read through
    /// <see cref="TokenizerJsonLoader.LoadWordPiece(Stream, ArtifactLoadOptions?)"/>.
    /// </summary>
    /// <remarks>
    /// Asserted separately from the encodings because <c>[SEP]</c> is the entry the
    /// natural implementation gets wrong, and it would be lost in a wall of token
    /// diffs: an entry that is <c>special</c> and <c>normalized</c> at once is one
    /// that <c>normalized = !special</c> — the rule every file
    /// <c>add_special_tokens</c> wrote obeys — says cannot exist.
    /// </remarks>
    [Fact]
    public void The_file_the_corpus_carries_declares_both_passes_and_every_flag()
    {
        using JsonDocument doc = OracleLoader.Load("wordpiece_added_tokens.json");
        IReadOnlyList<AddedToken> added = Vocabulary(doc).AddedTokens;

        AddedToken cls = Assert.Single(added, t => t.Content == "[CLS]");
        Assert.True(cls.Special);
        Assert.False(cls.Normalized);

        AddedToken mask = Assert.Single(added, t => t.Content == "<MASK>");
        Assert.False(mask.Special);
        Assert.True(mask.Normalized);

        AddedToken sep = Assert.Single(added, t => t.Content == "[SEP]");
        Assert.True(sep.Special);
        Assert.True(sep.Normalized);

        Assert.False(Assert.Single(added, t => t.Content == "<R>").Normalized);
        Assert.True(Assert.Single(added, t => t.Content == "A<R>").Normalized);

        AddedToken lstrip = Assert.Single(added, t => t.Content == "<L>");
        Assert.True(lstrip.Lstrip);
        Assert.False(lstrip.Normalized);

        AddedToken rstrip = Assert.Single(added, t => t.Content == "<W>");
        Assert.True(rstrip.Rstrip);
        Assert.True(rstrip.Normalized);

        Assert.True(Assert.Single(added, t => t.Content == "<S>").SingleWord);
    }

    /// <summary>
    /// The model's own vocabulary is left as the file wrote it: an added token is
    /// matched as text, so folding it in would also make it a piece the WordPiece
    /// model itself could emit.
    /// </summary>
    [Fact]
    public void The_added_tokens_are_not_folded_into_the_model_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("wordpiece_added_tokens.json");
        WordPieceVocabulary vocabulary = Vocabulary(doc);

        Assert.Equal(8, vocabulary.AddedTokens.Count);
        Assert.DoesNotContain("[CLS]", vocabulary.Vocab.Keys, StringComparer.Ordinal);
        Assert.True(vocabulary.Lowercase);

        // The ids the added entries carry continue the model's own numbering, which
        // is what makes a fold-in look harmless until a text names one.
        Assert.All(vocabulary.AddedTokens, t => Assert.True(t.Id >= vocabulary.Count));
    }

    private static WordPieceVocabulary Vocabulary(JsonDocument doc)
    {
        string json = doc.RootElement.GetProperty("metadata").GetProperty("tokenizer_json").GetString()!;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return TokenizerJsonLoader.LoadWordPiece(stream);
    }
}
