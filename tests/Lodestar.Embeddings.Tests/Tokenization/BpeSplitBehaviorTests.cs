using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

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
        new BpePreTokenizer(step, null, false, false).Split(text, pieces);
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
        var failures = new List<string>();
        int checkedCases = 0;

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string modelName = c.GetProperty("model").GetString()!;
            JsonElement model = models.GetProperty(modelName);
            var step = new BpeSplitStep(
                model.GetProperty("pattern").GetString()!,
                Behavior(model.GetProperty("behavior").GetString()!),
                model.GetProperty("invert").GetBoolean());

            string text = c.GetProperty("text").GetString()!;
            string[] expected = [.. c.GetProperty("pieces").EnumerateArray().Select(p => p.GetString()!)];
            string[] actual = [.. Split(step, text).Select(ByteMapped)];

            if (!expected.SequenceEqual(actual))
            {
                failures.Add(
                    $"[{modelName}] \"{text}\"\n  exp: [{string.Join(", ", expected)}]\n  got: [{string.Join(", ", actual)}]");
            }
            checkedCases++;
        }

        Assert.True(checkedCases > 0, $"{Corpus} carries no case.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
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
    /// The loader reads the file's own <c>behavior</c> and <c>invert</c>, ignored entirely before issue
    /// #145 -- it took the pattern and applied <see cref="SplitBehavior.Removed"/> inverted to everything.
    /// Looped over all twenty models rather than a hand-picked few: the PascalCase switch in
    /// <c>ReadBpeSequencePreTokenizer</c> has five arms, and <see cref="The_ten_combinations"/> never goes
    /// through the loader at all, so a wrong mapping on any one arm -- <c>MergedWithPrevious</c> included --
    /// would otherwise go unnoticed. A hand-written subset of rows could go stale the same way -- enumerating
    /// <c>metadata.models</c> cannot.
    /// </summary>
    [Fact]
    public void The_loader_carries_the_step_every_model_declares()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        JsonElement models = doc.RootElement.GetProperty("metadata").GetProperty("models");
        var failures = new List<string>();
        int checkedModels = 0;

        foreach (JsonProperty model in models.EnumerateObject())
        {
            BpeVocabulary vocabulary = Vocabulary(model.Name);
            SplitBehavior expectedBehavior = Behavior(model.Value.GetProperty("behavior").GetString()!);
            bool expectedInvert = model.Value.GetProperty("invert").GetBoolean();

            if (vocabulary.PreSplit is null)
            {
                failures.Add($"[{model.Name}] PreSplit is null, expected {expectedBehavior} invert={expectedInvert}");
            }
            else if (vocabulary.PreSplit.Behavior != expectedBehavior || vocabulary.PreSplit.Invert != expectedInvert)
            {
                failures.Add(
                    $"[{model.Name}] expected {expectedBehavior} invert={expectedInvert}, " +
                    $"got {vocabulary.PreSplit.Behavior} invert={vocabulary.PreSplit.Invert}");
            }
            checkedModels++;
        }

        Assert.True(checkedModels > 0, $"{Corpus} carries no model.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// All 120 recorded cases, end to end through the loader and the merge loop —
    /// not just the pre-tokenizer's pieces. The corpus already carries
    /// <c>tokens</c> and <c>ids</c> per case; this is the shape
    /// <see cref="BpeSequenceSplitTests"/> uses for the same replay.
    /// </summary>
    [Fact]
    public void Every_model_is_reproduced_end_to_end()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        JsonElement models = doc.RootElement.GetProperty("metadata").GetProperty("models");
        int checkedModels = 0;

        foreach (JsonProperty model in models.EnumerateObject())
        {
            checkedModels++;
            var tokenizer = new BpeTokenizer(Vocabulary(model.Name));
            OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens", model.Name, nameProperty: "model");
        }

        Assert.True(checkedModels > 0, $"{Corpus} carries no model.");
    }

    /// <summary>
    /// End to end: the arrangement the file declares reaches the merge loop. <c>Isolated</c> keeps the
    /// space and the <c>!</c> that the old rule dropped. The array is taken from the corpus's own
    /// <c>isolated</c> case for <c>"ab cd!"</c> (case id 0), not a prediction: that model's vocabulary has
    /// no merges and no <c>"ab"</c>/<c>"cd"</c> entries, so <see cref="BpeTokenizer.Encode(string)"/> emits
    /// one token per byte -- <c>["a", "b", "Ġ", "c", "d", "!"]</c> -- rather than the whole pieces
    /// <c>["ab", "Ġ", "cd", "!"]</c> that <c>pre_tokenize_str</c> alone would show.
    /// </summary>
    [Fact]
    public void An_isolated_split_keeps_the_text_between_the_matches()
    {
        var tokenizer = new BpeTokenizer(Vocabulary("isolated"));

        Assert.Equal(["a", "b", "Ġ", "c", "d", "!"], tokenizer.Encode("ab cd!").Tokens);
    }

    /// <summary>
    /// Three shapes the reference refuses, cited against the reference rather than
    /// against this repository's word: the corpus carries the document it was
    /// handed and the error it answered with. Each row asserts on a substring
    /// distinguishing enough to prove <em>its own</em> branch fired — "Split"
    /// alone would pass for any of the three, including the unrelated "is a
    /// Sequence that is not exactly [Split, ByteLevel]" refusal, and would not
    /// tell three distinct messages from one repeated.
    /// </summary>
    [Theory]
    [InlineData("behavior_absent", "declares no behavior")]
    [InlineData("invert_absent", "declares no invert")]
    [InlineData("behavior_unknown", "Nonsense")]
    public void The_reference_refuses_it_too_and_so_do_we(string shape, string distinguishing)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        JsonElement refusal = doc.RootElement.GetProperty("metadata").GetProperty("refusals")
            .EnumerateArray().Single(r => r.GetProperty("shape").GetString() == shape);
        Assert.NotEmpty(refusal.GetProperty("error").GetString()!);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(
                Bytes(refusal.GetProperty("document").GetString()!), OracleReplay.BpeBounds()));
        Assert.Contains(distinguishing, error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The hand-built path, which no loader guards: <c>SplitBehavior</c> is a public
    /// enum on a public record, so a value outside its five members can only reach
    /// <see cref="BpeVocabulary.PreSplit"/> by construction, not through
    /// <see cref="TokenizerJsonLoader"/>. Refused at the <see cref="BpeTokenizer"/>
    /// constructor, naming the vocabulary, rather than surfacing later from inside
    /// <c>Encode</c> as an <see cref="ArgumentOutOfRangeException"/> naming an
    /// internal parameter no caller of the constructor can see.
    /// </summary>
    [Fact]
    public void The_constructor_refuses_an_undefined_split_behavior()
    {
        var vocabulary = new BpeVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 0, ["b"] = 1 },
            [])
        {
            PreSplit = new BpeSplitStep(@"\w+", (SplitBehavior)99, Invert: false),
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() => new BpeTokenizer(vocabulary));

        Assert.Contains("SplitBehavior", error.Message, StringComparison.Ordinal);
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
