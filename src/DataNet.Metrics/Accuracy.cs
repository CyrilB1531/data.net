namespace DataNet.Metrics;

/// <summary>
/// Plain agreement between truth and prediction — the equivalent of
/// <c>sklearn.metrics.accuracy_score</c>.
/// </summary>
public static class Accuracy
{
    /// <summary>
    /// The fraction of correctly predicted samples —
    /// <c>sklearn.metrics.accuracy_score(y_true, y_pred, normalize=…, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="normalize">When true (the default) return the fraction; when false, the weight of the correct samples.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length or are empty.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        if (yTrue.Length != yPred.Length)
        {
            throw new ArgumentException(
                $"yTrue has {yTrue.Length} entries and yPred has {yPred.Length}; they must agree.",
                nameof(yPred));
        }
        if (yTrue.Length == 0)
        {
            throw new ArgumentException("yTrue and yPred are empty; there is nothing to score.", nameof(yTrue));
        }
        if (!sampleWeight.IsEmpty && sampleWeight.Length != yTrue.Length)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {yTrue.Length} samples.",
                nameof(sampleWeight));
        }

        bool weighted = !sampleWeight.IsEmpty;
        double correct = 0.0;
        double total = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = weighted ? sampleWeight[i] : 1.0;
            if (yTrue[i] == yPred[i])
            {
                correct += weight;
            }
            total += weight;
        }

        return normalize ? correct / total : correct;
    }

    /// <summary>
    /// The same number read off an already-computed matrix: the weight on the
    /// diagonal over the total.
    /// </summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="normalize">When true (the default) return the fraction; when false, the weight on the diagonal.</param>
    /// <remarks>
    /// This is accuracy over the samples the matrix <em>kept</em>. A matrix built
    /// with an explicit label subset drops the samples outside it, so the result
    /// then differs from <c>accuracy_score</c>, which scores every sample.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    public static double Score(ConfusionMatrix cm, bool normalize = true)
    {
        Guard.NotNull(cm);

        int k = cm.Size;
        ReadOnlySpan<double> cells = cm.Cells;
        double diagonal = 0.0;
        for (int i = 0; i < k; i++)
        {
            diagonal += cells[(i * k) + i];
        }

        return normalize ? diagonal / cm.TotalWeight : diagonal;
    }
}
