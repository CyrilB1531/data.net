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
        if (denominator != 0.0)
        {
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
    /// Row sums of the matrix: the support of each requested class, counted
    /// against every observed label, not only the other requested ones —
    /// scikit-learn's <c>true_sum</c>. Only when an explicit label subset left
    /// some observed label out (<see cref="ConfusionMatrix.DroppedSamples"/>)
    /// does this differ from summing across just the requested labels: that
    /// left-out label's cells still land in <see cref="ConfusionMatrix.Cells"/>,
    /// at columns/rows beyond <see cref="ConfusionMatrix.Size"/>, and belong in
    /// this sum the same as a requested one would.
    /// </summary>
    public static double[] Support(ConfusionMatrix cm)
    {
        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] support = new double[k];
        for (int row = 0; row < k; row++)
        {
            double sum = 0.0;
            int offset = row * stride;
            for (int col = 0; col < stride; col++)
            {
                sum += cells[offset + col];
            }
            support[row] = sum;
        }
        return support;
    }

    /// <summary>
    /// Column sums: how much weight was predicted into each requested class,
    /// again counted against every observed label — scikit-learn's
    /// <c>pred_sum</c>. See <see cref="Support"/> for why the sum runs over
    /// <see cref="ConfusionMatrix.Stride"/> rows rather than just the requested
    /// <see cref="ConfusionMatrix.Size"/>.
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

    public static double[] PerClass(ConfusionMatrix cm, PrfMetric metric, double beta, ZeroDivision zeroDivision)
    {
        double[] tp = TruePositives(cm);
        double[] predicted = PredictedSum(cm);
        double[] support = Support(cm);
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

        double[] perClass = PerClass(cm, metric, beta, zeroDivision);

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
                double[] support = Support(cm);
                double weightSum = 0.0;
                double weighted = 0.0;
                for (int i = 0; i < perClass.Length; i++)
                {
                    weighted += perClass[i] * support[i];
                    weightSum += support[i];
                }
                // scikit-learn returns 0.0 rather than dividing by zero here.
                return weightSum == 0.0 ? 0.0 : weighted / weightSum;

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
        if (beta == 0.0)
        {
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
