namespace DataNet.Metrics.Internal;

/// <summary>
/// Multiclass ROC-AUC by reduction to binary problems — scikit-learn's
/// <c>multi_class="ovr"</c> and <c>multi_class="ovo"</c>.
/// </summary>
internal static class MultiClassRoc
{
    // NumPy's allclose defaults, which is the comparison sklearn makes.
    private const double RelativeTolerance = 1e-5;
    private const double AbsoluteTolerance = 1e-8;

    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        MultiClassStrategy strategy,
        Averaging average,
        ReadOnlySpan<int> labels,
        ReadOnlySpan<double> sampleWeight)
    {
        int n = Validate(yTrue, yScore, classCount, strategy, average, sampleWeight);
        int[] classes = ResolveLabels(yTrue, labels, classCount);
        ValidateRowSums(yScore, n, classCount);

        return strategy == MultiClassStrategy.OneVsRest
            ? OneVsRest(yTrue, yScore, classes, average, sampleWeight)
            : OneVsOne(yTrue, yScore, classes, average);
    }

    private static int Validate(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount,
        MultiClassStrategy strategy, Averaging average, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
        if (n == 0)
        {
            throw new ArgumentException("yTrue is empty; there is nothing to score.", nameof(yTrue));
        }
        if (classCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(classCount), classCount, "Multiclass ROC AUC needs at least two classes.");
        }
        if (yScore.Length != (long)n * classCount)
        {
            throw new ArgumentException(
                $"yScore has {yScore.Length} entries; {n} samples over {classCount} classes needs {(long)n * classCount}.",
                nameof(yScore));
        }
        if (average is not (Averaging.Macro or Averaging.Weighted))
        {
            throw new ArgumentException(
                "Multiclass ROC AUC accepts only Averaging.Macro and Averaging.Weighted, as scikit-learn does.",
                nameof(average));
        }
        if (!sampleWeight.IsEmpty)
        {
            if (sampleWeight.Length != n)
            {
                throw new ArgumentException(
                    $"sampleWeight has {sampleWeight.Length} entries but there are {n} samples.",
                    nameof(sampleWeight));
            }
            if (strategy == MultiClassStrategy.OneVsOne)
            {
                throw new ArgumentException(
                    "scikit-learn does not support sampleWeight for one-vs-one ROC AUC, and neither does this.",
                    nameof(sampleWeight));
            }
        }

        return n;
    }

    private static int[] ResolveLabels(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> labels, int classCount)
    {
        if (labels.IsEmpty)
        {
            var seen = new SortedSet<int>();
            foreach (int label in yTrue)
            {
                seen.Add(label);
            }
            if (seen.Count != classCount)
            {
                throw new ArgumentException(
                    $"yTrue holds {seen.Count} distinct labels but classCount is {classCount}. "
                    + "Pass labels when a class is absent from yTrue.",
                    nameof(classCount));
            }
            int[] resolved = new int[seen.Count];
            seen.CopyTo(resolved);
            return resolved;
        }

        if (labels.Length != classCount)
        {
            throw new ArgumentException(
                $"labels has {labels.Length} entries but classCount is {classCount}.", nameof(labels));
        }
        for (int i = 1; i < labels.Length; i++)
        {
            if (labels[i] <= labels[i - 1])
            {
                throw new ArgumentException(
                    "labels must be sorted ascending and unique for multiclass ROC AUC, as scikit-learn requires.",
                    nameof(labels));
            }
        }
        return labels.ToArray();
    }

    private static void ValidateRowSums(ReadOnlySpan<double> yScore, int n, int classCount)
    {
        for (int i = 0; i < n; i++)
        {
            double sum = 0.0;
            int offset = i * classCount;
            for (int c = 0; c < classCount; c++)
            {
                sum += yScore[offset + c];
            }

            if (Math.Abs(sum - 1.0) > AbsoluteTolerance + (RelativeTolerance * Math.Abs(sum)))
            {
                throw new ArgumentException(
                    $"yScore row {i} sums to {sum}; multiclass ROC AUC needs probabilities that sum to 1.",
                    nameof(yScore));
            }
        }
    }

    private static double OneVsRest(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes,
        Averaging average, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        int[] binary = new int[n];
        double[] column = new double[n];
        double[] scores = new double[k];
        double[] weights = new double[k];
        bool weighted = !sampleWeight.IsEmpty;

        for (int c = 0; c < k; c++)
        {
            double positiveWeight = 0.0;
            for (int i = 0; i < n; i++)
            {
                bool positive = yTrue[i] == classes[c];
                binary[i] = positive ? 1 : 0;
                column[i] = yScore[(i * k) + c];
                if (positive)
                {
                    positiveWeight += weighted ? sampleWeight[i] : 1.0;
                }
            }

            scores[c] = BinaryRoc.Score(binary, column, 1, sampleWeight);
            weights[c] = positiveWeight;
        }

        return average == Averaging.Macro ? Mean(scores) : WeightedMean(scores, weights);
    }

    /// <summary>
    /// The part of a one-vs-one pair score that stays the same across every
    /// call in the pair loop, so that <see cref="PairScore"/> takes the four
    /// values that actually vary per call rather than threading all ten
    /// through every invocation.
    /// </summary>
    private readonly ref struct PairContext(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, int classCount,
        int[] binary, double[] column)
    {
        public ReadOnlySpan<int> YTrue { get; } = yTrue;

        public ReadOnlySpan<double> YScore { get; } = yScore;

        public int[] Classes { get; } = classes;

        public int ClassCount { get; } = classCount;

        public int[] Binary { get; } = binary;

        public double[] Column { get; } = column;
    }

    private static double OneVsOne(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        int pairCount = k * (k - 1) / 2;
        double[] pairScores = new double[pairCount];
        double[] prevalence = new double[pairCount];
        int[] binary = new int[n];
        double[] column = new double[n];
        int pair = 0;
        PairContext context = new(yTrue, yScore, classes, k, binary, column);

        for (int a = 0; a < k; a++)
        {
            for (int b = a + 1; b < k; b++)
            {
                int size = 0;
                for (int i = 0; i < n; i++)
                {
                    if (yTrue[i] == classes[a] || yTrue[i] == classes[b])
                    {
                        size++;
                    }
                }

                // Hand & Till: each ordering of the pair is scored with its own
                // column, and the two are averaged.
                double aScore = PairScore(context, a, b, a, size);
                double bScore = PairScore(context, a, b, b, size);

                pairScores[pair] = (aScore + bScore) * 0.5;
                prevalence[pair] = (double)size / n;
                pair++;
            }
        }

        return average == Averaging.Macro ? Mean(pairScores) : WeightedMean(pairScores, prevalence);
    }

    private static double PairScore(PairContext context, int a, int b, int positiveClass, int size)
    {
        ReadOnlySpan<int> yTrue = context.YTrue;
        int[] classes = context.Classes;
        int k = context.ClassCount;
        int[] binary = context.Binary;
        double[] column = context.Column;
        int next = 0;

        for (int i = 0; i < yTrue.Length; i++)
        {
            if (yTrue[i] != classes[a] && yTrue[i] != classes[b])
            {
                continue;
            }

            binary[next] = yTrue[i] == classes[positiveClass] ? 1 : 0;
            column[next] = context.YScore[(i * k) + positiveClass];
            next++;
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, size), column.AsSpan(0, size), 1, default);
    }

    private static double Mean(double[] values)
    {
        double total = 0.0;
        foreach (double value in values)
        {
            total += value;
        }
        return total / values.Length;
    }

    private static double WeightedMean(double[] values, double[] weights)
    {
        double total = 0.0;
        double weightSum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            total += values[i] * weights[i];
            weightSum += weights[i];
        }
        return total / weightSum;
    }
}
