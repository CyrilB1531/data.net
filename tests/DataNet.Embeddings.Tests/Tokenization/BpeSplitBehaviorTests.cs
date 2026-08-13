using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
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

    /// <summary>
    /// The loader reads the file's own <c>behavior</c> and <c>invert</c>, which it
    /// ignored entirely before issue #145 — it took the pattern and applied
    /// <see cref="SplitBehavior.Removed"/> inverted to everything.
    /// </summary>
    [Theory]
    [InlineData("isolated", SplitBehavior.Isolated, false)]
    [InlineData("removed_inverted", SplitBehavior.Removed, true)]
    [InlineData("merged_with_next", SplitBehavior.MergedWithNext, false)]
    [InlineData("contiguous_adjacent", SplitBehavior.Contiguous, false)]
    public void The_loader_carries_the_step_the_file_declares(
        string model, SplitBehavior behavior, bool invert)
    {
        BpeVocabulary vocabulary = Vocabulary(model);

        Assert.NotNull(vocabulary.PreSplit);
        Assert.Equal(behavior, vocabulary.PreSplit.Behavior);
        Assert.Equal(invert, vocabulary.PreSplit.Invert);
    }

    /// <summary>
    /// End to end: the arrangement the file declares reaches the merge loop.
    /// <c>Isolated</c> keeps the space and the <c>!</c> that the old rule dropped.
    /// </summary>
    /// <remarks>
    /// The array is taken from the corpus's own <c>isolated</c> case for
    /// <c>"ab cd!"</c> (case id 0), not from a prediction: that model's vocabulary
    /// has no merges and no <c>"ab"</c>/<c>"cd"</c> entries, so
    /// <see cref="BpeTokenizer.Encode(string)"/> emits one token per byte —
    /// <c>["a", "b", "Ġ", "c", "d", "!"]</c> — rather than the whole pieces
    /// <c>["ab", "Ġ", "cd", "!"]</c> that <c>pre_tokenize_str</c> alone would show.
    /// </remarks>
    [Fact]
    public void An_isolated_split_keeps_the_text_between_the_matches()
    {
        var tokenizer = new BpeTokenizer(Vocabulary("isolated"));

        Assert.Equal(["a", "b", "Ġ", "c", "d", "!"], tokenizer.Encode("ab cd!").Tokens);
    }

    /// <summary>
    /// Three shapes the reference refuses, cited against the reference rather than
    /// against this repository's word: the corpus carries the document it was
    /// handed and the error it answered with.
    /// </summary>
    [Theory]
    [InlineData("behavior_absent")]
    [InlineData("invert_absent")]
    [InlineData("behavior_unknown")]
    public void The_reference_refuses_it_too_and_so_do_we(string shape)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        JsonElement refusal = doc.RootElement.GetProperty("metadata").GetProperty("refusals")
            .EnumerateArray().Single(r => r.GetProperty("shape").GetString() == shape);
        Assert.NotEmpty(refusal.GetProperty("error").GetString()!);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(
                Bytes(refusal.GetProperty("document").GetString()!), OracleReplay.BpeBounds()));
        Assert.Contains("Split", error.Message, StringComparison.Ordinal);
    }

    private static BpeVocabulary Vocabulary(string model)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        string json = doc.RootElement.GetProperty("metadata").GetProperty("models")
            .GetProperty(model).GetProperty("tokenizer_json").GetString()!;
        return TokenizerJsonLoader.LoadBpe(Bytes(json), OracleReplay.BpeBounds());
    }

    private static MemoryStream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));
}
