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

    /// <summary>One U+FFFD per byte of a run that is not well-formed UTF-8, which is the reference's rule.</summary>
    /// <remarks>
    /// Not decision 0023's, which is the <c>ByteLevel</c> decoder: .NET's lossy UTF-8 decoder
    /// substitutes once per maximal invalid subpart, so the two agree on a lone lead byte and
    /// part on every longer run — <c>&lt;0xF0&gt; &lt;0x9F&gt;</c> is two characters here and
    /// would be one, and <c>&lt;0xC3&gt; &lt;0x28&gt;</c> two rather than U+FFFD and <c>(</c>.
    /// Pinned against <c>tokenizers</c> 0.23.1 by <c>BpeByteFallbackOracleTests</c>.
    /// </remarks>
    [Theory]
    [InlineData("\ufffd", "<0xC3>")]
    [InlineData("\ufffd\ufffd", "<0xF0>", "<0x9F>")]
    [InlineData("\ufffd\ufffd", "<0xC3>", "<0x28>")]
    [InlineData("a\ufffd\ufffdb", "a", "<0xF0>", "<0x9F>", "b")]
    [InlineData("é", "<0xC3>", "<0xA9>")]
    public void A_byte_run_decodes_the_way_the_reference_decodes_it(string expected, params string[] pieces)
    {
        BpeTokenizer tokenizer = Load("{ \"type\": \"ByteFallback\" }");
        var ids = new List<int>();
        foreach (string piece in pieces)
        {
            Assert.True(tokenizer.TryGetId(piece, out int id));
            ids.Add(id);
        }

        Assert.Equal(expected, tokenizer.Decode(ids));
    }

    [Fact]
    public void A_decoder_shape_that_is_not_reproduced_is_refused()
    {
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(
            () => Load("{ \"type\": \"WordPiece\", \"prefix\": \"##\", \"cleanup\": true }"));

        Assert.Contains("decoder", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_decoder_Sequence_out_of_order_is_refused()
    {
        // Strip first is the same four steps LlamaDecoder declares, reordered -- the reference
        // runs them in the declared order, so this is not an equivalent Sequence.
        const string reordered =
            "{ \"type\": \"Sequence\", \"decoders\": [ { \"type\": \"Strip\", \"content\": \" \", \"start\": 1, \"stop\": 0 },"
            + " { \"type\": \"Replace\", \"pattern\": { \"String\": \"▁\" }, \"content\": \" \" },"
            + " { \"type\": \"ByteFallback\" }, { \"type\": \"Fuse\" } ] }";

        InvalidDataException thrown = Assert.Throws<InvalidDataException>(() => Load(reordered));

        Assert.Contains("decoder", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_metaspace_file_without_byte_fallback_still_loads_its_Metaspace_decoder()
    {
        const string metaspaceDecoder =
            "{ \"type\": \"Metaspace\", \"replacement\": \"▁\", \"prepend_scheme\": \"first\", \"split\": false }";
        BpeTokenizer tokenizer = Load(metaspaceDecoder, byteFallback: false);
        Assert.True(tokenizer.TryGetId("▁a", out int id));

        // Decision 0062: outside the byte_fallback boundary, the decoder is accepted and not
        // applied -- the escape stays a symbol rather than becoming a space.
        Assert.Equal("▁a", tokenizer.Decode([id]));
    }

    private const string LlamaDecoder =
        "{ \"type\": \"Sequence\", \"decoders\": [ { \"type\": \"Replace\", \"pattern\": { \"String\": \"▁\" }, \"content\": \" \" },"
        + " { \"type\": \"ByteFallback\" }, { \"type\": \"Fuse\" },"
        + " { \"type\": \"Strip\", \"content\": \" \", \"start\": 1, \"stop\": 0 } ] }";

    private static BpeTokenizer Load(string decoder, bool byteFallback = true) => new(Vocabulary(decoder, byteFallback));

    private static BpeVocabulary Vocabulary(string decoder, bool byteFallback = true)
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
            + $" \"byte_fallback\": {(byteFallback ? "true" : "false")}, \"vocab\": {{ {string.Join(", ", entries)} }}, \"merges\": [\"▁ a\"] }} }}";
        return TokenizerJsonLoader.LoadBpe(new MemoryStream(Encoding.UTF8.GetBytes(json)), OracleReplay.BpeBounds());
    }
}
