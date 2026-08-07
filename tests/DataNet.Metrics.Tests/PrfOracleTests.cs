using System.Globalization;
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class PrfOracleTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_precision_recall_fscore_support(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        int[] yPred = MetricsCorpus.Ints(c, "y_pred");
        int[] labels = MetricsCorpus.OptionalInts(c, "labels");
        double[] sampleWeight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight);
        int posLabel = c.GetProperty("pos_label").GetInt32();

        foreach (JsonProperty entry in c.GetProperty("averaged").EnumerateObject())
        {
            (Averaging average, ZeroDivision zero) = ParseKey(entry.Name);
            string key = $"{what} {entry.Name}";

            Assert.Equal(entry.Value.GetProperty("precision").GetDouble(),
                Precision.Score(cm, average, posLabel, zero), MetricsCorpus.Tolerance);

            double recall = Recall.Score(cm, average, posLabel, zero);
            Assert.Equal(entry.Value.GetProperty("recall").GetDouble(), recall, MetricsCorpus.Tolerance);
            // The span overload has no arithmetic of its own: it only has to
            // build the same ConfusionMatrix and forward to the overload
            // above, already checked against the oracle. Agreement here is
            // what would catch a dropped labels/sampleWeight or a swapped
            // argument, which no amount of testing the matrix overload alone
            // would ever see.
            Assert.Equal(recall,
                Recall.Score(yTrue, yPred, average, posLabel, zero, labels, sampleWeight),
                MetricsCorpus.Tolerance);

            Assert.True(
                Math.Abs(entry.Value.GetProperty("f1").GetDouble()
                         - F1.Score(cm, average, posLabel, zero)) < MetricsCorpus.Tolerance,
                $"{key}: f1 diverged");
        }

        foreach (JsonProperty entry in c.GetProperty("per_class").EnumerateObject())
        {
            ZeroDivision zero = ParseZeroDivision(entry.Name);
            AssertSequence(entry.Value, "precision", Precision.PerClass(cm, zero), what);

            AssertSequence(entry.Value, "recall", Recall.PerClass(cm, zero), what);
            // Straight against the oracle, not merely against the line above:
            // this is the span overload, built from a fresh ConfusionMatrix,
            // so it also exercises that Recall.PerClass forwards
            // PrfMetric.Recall (not, say, Precision) all the way through.
            AssertSequence(entry.Value, "recall",
                Recall.PerClass(yTrue, yPred, zero, labels, sampleWeight), what);

            AssertSequence(entry.Value, "f1", F1.PerClass(cm, zero), what);
            AssertSequence(entry.Value, "f1",
                F1.PerClass(yTrue, yPred, zero, labels, sampleWeight), what);

            // FBeta.PerClass has no dedicated oracle field, but it is defined
            // to equal F1 at beta=1 and precision at beta=0 (Prf.FScore's own
            // beta==0 special case) — two points anchored to real oracle
            // arrays, not hand-computed, and far enough apart that a
            // hardcoded beta (e.g. PerClass silently always scoring beta=1)
            // would fail the second one.
            AssertSequence(entry.Value, "f1", FBeta.PerClass(cm, 1.0, zero), what);
            AssertSequence(entry.Value, "precision", FBeta.PerClass(cm, 0.0, zero), what);
            AssertSequence(entry.Value, "precision",
                FBeta.PerClass(yTrue, yPred, 0.0, zero, labels, sampleWeight), what);

            AssertSequence(entry.Value, "support", Support(c, cm), what);
        }

        foreach (JsonProperty entry in c.GetProperty("fbeta").EnumerateObject())
        {
            string[] parts = entry.Name.Split('|');
            double beta = double.Parse(parts[0], CultureInfo.InvariantCulture);
            Averaging average = ParseAveraging(parts[1]);
            ZeroDivision zero = ParseZeroDivision(parts[2]);

            Assert.Equal(entry.Value.GetDouble(),
                FBeta.Score(cm, beta, average, posLabel, zero), MetricsCorpus.Tolerance);
        }
    }

    // Support is scikit-learn's true_sum: the total weight of each requested
    // label across the whole sample, regardless of what was predicted for it.
    // It is computed here straight from y_true rather than from the matrix's
    // public view, which — like sklearn's own confusion_matrix(labels=…) —
    // legitimately still drops a sample whose predicted label falls outside
    // the requested set; support must not. That keeps this an independent
    // check against the oracle rather than a replay of production code.
    private static double[] Support(JsonElement c, ConfusionMatrix cm)
    {
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        double[] sampleWeight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
        bool weighted = sampleWeight.Length > 0;

        var ordinal = new Dictionary<int, int>();
        for (int i = 0; i < cm.Labels.Count; i++)
        {
            ordinal[cm.Labels[i]] = i;
        }

        double[] support = new double[cm.Labels.Count];
        for (int i = 0; i < yTrue.Length; i++)
        {
            if (ordinal.TryGetValue(yTrue[i], out int index))
            {
                support[index] += weighted ? sampleWeight[i] : 1.0;
            }
        }
        return support;
    }

    private static void AssertSequence(JsonElement expected, string name, double[] actual, string what)
    {
        double[] want = MetricsCorpus.Doubles(expected, name);
        Assert.Equal(want.Length, actual.Length);
        for (int i = 0; i < want.Length; i++)
        {
            Assert.True(Math.Abs(want[i] - actual[i]) < MetricsCorpus.Tolerance,
                $"{what}: {name}[{i}] expected {want[i]}, got {actual[i]}");
        }
    }

    private static (Averaging, ZeroDivision) ParseKey(string key)
    {
        string[] parts = key.Split('|');
        return (ParseAveraging(parts[0]), ParseZeroDivision(parts[1]));
    }

    private static Averaging ParseAveraging(string name) => name switch
    {
        "micro" => Averaging.Micro,
        "macro" => Averaging.Macro,
        "weighted" => Averaging.Weighted,
        "binary" => Averaging.Binary,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown averaging in the corpus."),
    };

    private static ZeroDivision ParseZeroDivision(string name) => name switch
    {
        "0" => ZeroDivision.Zero,
        "1" => ZeroDivision.One,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown zero_division in the corpus."),
    };
}
