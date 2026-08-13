using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// The pre-tokenizer's own output, which is where issue #143's defect lived.
/// <see cref="BpePreTokenizer"/> is internal and the test project has
/// <c>InternalsVisibleTo</c>, so the cascade is asserted directly rather than
/// inferred from tokens.
/// </summary>
public sealed class BpePreTokenizerTests
{
    private const string Corpus = "bpe_sequence_split.json";

    private static string[] Split(BpePreTokenizer pre, string text)
    {
        List<string> pieces = [];
        pre.Split(text, pieces);
        return [.. pieces];
    }

    /// <summary>
    /// Both patterns null is the classic word-boundary split, unchanged: this is
    /// the default a hand-built <see cref="BpeVocabulary"/> gets, and every
    /// classic-lineage model already relies on it.
    /// </summary>
    [Fact]
    public void Both_null_is_still_the_word_boundary_split()
    {
        Assert.Equal(["world", "!"], Split(new BpePreTokenizer(null, null), "world!"));
    }

    /// <summary>One pattern and no pre-split is what a bare <c>ByteLevel</c> declares.</summary>
    [Fact]
    public void A_pattern_alone_is_the_only_split()
    {
        Assert.Equal(["hello", "123"], Split(new BpePreTokenizer(null, BpePatterns.Gpt2), "hello123"));
    }

    /// <summary>
    /// A pre-split alone is a <c>Sequence</c> whose <c>ByteLevel</c> step has
    /// <c>use_regex</c> off — the pieces the <c>Split</c> step produced, untouched.
    /// Llama-3's own pattern already separates letters from digits, so
    /// "untouched" here still means two pieces, not one: this is
    /// corpus case 17 (<c>use_regex_off</c>, <c>"hello123 don't"</c>), whose
    /// recorded pieces start <c>['hello', '123', ...]</c> rather than fusing
    /// the run.
    /// </summary>
    [Fact]
    public void A_pre_split_alone_is_the_only_split()
    {
        Assert.Equal(["hello", "123"], Split(new BpePreTokenizer(BpePatterns.Llama3, null), "hello123"));
    }

    /// <summary>
    /// Both, in order: the second pattern re-splits every piece the first
    /// produced. This is the case that was missing — Llama-3's pattern keeps
    /// <c>'ai</c> whole and GPT-2's, which knows only the seven English
    /// contractions, breaks it.
    /// </summary>
    [Fact]
    public void Both_run_in_order_and_the_second_re_splits_the_first_s_pieces()
    {
        var pre = new BpePreTokenizer(BpePatterns.Llama3, BpePatterns.Gpt2);

        Assert.Equal(["j", "'", "ai"], Split(pre, "j'ai"));
        Assert.Equal(["hello", "123"], Split(pre, "hello123"));
    }

    /// <summary>
    /// The order is not symmetric, so a swap has to be visible. <c>j'ai</c>
    /// will not do here -- both orders land on <c>["j", "'", "ai"]</c> for it,
    /// because GPT-2 alone already isolates the apostrophe and Llama-3 then
    /// leaves that isolated piece alone. <c>'Tis</c> does separate the two
    /// orders: Llama-3's contraction list is case-insensitive, so run first it
    /// consumes <c>'T</c> as the <c>'t</c> contraction, leaving GPT-2 (whose
    /// list is case-sensitive) to split that into <c>'</c> and <c>T</c> --
    /// <c>["'", "T", "is"]</c>. Run second, GPT-2 never gets the chance: alone
    /// it already isolates the apostrophe from <c>Tis</c>, and Llama-3 applied
    /// to that lone apostrophe has no contraction to find -- <c>["'", "Tis"]</c>.
    /// Verified directly with <c>Regex.Matches</c> against both patterns, not
    /// through this class, so the example is checked independently of the code
    /// under test.
    /// </summary>
    [Fact]
    public void The_order_matters()
    {
        Assert.NotEqual(
            Split(new BpePreTokenizer(BpePatterns.Llama3, BpePatterns.Gpt2), "'Tis"),
            Split(new BpePreTokenizer(BpePatterns.Gpt2, BpePatterns.Llama3), "'Tis"));
    }

    /// <summary>
    /// Every piece the corpus recorded, replayed through the cascade the file
    /// shape implies — the frozen reference rather than this file's own idea of
    /// what the patterns do.
    /// </summary>
    [Theory]
    [InlineData("use_regex_on", true)]
    [InlineData("use_regex_off", false)]
    public void Every_recorded_piece_is_reproduced(string model, bool secondSplit)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        var pre = new BpePreTokenizer(BpePatterns.Llama3, secondSplit ? BpePatterns.Gpt2 : null);
        int checkedCases = 0;

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (c.GetProperty("model").GetString() != model)
            {
                continue;
            }
            checkedCases++;
            string text = c.GetProperty("text").GetString()!;
            string[] expected = [.. c.GetProperty("pieces").EnumerateArray().Select(p => p.GetString()!)];
            // The corpus records byte-mapped pieces; the split runs before the
            // mapping, so the comparison is over the mapping of what we produce.
            // ToArray() rather than a collection expression: with a named
            // string[] on the left, a collection expression on the right makes
            // Assert.Equal<T>(T, T) and Assert.Equal<T>(ReadOnlySpan<T>,
            // ReadOnlySpan<T>) equally applicable (CS0121). A concrete array on
            // both sides removes the ambiguity without changing what is compared.
            Assert.Equal(expected, Split(pre, text).Select(ByteMapped).ToArray());
        }

        Assert.True(checkedCases > 0, $"{Corpus} carries no case for {model}.");
    }

    /// <summary>
    /// One piece through the byte-level alphabet, which is what the corpus
    /// recorded: <c>pre_tokenize_str</c> returns the pieces after the
    /// <c>ByteLevel</c> step has mapped them, while
    /// <see cref="BpePreTokenizer.Split"/> runs before any mapping.
    /// </summary>
    private static string ByteMapped(string piece) =>
        new([.. Encoding.UTF8.GetBytes(piece).Select(ByteLevelAlphabet.ToChar)]);
}
