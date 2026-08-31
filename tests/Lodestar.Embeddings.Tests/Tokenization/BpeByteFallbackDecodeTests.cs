using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>
/// The decode chain Llama-2 declares, and the two shapes around it that are refused.
/// </summary>
/// <remarks>
/// Measured end to end against <c>tokenizers</c> 0.23.1: the same four tokens decode to
/// <c>aéb</c> under the declared Sequence, to <c>▁aéb</c> under a bare ByteFallback, and to
/// the pieces spelled out when no decoder is declared.
/// </remarks>
public sealed class BpeByteFallbackDecodeTests
{
    [Fact]
    public void The_declared_sequence_undoes_the_escape_and_the_bytes()
    {
        BpeTokenizer tokenizer = Load(LlamaDecoder);
        TokenizationResult encoded = tokenizer.Encode("aéb");

        Assert.Equal("aéb", tokenizer.Decode(encoded.Ids));
    }

    [Fact]
    public void A_bare_ByteFallback_leaves_the_escape_in()
    {
        BpeTokenizer tokenizer = Load("{ \"type\": \"ByteFallback\" }");
        TokenizationResult encoded = tokenizer.Encode("aéb");

        Assert.Equal("▁aéb", tokenizer.Decode(encoded.Ids));
    }

    [Fact]
    public void A_lone_byte_piece_decodes_to_the_replacement_character()
    {
        // Decision 0023's substitution, which the reference also applies here: <0xC3> opens a
        // two-byte sequence, and on its own it is not well-formed UTF-8.
        BpeTokenizer tokenizer = Load("{ \"type\": \"ByteFallback\" }");
        Assert.True(tokenizer.TryGetId("<0xC3>", out int id));

        Assert.Equal("�", tokenizer.Decode([id]));
    }

    [Fact]
    public void A_decoder_shape_that_is_not_reproduced_is_refused()
    {
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(
            () => Load("{ \"type\": \"WordPiece\", \"prefix\": \"##\", \"cleanup\": true }"));

        Assert.Contains("decoder", thrown.Message, StringComparison.Ordinal);
    }

    private const string LlamaDecoder =
        "{ \"type\": \"Sequence\", \"decoders\": [ { \"type\": \"Replace\", \"pattern\": { \"String\": \"▁\" }, \"content\": \" \" },"
        + " { \"type\": \"ByteFallback\" }, { \"type\": \"Fuse\" },"
        + " { \"type\": \"Strip\", \"content\": \" \", \"start\": 1, \"stop\": 0 } ] }";

    private static BpeTokenizer Load(string decoder) => new(Vocabulary(decoder));

    private static BpeVocabulary Vocabulary(string decoder)
    {
        // A metaspace + byte_fallback file, which is the shape both target models declare.
        var entries = new List<string> { "\"<unk>\": 0", "\"a\": 1", "\"b\": 2", "\"▁\": 3", "\"▁a\": 4" };
        int next = 5;
        for (int b = 0; b < BytePieces.Count; b++)
        {
            entries.Add($"\"{BytePieces.Name(b)}\": {next++}");
        }
        string json = "{ \"version\": \"1.0\", \"added_tokens\": [], \"normalizer\": null,"
            + " \"pre_tokenizer\": { \"type\": \"Metaspace\", \"replacement\": \"▁\", \"prepend_scheme\": \"first\", \"split\": false },"
            + $" \"post_processor\": null, \"decoder\": {decoder}, \"model\": {{ \"type\": \"BPE\", \"unk_token\": \"<unk>\","
            + $" \"byte_fallback\": true, \"vocab\": {{ {string.Join(", ", entries)} }}, \"merges\": [\"▁ a\"] }} }}";
        return TokenizerJsonLoader.LoadBpe(new MemoryStream(Encoding.UTF8.GetBytes(json)), OracleReplay.BpeBounds());
    }
}
