using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>
/// The absolute bit pin ADR 0018's Consequences require: every multiclass corpus
/// value against the raw IEEE-754 bits this implementation produced, because a
/// relative pin alone missed a real regression — reassociating the division in
/// <c>MultiClassRoc.Mean</c>/<c>WeightedMean</c> passed every other test while
/// moving the last bit of three of the twelve values below. The oracle cannot
/// replace this either: <c>tests/oracles/roc_auc.json</c> rounds to 12 decimals,
/// below where these bits move. See
/// <c>docs/decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md</c>.
/// </summary>
public sealed class RocAucFrozenBitsTests
{
    /// <summary>
    /// <c>BitConverter.DoubleToInt64Bits</c> of every multiclass corpus value,
    /// keyed by <c>RocCorpus.Describe</c> plus the <c>values</c> key, so a failure
    /// names the case and the strategy/averaging pair rather than an index.
    /// </summary>
    private static readonly Dictionary<string, long> FrozenBits = new(StringComparer.Ordinal)
    {
        // multiclass_3, k=3, n=240, no sample weights.
        ["multiclass_3 (weighted=False) ovr|macro"] = 0x3FED8BFFD9A439E9,
        ["multiclass_3 (weighted=False) ovr|weighted"] = 0x3FED8B3ED74F9193,
        ["multiclass_3 (weighted=False) ovo|macro"] = 0x3FED8C2B011DCC9D,
        ["multiclass_3 (weighted=False) ovo|weighted"] = 0x3FED8BD7533E71CC,

        // multiclass_3 with sample weights. One-vs-one is absent by design:
        // weighted one-vs-one is refused, as scikit-learn refuses it.
        ["multiclass_3 (weighted=True) ovr|macro"] = 0x3FED91866633DFBC,
        ["multiclass_3 (weighted=True) ovr|weighted"] = 0x3FED9A028C6EE1C5, // moves to …C4 under the Mean mutation

        // multiclass_5, k=5, n=400, no sample weights.
        ["multiclass_5 (weighted=False) ovr|macro"] = 0x3FED6A6AA113FF66, // moves to …67 under the Mean mutation
        ["multiclass_5 (weighted=False) ovr|weighted"] = 0x3FED6A992BCF7AFB,
        ["multiclass_5 (weighted=False) ovo|macro"] = 0x3FED6A47AF012772,
        ["multiclass_5 (weighted=False) ovo|weighted"] = 0x3FED6A6B9C6F6349, // moves to …4A under the Mean mutation

        // multiclass_5 with sample weights.
        ["multiclass_5 (weighted=True) ovr|macro"] = 0x3FEDB3CE17739D58,
        ["multiclass_5 (weighted=True) ovr|weighted"] = 0x3FEDB403E6C3EDAD,
    };

    [Theory]
    [MemberData(nameof(RocCorpus.MulticlassIndices), MemberType = typeof(RocCorpus))]
    public void Reproduces_the_frozen_bits_of_every_multiclass_corpus_value(int index)
    {
        JsonElement c = RocCorpus.Cases[index];
        int[] yTrue = RocCorpus.YTrue(c);
        double[] scores = RocCorpus.RowMajorScores(c);
        double[] weight = RocCorpus.SampleWeight(c);
        int classCount = c.GetProperty("class_count").GetInt32();

        // Every value is compared before anything is asserted: stopping at the
        // first mismatch would hide which other values the same change moved.
        var moved = new List<string>();

        foreach (JsonProperty entry in c.GetProperty("values").EnumerateObject())
        {
            string key = $"{RocCorpus.Describe(c)} {entry.Name}";
            string[] parts = entry.Name.Split('|');

            double actual = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
            {
                Strategy = parts[0] == "ovr" ? MultiClassStrategy.OneVsRest : MultiClassStrategy.OneVsOne,
                Average = parts[1] == "macro" ? Averaging.Macro : Averaging.Weighted,
                SampleWeight = weight,
            });

            long expected = Assert.Contains(key, FrozenBits);
            long got = BitConverter.DoubleToInt64Bits(actual);

            if (expected != got)
            {
                moved.Add($"{key}: frozen 0x{expected:X16}, got 0x{got:X16} ({actual:R})");
            }
        }

        Assert.True(moved.Count == 0,
            string.Join(Environment.NewLine, moved) + Environment.NewLine
            + "The arithmetic behind these values changed. If that was intended, regenerate "
            + "the constants and say in the commit message which values moved and why.");
    }

    /// <summary>
    /// The theory above already refuses a corpus value with no constant. This
    /// closes the other direction: a constant left behind for a value the corpus
    /// no longer holds, which the theory would never walk and so never notice,
    /// leaving a pin that pins nothing. Compared as ordered sets so a failure
    /// lists both sides.
    /// </summary>
    [Fact]
    public void Pins_every_corpus_value_and_no_value_the_corpus_does_not_hold()
    {
        List<string> expected =
        [
            .. RocCorpus.Cases
                .Where(c => c.GetProperty("kind").GetString() == "multiclass")
                .SelectMany(c => c.GetProperty("values").EnumerateObject()
                    .Select(entry => $"{RocCorpus.Describe(c)} {entry.Name}"))
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(expected, [.. FrozenBits.Keys.Order(StringComparer.Ordinal)]);
    }
}
