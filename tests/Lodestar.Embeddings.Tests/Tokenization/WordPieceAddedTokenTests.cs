using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// Replays <c>wordpiece_added_tokens.json</c>: a lowercasing WordPiece model whose <c>added_tokens</c>
/// table uses all four matching flags. No other committed WordPiece corpus adds a token at all --
/// <c>tokenizer_json.json</c> and <c>vocab_txt.json</c> both carry an empty table -- so this is the only
/// evidence <see cref="WordPieceTokenizer"/> reads one, and the only place <c>normalized</c> can be seen:
/// a model without a normalizer cannot tell its two passes apart. Its four cases (ids 0-7): a raw entry
/// (<c>[CLS]</c>) vs. un-lowercased text, a normalized one (<c>&lt;MASK&gt;</c>) vs. lowercased text
/// emitting the lowercased form, <c>[SEP]</c> both <c>special</c> and <c>normalized</c> (proving the
/// discriminator is <c>normalized</c>, not <c>special</c>), and <c>&lt;R&gt;</c>/<c>A&lt;R&gt;</c>, where the raw pass wins over a normalized match starting further left.
/// </summary>
public sealed class WordPieceAddedTokenTests
{
    [Fact]
    public void Encode_matches_tokenizers_for_every_case()
    {
        using JsonDocument doc = OracleLoader.Load("wordpiece_added_tokens.json");
        var tokenizer = new WordPieceTokenizer(Vocabulary(doc));

        OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens", nameProperty: "name");
    }

    /// <summary>
    /// The table the corpus's own file declares, read through
    /// <see cref="TokenizerJsonLoader.LoadWordPiece(Stream, ArtifactLoadOptions?)"/>. Asserted separately
    /// from the encodings because <c>[SEP]</c> is the entry the natural implementation gets wrong, and it
    /// would be lost in a wall of token diffs: an entry both <c>special</c> and <c>normalized</c> is one
    /// the rule every <c>add_special_tokens</c> file obeys -- normalized as the opposite of special -- says cannot exist.
    /// </summary>
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
