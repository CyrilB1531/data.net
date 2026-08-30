using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>
/// The two spellings a file uses for one value, and the shape that stays refused.
/// </summary>
/// <remarks>
/// Llama-2 declares a <c>Prepend</c>+<c>Replace</c> normalizer with a null
/// pre-tokenizer; Mistral v0.1 declares a <c>Metaspace</c> pre-tokenizer with a null
/// normalizer. Decision 0050 makes the loader absorb that variation (#316).
/// </remarks>
public sealed class BpeMetaspaceLoaderTests
{
    [Fact]
    public void A_Metaspace_pre_tokenizer_becomes_the_escape()
    {
        BpeVocabulary vocabulary = Load(
            "\"pre_tokenizer\": { \"type\": \"Metaspace\", \"replacement\": \"▁\", \"prepend_scheme\": \"first\", \"split\": false },");

        Assert.NotNull(vocabulary.Metaspace);
        Assert.Equal('▁', vocabulary.Metaspace!.Replacement);
        Assert.False(vocabulary.Metaspace.RemoveExtraWhitespaces);
        Assert.Equal(MetaspacePrependScheme.First, vocabulary.Metaspace.PrependScheme);
        Assert.True(vocabulary.Metaspace.SkipPrependWhenAlreadyPrefixed);
    }

    [Fact]
    public void A_Prepend_and_Replace_normalizer_becomes_the_same_escape()
    {
        BpeVocabulary vocabulary = Load(
            "\"normalizer\": { \"type\": \"Sequence\", \"normalizers\": [ { \"type\": \"Prepend\", \"prepend\": \"▁\" }, { \"type\": \"Replace\", \"pattern\": { \"String\": \" \" }, \"content\": \"▁\" } ] },");

        Assert.NotNull(vocabulary.Metaspace);
        Assert.Equal('▁', vocabulary.Metaspace!.Replacement);
        Assert.False(vocabulary.Metaspace.RemoveExtraWhitespaces);
        Assert.Equal(MetaspacePrependScheme.Always, vocabulary.Metaspace.PrependScheme);
        Assert.False(vocabulary.Metaspace.SkipPrependWhenAlreadyPrefixed);
    }

    [Fact]
    public void A_sequence_carrying_a_step_we_do_not_reproduce_is_refused()
    {
        // Decision 0050 keeps 0017's rule while overturning two of its clauses: refusing
        // beats reducing a three-step sequence to the two steps we know.
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(() => Load(
            "\"normalizer\": { \"type\": \"Sequence\", \"normalizers\": [ { \"type\": \"Prepend\", \"prepend\": \"▁\" }, { \"type\": \"Replace\", \"pattern\": { \"String\": \" \" }, \"content\": \"▁\" }, { \"type\": \"NFC\" } ] },"));

        Assert.Contains("Sequence", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>pattern</c> is an object in every file <c>tokenizers</c> writes, and a hand-made
    /// one holding a bare string used to reach <c>TryGetProperty</c> on a non-object —
    /// an <c>InvalidOperationException</c> where the loader promises <c>InvalidDataException</c>.
    /// </summary>
    [Fact]
    public void A_Replace_whose_pattern_is_a_bare_string_is_refused_by_name()
    {
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(() => Load(
            "\"normalizer\": { \"type\": \"Sequence\", \"normalizers\": [ { \"type\": \"Prepend\", \"prepend\": \"\u2581\" }, { \"type\": \"Replace\", \"pattern\": \" \", \"content\": \"\u2581\" } ] },"));

        Assert.Contains("Prepend and a Replace", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_writing_both_spellings_is_refused()
    {
        // Neither model decision 0050 was read from writes both, so there is no
        // measurement saying which of the two such a file would apply.
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(() => Load(
            MetaspaceBlock + " " + PrependReplaceBlock));

        Assert.Contains("Metaspace pre_tokenizer", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("Prepend plus Replace normalizer Sequence", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Before <c>tokenizers</c> 0.14 a <c>Metaspace</c> carried a boolean where it now
    /// carries <c>prepend_scheme</c>, and the loader absorbs that spelling too.
    /// </summary>
    [Fact]
    public void The_pre_0_14_add_prefix_space_spelling_is_a_prepend_scheme()
    {
        // One Fact rather than a Theory: MetaspacePrependScheme is internal, and an
        // InlineData parameter of it would make this method less accessible than xunit needs.
        Assert.Equal(MetaspacePrependScheme.Always, WithAddPrefixSpace("true").Metaspace!.PrependScheme);
        Assert.Equal(MetaspacePrependScheme.Never, WithAddPrefixSpace("false").Metaspace!.PrependScheme);
    }

    /// <summary>
    /// The two spellings are one value but for the prepend, which decision 0062 measures
    /// and this pins field by field — the escape the loader builds is where it lives.
    /// </summary>
    [Fact]
    public void The_two_spellings_produce_one_value_but_for_the_prepend()
    {
        MetaspaceEscape fromPreTokenizer = Load(AlwaysMetaspaceBlock).Metaspace!;
        MetaspaceEscape fromNormalizer = Load(PrependReplaceBlock).Metaspace!;

        Assert.Equal(fromPreTokenizer.Replacement, fromNormalizer.Replacement);
        Assert.Equal(fromPreTokenizer.RemoveExtraWhitespaces, fromNormalizer.RemoveExtraWhitespaces);
        Assert.Equal(fromPreTokenizer.PrependScheme, fromNormalizer.PrependScheme);
        Assert.NotEqual(fromPreTokenizer.SkipPrependWhenAlreadyPrefixed, fromNormalizer.SkipPrependWhenAlreadyPrefixed);
    }

    /// <summary>
    /// The normalizer spelling reads as <c>always</c>, not <c>first</c>: it runs on every
    /// gap the added tokens leave, so it prepends to each of them. Measured —
    /// <c>bpe_metaspace.json</c>'s two cases part on exactly that text.
    /// </summary>
    [Fact]
    public void The_normalizer_spelling_prepends_to_every_piece()
    {
        Assert.Equal(MetaspacePrependScheme.Always, Load(PrependReplaceBlock).Metaspace!.PrependScheme);
    }

    [Fact]
    public void A_Metaspace_that_still_splits_is_refused()
    {
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(() => Load(
            "\"pre_tokenizer\": { \"type\": \"Metaspace\", \"replacement\": \"\u2581\", \"prepend_scheme\": \"first\" },"));

        Assert.Contains("split", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The escape reaching the merge loop, which is what a loaded value nothing applies
    /// would not do: every token here is spelled with the symbol the text never held.
    /// </summary>
    [Fact]
    public void The_escape_reaches_the_token_stream()
    {
        BpeVocabulary vocabulary = Load(
            MetaspaceBlock,
            "\"model\": { \"type\": \"BPE\", \"vocab\": { \"a\": 0, \"b\": 1, \"\u2581\": 2, \"\u2581a\": 3 }, \"merges\": [\"\u2581 a\"] }");

        TokenizationResult encoded = new BpeTokenizer(vocabulary).Encode("a b");

        Assert.Equal(["\u2581a", "\u2581", "b"], encoded.Tokens);
        Assert.Equal([3, 2, 1], encoded.Ids);
    }

    private const string MetaspaceBlock =
        "\"pre_tokenizer\": { \"type\": \"Metaspace\", \"replacement\": \"\u2581\", \"prepend_scheme\": \"first\", \"split\": false },";

    private const string AlwaysMetaspaceBlock =
        "\"pre_tokenizer\": { \"type\": \"Metaspace\", \"replacement\": \"\u2581\", \"prepend_scheme\": \"always\", \"split\": false },";

    private const string PrependReplaceBlock =
        "\"normalizer\": { \"type\": \"Sequence\", \"normalizers\": [ { \"type\": \"Prepend\", \"prepend\": \"\u2581\" }, { \"type\": \"Replace\", \"pattern\": { \"String\": \" \" }, \"content\": \"\u2581\" } ] },";

    private static BpeVocabulary WithAddPrefixSpace(string declared) =>
        Load("\"pre_tokenizer\": { \"type\": \"Metaspace\", \"replacement\": \"\u2581\", \"add_prefix_space\": "
            + declared + ", \"split\": false },");

    private static BpeVocabulary Load(string block) =>
        Load(block, "\"model\": { \"type\": \"BPE\", \"vocab\": { \"a\": 0, \"b\": 1, \"ab\": 2 }, \"merges\": [\"a b\"] }");

    private static BpeVocabulary Load(string block, string model)
    {
        string json = "{ \"version\": \"1.0\", " + block + " " + model + " }";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }
}
