using System.Collections.ObjectModel;
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The per-class table people actually read — the equivalent of
/// <c>sklearn.metrics.classification_report</c>.
/// </summary>
/// <remarks>
/// A class rather than a record: its rows live in a list, over which a
/// synthesised equality would compare references, and bit-exact equality over
/// computed <see cref="double"/> values would be misleading even if they did not.
/// </remarks>
public sealed class ClassificationReport
{
    private ClassificationReport(
        ReadOnlyCollection<ClassRow> classes,
        double accuracy,
        AverageRow macro,
        AverageRow weighted,
        AverageRow? micro,
        double totalSupport,
        bool isWeighted)
    {
        Classes = classes;
        Accuracy = accuracy;
        MacroAverage = macro;
        WeightedAverage = weighted;
        MicroAverage = micro;
        TotalSupport = totalSupport;
        IsWeighted = isWeighted;
    }

    /// <summary>One line per class, in the matrix's label order.</summary>
    public IReadOnlyList<ClassRow> Classes { get; }

    /// <summary>Accuracy over the samples the matrix counted.</summary>
    public double Accuracy { get; }

    /// <summary>The unweighted mean of the per-class scores.</summary>
    public AverageRow MacroAverage { get; }

    /// <summary>The support-weighted mean of the per-class scores.</summary>
    public AverageRow WeightedAverage { get; }

    /// <summary>
    /// The micro average, non-null exactly when an explicit label set left an
    /// observed label out — which is when scikit-learn's text prints a
    /// <c>micro avg</c> row in place of the <c>accuracy</c> row.
    /// </summary>
    public AverageRow? MicroAverage { get; }

    /// <summary>The total weight the report covers.</summary>
    public double TotalSupport { get; }

    internal bool IsWeighted { get; }

    /// <summary>
    /// Builds the report from an existing matrix —
    /// <c>classification_report(y_true, y_pred, target_names=…, zero_division=…)</c>.
    /// </summary>
    /// <param name="cm">The matrix to summarise.</param>
    /// <param name="targetNames">Readable names, one per label, in label order. The equivalent of scikit-learn's <c>target_names</c>.</param>
    /// <param name="zeroDivision">What an undefined per-class score returns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="targetNames"/> has a different length from the label set.</exception>
    public static ClassificationReport Compute(
        ConfusionMatrix cm,
        IReadOnlyList<string>? targetNames = null,
        ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);

        int k = cm.Size;
        if (targetNames is not null && targetNames.Count != k)
        {
            throw new ArgumentException(
                $"targetNames has {targetNames.Count} entries but there are {k} labels.",
                nameof(targetNames));
        }

        double[] precision = Prf.PerClass(cm, PrfMetric.Precision, 1.0, zeroDivision);
        double[] recall = Prf.PerClass(cm, PrfMetric.Recall, 1.0, zeroDivision);
        double[] f1 = Prf.PerClass(cm, PrfMetric.FScore, 1.0, zeroDivision);
        double[] support = Prf.Support(cm);

        ClassRow[] rows = new ClassRow[k];
        double totalSupport = 0.0;
        for (int i = 0; i < k; i++)
        {
            rows[i] = new ClassRow(cm.Labels[i], targetNames?[i], precision[i], recall[i], f1[i], support[i]);
            totalSupport += support[i];
        }

        AverageRow macro = new(
            "macro avg",
            Prf.Aggregate(cm, PrfMetric.Precision, 1.0, Averaging.Macro, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.Recall, 1.0, Averaging.Macro, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.FScore, 1.0, Averaging.Macro, 0, zeroDivision),
            totalSupport);

        AverageRow weighted = new(
            "weighted avg",
            Prf.Aggregate(cm, PrfMetric.Precision, 1.0, Averaging.Weighted, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.Recall, 1.0, Averaging.Weighted, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.FScore, 1.0, Averaging.Weighted, 0, zeroDivision),
            totalSupport);

        AverageRow? micro = null;
        if (cm.ExplicitLabels && cm.DroppedSamples)
        {
            micro = new AverageRow(
                "micro avg",
                Prf.Aggregate(cm, PrfMetric.Precision, 1.0, Averaging.Micro, 0, zeroDivision),
                Prf.Aggregate(cm, PrfMetric.Recall, 1.0, Averaging.Micro, 0, zeroDivision),
                Prf.Aggregate(cm, PrfMetric.FScore, 1.0, Averaging.Micro, 0, zeroDivision),
                totalSupport);
        }

        // Fully qualified on purpose: this class has an `Accuracy` property, and an
        // unqualified `Accuracy.Score(cm)` binds to it rather than to the type.
        double accuracy = DataNet.Metrics.Accuracy.Score(cm);

        return new ClassificationReport(
            Array.AsReadOnly(rows), accuracy, macro, weighted, micro, totalSupport, cm.IsWeighted);
    }

    /// <summary>Builds the report straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="targetNames">Readable names, one per label, in label order.</param>
    /// <param name="zeroDivision">What an undefined per-class score returns.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    public static ClassificationReport Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        IReadOnlyList<string>? targetNames = null,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Compute(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), targetNames, zeroDivision);

    /// <summary>
    /// Renders the table the way <c>classification_report</c> prints it, to the
    /// character.
    /// </summary>
    /// <param name="digits">Decimal places for the three score columns, as scikit-learn's <c>digits</c>.</param>
    /// <remarks>
    /// Parity is asserted for <see cref="ZeroDivision.Zero"/> and
    /// <see cref="ZeroDivision.One"/>. A report built with
    /// <see cref="ZeroDivision.NaN"/> renders .NET's <c>NaN</c> where Python
    /// writes <c>nan</c>; the numbers still match, the text does not.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is negative.</exception>
    public string ToText(int digits = 2)
    {
        Guard.NotLessThan(digits, 0);
        return ReportText.Render(this, digits);
    }

    /// <summary>The two-digit table, as <see cref="ToText"/> renders it.</summary>
    public override string ToString() => ToText();
}
