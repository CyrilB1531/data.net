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
    }

    [Fact]
    public void A_Prepend_and_Replace_normalizer_becomes_the_same_escape()
    {
        BpeVocabulary vocabulary = Load(
            "\"normalizer\": { \"type\": \"Sequence\", \"normalizers\": [ { \"type\": \"Prepend\", \"prepend\": \"▁\" }, { \"type\": \"Replace\", \"pattern\": { \"String\": \" \" }, \"content\": \"▁\" } ] },");

        Assert.NotNull(vocabulary.Metaspace);
        Assert.Equal('▁', vocabulary.Metaspace!.Replacement);
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

    private static BpeVocabulary Load(string block)
    {
        string json = "{ \"version\": \"1.0\", " + block
            + " \"model\": { \"type\": \"BPE\", \"vocab\": { \"a\": 0, \"b\": 1, \"ab\": 2 }, \"merges\": [\"a b\"] } }";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }
}
