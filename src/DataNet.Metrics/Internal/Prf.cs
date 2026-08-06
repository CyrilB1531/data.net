namespace DataNet.Metrics.Internal;

/// <summary>Which of the three related scores is being computed.</summary>
internal enum PrfMetric
{
    Precision,
    Recall,
    FScore,
}

/// <summary>
/// The arithmetic behind precision, recall and F-beta, kept in one place because
/// scikit-learn's zero-division and averaging rules are the whole difficulty and
/// are identical across the three.
/// </summary>
internal static class Prf
{
    /// <summary>
    /// scikit-learn's <c>_prf_divide</c>: the zero-division policy applies to a
    /// zero denominator per class, before any averaging.
    /// </summary>
    public static double Divide(double numerator, double denominator, ZeroDivision zeroDivision, string metric)
    {
        // SonarLint S1244 warns against comparing floating point for exact
        // equality, which is right for arithmetic and wrong here: this is
        // scikit-learn's own _prf_divide test, deciding between a real
        // division and the zero-division policy. A tolerance would silently
        // reroute a legitimate small-but-nonzero denominator into the
        // undefined branch and change the result.
#pragma warning disable S1244
        if (denominator != 0.0)
        {
#pragma warning restore S1244
            return numerator / denominator;
        }

        return Undefined(zeroDivision, metric);
    }

    public static double Undefined(ZeroDivision zeroDivision, string metric) => zeroDivision switch
    {
        ZeroDivision.Zero => 0.0,
        ZeroDivision.One => 1.0,
        ZeroDivision.NaN => double.NaN,
        _ => throw new UndefinedMetricException(
            $"{metric} is undefined here: no sample contributes to its denominator. "
            + "Pass ZeroDivision.Zero, One or NaN to get a value instead."),
    };

    /// <summary>
    /// The support of each requested class, counted against every observed
    /// label, not only the other requested ones — scikit-learn's <c>true_sum</c>.
    /// Only when an explicit label subset left some observed label out
    /// (<see cref="ConfusionMatrix.DroppedSamples"/>) does this differ from
    /// summing across just the requested labels: that left-out label's samples
    /// still count here the same as a requested one's would.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="ConfusionMatrix.TrueSum"/> directly rather than
    /// re-deriving it by summing <see cref="ConfusionMatrix.Cells"/>: the two are
    /// mathematically the same total, but summing already-built cells adds in a
    /// different grouping than scikit-learn's own <c>np.bincount</c> over the
    /// samples, and floating-point addition is not associative — the two orders
    /// can and do disagree in the last bit, which matters when a caller
    /// (<c>ClassificationReport.ToText</c>) prints the value verbatim.
    /// </remarks>
    public static double[] Support(ConfusionMatrix cm) => cm.TrueSum.ToArray();

    /// <summary>
    /// Column sums: how much weight was predicted into each requested class,
    /// again counted against every observed label — scikit-learn's
    /// <c>pred_sum</c>. Runs over <see cref="ConfusionMatrix.Stride"/> rows
    /// rather than just the requested <see cref="ConfusionMatrix.Size"/> for the
    /// same reason <see cref="ConfusionMatrix.Stride"/>'s own remarks give: a
    /// predicted label outside the request still belongs in a requested true
    /// label's column sum, the same as scikit-learn counts it.
    /// </summary>
    public static double[] PredictedSum(ConfusionMatrix cm)
    {
        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] predicted = new double[k];
        for (int row = 0; row < stride; row++)
        {
            int offset = row * stride;
            for (int col = 0; col < k; col++)
            {
                predicted[col] += cells[offset + col];
            }
        }
        return predicted;
    }

    /// <summary>The diagonal: correctly predicted weight per requested class.</summary>
    public static double[] TruePositives(ConfusionMatrix cm)
    {
        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] tp = new double[k];
        for (int i = 0; i < k; i++)
        {
            tp[i] = cells[(i * stride) + i];
        }
        return tp;
    }

    public static double[] PerClass(ConfusionMatrix cm, PrfMetric metric, double beta, ZeroDivision zeroDivision) =>
        PerClass(cm, metric, beta, zeroDivision, out _);

    // Same computation, plus the support it already has to build along the
    // way, out for Aggregate's Weighted branch to reuse instead of paying for
    // a second O(k*stride) pass over the matrix.
    private static double[] PerClass(
        ConfusionMatrix cm, PrfMetric metric, double beta, ZeroDivision zeroDivision, out double[] support)
    {
        double[] tp = TruePositives(cm);
        double[] predicted = PredictedSum(cm);
        support = Support(cm);
        double[] result = new double[cm.Size];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = metric switch
            {
                PrfMetric.Precision => Divide(tp[i], predicted[i], zeroDivision, "Precision"),
                PrfMetric.Recall => Divide(tp[i], support[i], zeroDivision, "Recall"),
                _ => FScore(tp[i], predicted[i], support[i], beta, zeroDivision),
            };
        }

        return result;
    }

    public static double Aggregate(
        ConfusionMatrix cm, PrfMetric metric, double beta, Averaging average, int posLabel, ZeroDivision zeroDivision)
    {
        if (average == Averaging.Micro)
        {
            return Micro(cm, metric, beta, zeroDivision);
        }

        double[] perClass = PerClass(cm, metric, beta, zeroDivision, out double[] support);

        switch (average)
        {
            case Averaging.Macro:
                double total = 0.0;
                foreach (double value in perClass)
                {
                    total += value;
                }
                return total / perClass.Length;

            case Averaging.Weighted:
                double weightSum = 0.0;
                double weighted = 0.0;
                for (int i = 0; i < perClass.Length; i++)
                {
                    weighted += perClass[i] * support[i];
                    weightSum += support[i];
                }
                // scikit-learn returns 0.0 rather than dividing by zero here.
                // SonarLint S1244 warns against comparing floating point for
                // exact equality, which is right for arithmetic and wrong
                // here: this asks whether any requested class carries
                // support at all, not whether two computed quantities are
                // close. A tolerance would treat a genuinely small total
                // support as "no support" and silently swap in the
                // zero-division answer for a well-defined weighted mean.
#pragma warning disable S1244
                return weightSum == 0.0 ? 0.0 : weighted / weightSum;
#pragma warning restore S1244

            case Averaging.Binary:
                return perClass[BinaryOrdinal(cm, posLabel)];

            default:
                throw new ArgumentOutOfRangeException(nameof(average), average, "Unknown averaging mode.");
        }
    }

    private static double Micro(ConfusionMatrix cm, PrfMetric metric, double beta, ZeroDivision zeroDivision)
    {
        double[] tp = TruePositives(cm);
        double[] predicted = PredictedSum(cm);
        double[] support = Support(cm);

        double tpSum = 0.0;
        double predictedSum = 0.0;
        double supportSum = 0.0;
        for (int i = 0; i < tp.Length; i++)
        {
            tpSum += tp[i];
            predictedSum += predicted[i];
            supportSum += support[i];
        }

        return metric switch
        {
            PrfMetric.Precision => Divide(tpSum, predictedSum, zeroDivision, "Precision"),
            PrfMetric.Recall => Divide(tpSum, supportSum, zeroDivision, "Recall"),
            _ => FScore(tpSum, predictedSum, supportSum, beta, zeroDivision),
        };
    }

    // scikit-learn derives F-beta from the raw tp/predicted/support counts, not
    // from the already-divided precision and recall: substituting P = tp/predicted
    // and R = tp/support into (1+beta^2)*P*R / (beta^2*P + R) and cancelling tp
    // leaves "score = (1 + beta^2) * tp / (predicted + beta^2 * support)". Going
    // through precision and recall first would apply the zero-division policy
    // twice — once for each of them, and once more for a denominator that looks
    // like zero but is not — and produce a different answer whenever tp is zero
    // but predicted or support is not.
    private static double FScore(double tp, double predicted, double support, double beta, ZeroDivision zeroDivision)
    {
        // SonarLint S1244 warns against comparing floating point for exact
        // equality, which does not apply here: this is not a numerical
        // guard at all. beta == 0 selects a documented, discrete behaviour —
        // scikit-learn defines fbeta_score(beta=0) as precision — the same
        // way a switch selects a case. There is no "close to zero" beta that
        // should also take this branch.
#pragma warning disable S1244
        if (beta == 0.0)
        {
#pragma warning restore S1244
            return Divide(tp, predicted, zeroDivision, "Precision");
        }

        double beta2 = beta * beta;
        double numerator = (1.0 + beta2) * tp;
        double denominator = predicted + (beta2 * support);
        return Divide(numerator, denominator, zeroDivision, "F-score");
    }

    private static int BinaryOrdinal(ConfusionMatrix cm, int posLabel)
    {
        // scikit-learn refuses average="binary" as soon as the *observed* target
        // has more than two classes. A matrix that dropped samples is exactly a
        // matrix whose label set did not cover what was observed.
        if (cm.Size > 2 || (cm.ExplicitLabels && cm.DroppedSamples))
        {
            throw new ArgumentException(
                "Averaging.Binary needs a two-class target. Use Micro, Macro or Weighted, or PerClass.",
                nameof(posLabel));
        }

        for (int i = 0; i < cm.Labels.Count; i++)
        {
            if (cm.Labels[i] == posLabel)
            {
                return i;
            }
        }

        throw new ArgumentException(
            $"posLabel {posLabel} does not occur in the data.", nameof(posLabel));
    }

    public static void ValidateBeta(double beta)
    {
        if (double.IsNaN(beta) || double.IsInfinity(beta) || beta < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(beta), beta, "beta must be a finite number greater than or equal to zero.");
        }
    }
}
