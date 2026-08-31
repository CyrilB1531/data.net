using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// What a byte_fallback file has to carry to load, and what it is refused for.
/// </summary>
/// <remarks>
/// These are pinned here rather than by an oracle because <c>tokenizers</c> 0.23.1 accepts
/// every file below: it degrades a missing piece to the unknown token, or drops the symbol
/// when no unknown token is declared. Decision 0063 refuses instead.
/// </remarks>
public sealed class BpeByteFallbackLoaderTests
{
    [Fact]
    public void A_complete_byte_alphabet_loads_with_the_flag_set()
    {
        BpeVocabulary vocabulary = Load(Complete());

        Assert.True(vocabulary.ByteFallback);
    }

    [Fact]
    public void A_file_without_the_flag_leaves_it_off()
    {
        BpeVocabulary vocabulary = Load(Complete(byteFallback: false));

        Assert.False(vocabulary.ByteFallback);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x41)]
    [InlineData(0xFF)]
    public void One_missing_piece_is_refused_and_named(int missing)
    {
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(() => Load(Complete(without: missing)));

        Assert.Contains(BytePieces.Name(missing), thrown.Message, StringComparison.Ordinal);
        Assert.Contains("byte_fallback", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_lowercase_spelling_is_not_a_byte_piece()
    {
        // The reference resolves nothing for <0xc3> and falls to the unknown token, so a
        // vocabulary spelling it that way is one piece short however many entries it has.
        InvalidDataException thrown = Assert.Throws<InvalidDataException>(
            () => Load(Complete(without: 0xC3, extra: "<0xc3>")));

        Assert.Contains("<0xC3>", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>A <c>tokenizer.json</c> whose vocabulary carries every byte piece but the ones named.</summary>
    private static string Complete(bool byteFallback = true, int? without = null, string? extra = null)
    {
        var entries = new List<string> { "\"<unk>\": 0", "\"a\": 1", "\"b\": 2" };
        int next = 3;
        for (int b = 0; b < BytePieces.Count; b++)
        {
            if (b == without)
            {
                continue;
            }
            entries.Add($"\"{BytePieces.Name(b)}\": {next++}");
        }
        if (extra is not null)
        {
            entries.Add($"\"{extra}\": {next}");
        }
        return "{ \"version\": \"1.0\", \"added_tokens\": [], \"normalizer\": null, \"pre_tokenizer\": null,"
            + " \"post_processor\": null, \"decoder\": null, \"model\": { \"type\": \"BPE\", \"unk_token\": \"<unk>\","
            + $" \"byte_fallback\": {(byteFallback ? "true" : "false")}, \"vocab\": {{ {string.Join(", ", entries)} }},"
            + " \"merges\": [] } }";
    }

    private static BpeVocabulary Load(string json) =>
        TokenizerJsonLoader.LoadBpe(new MemoryStream(Encoding.UTF8.GetBytes(json)), OracleReplay.BpeBounds());
}
