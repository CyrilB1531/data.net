using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

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
    /// A pre-split step over <paramref name="pattern"/>, with the behaviour and invert this file's #143
    /// cascade always meant: keep the regex matches, drop everything else. Not a bridge built elsewhere --
    /// a real Llama-3 file reaches <see cref="BpePreTokenizer"/> declaring <see cref="SplitBehavior.Isolated"/>,
    /// not <see cref="SplitBehavior.Removed"/> inverted. The rule lives in <see cref="BpePreTokenizer"/>'s
    /// own constructor, in the branch taken when its <c>preSplit</c> parameter is
    /// <see langword="null"/>. This helper exists only because this suite builds
    /// <see cref="BpePreTokenizer"/> directly with a non-null step, bypassing that branch.
    /// </summary>
    private static BpeSplitStep PreSplit(string pattern) => new(pattern, SplitBehavior.Removed, Invert: true);

    /// <summary>
    /// The classic word-boundary split, unchanged: <see cref="BpePatterns.Whitespace"/>
    /// is the pattern a classic-lineage model declares, and the one this class used to
    /// invent when it was handed neither pattern (issue #122).
    /// </summary>
    [Fact]
    public void The_whitespace_pattern_is_still_the_word_boundary_split()
    {
        Assert.Equal(["world", "!"], Split(new BpePreTokenizer(null, BpePatterns.Whitespace, false, false), "world!"));
    }

    /// <summary>
    /// "world!" cannot tell apart the rule this branch actually uses (<c>Removed</c>,
    /// <c>invert</c> on) from the one a naive reading of "Whitespace" might reach for
    /// (<c>Isolated</c>) -- it has no whitespace, so both drop nothing and agree. A
    /// text with a real gap does discriminate: <c>Isolated</c> would keep the space
    /// between the two words as its own piece, which the classic lineage's tokens
    /// have never contained (measured, <c>bpe.json</c>'s 16 of 20 whitespace-bearing
    /// cases). Without this, the constructor's choice was proven only two layers up,
    /// as a token-id match in <c>BpeTokenizerTests</c>.
    /// </summary>
    [Fact]
    public void The_whitespace_pattern_drops_the_gap_between_words()
    {
        Assert.Equal(["ab", "cd"], Split(new BpePreTokenizer(null, BpePatterns.Whitespace, false, false), "ab cd"));
    }

    /// <summary>
    /// The mode: no pattern at all, so the text arrives whole -- gap, punctuation and
    /// all. The same input the two cases above split three ways.
    /// </summary>
    [Fact]
    public void The_no_split_mode_produces_one_piece()
    {
        Assert.Equal(["ab cd!"], Split(new BpePreTokenizer(null, null, true, false), "ab cd!"));
    }

    /// <summary>One pattern and no pre-split is what a bare <c>ByteLevel</c> declares.</summary>
    [Fact]
    public void A_pattern_alone_is_the_only_split()
    {
        Assert.Equal(["hello", "123"], Split(new BpePreTokenizer(null, BpePatterns.Gpt2, false, false), "hello123"));
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
        Assert.Equal(["hello", "123"], Split(new BpePreTokenizer(PreSplit(BpePatterns.Llama3), null, false, false), "hello123"));
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
        var pre = new BpePreTokenizer(PreSplit(BpePatterns.Llama3), BpePatterns.Gpt2, false, false);

        Assert.Equal(["j", "'", "ai"], Split(pre, "j'ai"));
        Assert.Equal(["hello", "123"], Split(pre, "hello123"));
    }

    /// <summary>
    /// The order is not symmetric, so a swap has to be visible. <c>j'ai</c> will not do -- both orders
    /// land on <c>["j", "'", "ai"]</c>, since GPT-2 alone already isolates the apostrophe and Llama-3 then
    /// leaves it alone. <c>'Tis</c> does separate the two: Llama-3's contraction list is case-insensitive,
    /// so run first it consumes <c>'T</c> as the <c>'t</c> contraction, leaving GPT-2 (case-sensitive) to
    /// split it into <c>["'", "T", "is"]</c>. Run second, GPT-2 already isolates the apostrophe from
    /// <c>Tis</c> alone, and Llama-3 finds no contraction in that lone apostrophe --
    /// <c>["'", "Tis"]</c>. Verified directly with <c>Regex.Matches</c> against both patterns, independent of the code under test.
    /// </summary>
    [Fact]
    public void The_order_matters()
    {
        Assert.Equal(["'", "T", "is"], Split(new BpePreTokenizer(PreSplit(BpePatterns.Llama3), BpePatterns.Gpt2, false, false), "'Tis"));
        Assert.Equal(["'", "Tis"], Split(new BpePreTokenizer(PreSplit(BpePatterns.Gpt2), BpePatterns.Llama3, false, false), "'Tis"));
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
        var pre = new BpePreTokenizer(PreSplit(BpePatterns.Llama3), secondSplit ? BpePatterns.Gpt2 : null, false, false);
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
            // The corpus records byte-mapped pieces (split runs before mapping), so this compares
            // against the mapping of what we produce. ToArray() avoids Assert.Equal's CS0121 array/span overload ambiguity.
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
