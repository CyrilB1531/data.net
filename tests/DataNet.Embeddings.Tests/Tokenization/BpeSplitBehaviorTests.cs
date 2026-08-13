using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_split_behavior.json</c>: twenty models, one per
/// <c>behavior</c> and <c>invert</c> combination, each over two patterns.
/// </summary>
public sealed class BpeSplitBehaviorTests
{
    private const string Corpus = "bpe_split_behavior.json";

    private static string[] Split(BpeSplitStep step, string text)
    {
        List<string> pieces = [];
        new BpePreTokenizer(step, null).Split(text, pieces);
        return [.. pieces];
    }

    /// <summary>
    /// One piece through the byte-level alphabet, which is what the corpus
    /// recorded: <c>pre_tokenize_str</c> returns the pieces after the
    /// <c>ByteLevel</c> step mapped them, while
    /// <see cref="BpePreTokenizer.Split"/> runs before any mapping.
    /// </summary>
    private static string ByteMapped(string piece) =>
        new([.. Encoding.UTF8.GetBytes(piece).Select(ByteLevelAlphabet.ToChar)]);

    /// <summary>
    /// Every piece the corpus recorded, for every one of the twenty models. The
    /// corpus is the authority: where this disagrees, the code is wrong.
    /// </summary>
    [Fact]
    public void Every_recorded_piece_is_reproduced()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        JsonElement models = doc.RootElement.GetProperty("metadata").GetProperty("models");
        int checkedCases = 0;

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            JsonElement model = models.GetProperty(c.GetProperty("model").GetString()!);
            var step = new BpeSplitStep(
                model.GetProperty("pattern").GetString()!,
                Behavior(model.GetProperty("behavior").GetString()!),
                model.GetProperty("invert").GetBoolean());

            string[] expected = [.. c.GetProperty("pieces").EnumerateArray().Select(p => p.GetString()!)];
            string[] actual = [.. Split(step, c.GetProperty("text").GetString()!).Select(ByteMapped)];

            Assert.Equal(expected, actual);
            checkedCases++;
        }

        Assert.True(checkedCases > 0, $"{Corpus} carries no case.");
    }

    /// <summary>
    /// The rules, pinned as literals rather than only replayed — so a corpus
    /// that stopped carrying a distinction fails here rather than silently
    /// asserting less.
    /// </summary>
    [Theory]
    [InlineData(SplitBehavior.Isolated, false, new[] { "ab", " ", "cd", "!" })]
    [InlineData(SplitBehavior.Isolated, true, new[] { "ab", " ", "cd", "!" })]
    [InlineData(SplitBehavior.Removed, false, new[] { " ", "!" })]
    [InlineData(SplitBehavior.Removed, true, new[] { "ab", "cd" })]
    [InlineData(SplitBehavior.MergedWithPrevious, false, new[] { "ab", " cd", "!" })]
    [InlineData(SplitBehavior.MergedWithPrevious, true, new[] { "ab ", "cd!" })]
    [InlineData(SplitBehavior.MergedWithNext, false, new[] { "ab ", "cd!" })]
    [InlineData(SplitBehavior.MergedWithNext, true, new[] { "ab", " cd", "!" })]
    [InlineData(SplitBehavior.Contiguous, false, new[] { "ab", " ", "cd", "!" })]
    [InlineData(SplitBehavior.Contiguous, true, new[] { "ab", " ", "cd", "!" })]
    public void The_ten_combinations(SplitBehavior behavior, bool invert, string[] expected)
    {
        Assert.Equal(expected, Split(new BpeSplitStep(@"\w+", behavior, invert), "ab cd!"));
    }

    /// <summary>
    /// The one shape that tells <c>Isolated</c> from <c>Contiguous</c>: two
    /// adjacent matches. Everything else in the corpus agrees on them.
    /// </summary>
    [Theory]
    [InlineData(SplitBehavior.Isolated, new[] { "a", "X", "X", "b" })]
    [InlineData(SplitBehavior.Contiguous, new[] { "a", "XX", "b" })]
    public void Adjacent_matches_are_where_isolated_and_contiguous_part(
        SplitBehavior behavior, string[] expected)
    {
        Assert.Equal(expected, Split(new BpeSplitStep("X", behavior, Invert: false), "aXXb"));
    }

    /// <summary>Empty pieces are dropped, at every boundary the corpus carries.</summary>
    [Theory]
    [InlineData("", new string[0])]
    [InlineData("abc", new[] { "abc" })]
    [InlineData("  ", new[] { "  " })]
    public void Empty_pieces_are_dropped(string text, string[] expected)
    {
        Assert.Equal(expected, Split(new BpeSplitStep(@"\w+", SplitBehavior.Isolated, Invert: false), text));
    }

    private static SplitBehavior Behavior(string name) =>
        name switch
        {
            "isolated" => SplitBehavior.Isolated,
            "removed" => SplitBehavior.Removed,
            "merged_with_previous" => SplitBehavior.MergedWithPrevious,
            "merged_with_next" => SplitBehavior.MergedWithNext,
            "contiguous" => SplitBehavior.Contiguous,
            _ => throw new Xunit.Sdk.XunitException($"{Corpus} carries an unknown behavior '{name}'."),
        };
}
