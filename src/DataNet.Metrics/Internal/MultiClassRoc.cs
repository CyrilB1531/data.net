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
        MultiClassRocOptions options)
    {
        Averaging average = options.Average ?? Averaging.Macro;
        int n = Validate(yTrue, yScore, classCount, options, average);
        int[] classes = ResolveLabels(yTrue, options.Labels, classCount);
        ValidateRowSums(yScore, n, classCount);

        return options.Strategy == MultiClassStrategy.OneVsRest
            ? OneVsRest(yTrue, yScore, classes, average, options.SampleWeight)
            : OneVsOne(yTrue, yScore, classes, average);
    }

    private static int Validate(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount,
        MultiClassRocOptions options, Averaging average)
    {
        int n = yTrue.Length;
        if (options.MaxDegreeOfParallelism < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), options.MaxDegreeOfParallelism,
                "MaxDegreeOfParallelism cannot be negative. 0 and 1 are both sequential.");
        }
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
                nameof(options));
        }
        if (!options.SampleWeight.IsEmpty)
        {
            if (options.SampleWeight.Length != n)
            {
                throw new ArgumentException(
                    $"sampleWeight has {options.SampleWeight.Length} entries but there are {n} samples.",
                    nameof(options));
            }
            if (options.Strategy == MultiClassStrategy.OneVsOne)
            {
                throw new ArgumentException(
                    "scikit-learn does not support sampleWeight for one-vs-one ROC AUC, and neither does this.",
                    nameof(options));
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

    /// <summary>
    /// One binary ROC-AUC over a column of the score matrix, where the column is
    /// addressed as <c>scores[offset + (i * stride)]</c>.
    /// </summary>
    /// <remarks>
    /// The two callers hold the same numbers in two layouts, and this is where
    /// that difference is confined to two integers. The sequential driver passes
    /// the caller's row-major span with <c>offset = c</c> and <c>stride = k</c>,
    /// reading it in place; the parallel driver passes a column-major transpose
    /// with <c>offset = c * n</c> and <c>stride = 1</c>, because a span cannot be
    /// captured by a worker's lambda and the copy may as well be contiguous per
    /// column while it is being made.
    /// </remarks>
    private static double ClassScore(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int offset, int stride,
        int positiveLabel, ReadOnlySpan<double> sampleWeight, BinaryRoc.Scratch scratch,
        out double positiveWeight)
    {
        int n = yTrue.Length;
        int[] binary = scratch.Binary;
        double[] column = scratch.Column;
        bool weighted = !sampleWeight.IsEmpty;
        positiveWeight = 0.0;

        for (int i = 0; i < n; i++)
        {
            bool positive = yTrue[i] == positiveLabel;
            binary[i] = positive ? 1 : 0;
            column[i] = scores[offset + (i * stride)];
            if (positive)
            {
                positiveWeight += weighted ? sampleWeight[i] : 1.0;
            }
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, n), column.AsSpan(0, n), 1, sampleWeight, scratch);
    }

    /// <summary>
    /// One ordering of one Hand &amp; Till pair: the samples of two classes only,
    /// scored with <paramref name="positiveLabel"/>'s column.
    /// </summary>
    private static double PairScore(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int offset, int stride,
        int labelA, int labelB, int positiveLabel, BinaryRoc.Scratch scratch)
    {
        int[] binary = scratch.Binary;
        double[] column = scratch.Column;
        int next = 0;

        for (int i = 0; i < yTrue.Length; i++)
        {
            if (yTrue[i] != labelA && yTrue[i] != labelB)
            {
                continue;
            }

            binary[next] = yTrue[i] == positiveLabel ? 1 : 0;
            column[next] = scores[offset + (i * stride)];
            next++;
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, next), column.AsSpan(0, next), 1, default, scratch);
    }

    private static double OneVsRest(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes,
        Averaging average, ReadOnlySpan<double> sampleWeight)
    {
        int k = classes.Length;
        double[] scores = new double[k];
        double[] weights = new double[k];
        BinaryRoc.Scratch scratch = BinaryRoc.Scratch.Rent(yTrue.Length);

        try
        {
            for (int c = 0; c < k; c++)
            {
                scores[c] = ClassScore(
                    yTrue, yScore, c, k, classes[c], sampleWeight, scratch, out double positiveWeight);
                weights[c] = positiveWeight;
            }
        }
        finally
        {
            scratch.Return();
        }

        return average == Averaging.Macro ? Mean(scores) : WeightedMean(scores, weights);
    }

    private static double OneVsOne(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        (int A, int B)[] pairs = Pairs(k);
        double[] pairScores = new double[pairs.Length];
        double[] prevalence = new double[pairs.Length];
        BinaryRoc.Scratch scratch = BinaryRoc.Scratch.Rent(n);

        try
        {
            for (int pair = 0; pair < pairs.Length; pair++)
            {
                ScorePair(yTrue, yScore, classes, k, 1, pairs[pair], pair, pairScores, prevalence, scratch);
            }
        }
        finally
        {
            scratch.Return();
        }

        return average == Averaging.Macro ? Mean(pairScores) : WeightedMean(pairScores, prevalence);
    }

    /// <summary>
    /// The body of one pair, shared by the sequential and parallel drivers so the
    /// arithmetic exists once. Writes only its own two slots.
    /// </summary>
    private static void ScorePair(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int[] classes, int stride, int columnStride,
        (int A, int B) pair, int index, double[] pairScores, double[] prevalence, BinaryRoc.Scratch scratch)
    {
        int n = yTrue.Length;
        int labelA = classes[pair.A];
        int labelB = classes[pair.B];
        int size = 0;
        for (int i = 0; i < n; i++)
        {
            if (yTrue[i] == labelA || yTrue[i] == labelB)
            {
                size++;
            }
        }

        // Hand & Till: each ordering of the pair is scored with its own column,
        // and the two are averaged. columnStride is the distance between one
        // column's start and the next: 1 for the row-major span the sequential
        // driver owns (columns are adjacent; stride carries the row-to-row
        // step), n for the column-major transpose a parallel worker is handed
        // (columns are n apart; the row-to-row step within one is 1). Deriving
        // both offsets from columnStride, rather than branching on it, is what
        // keeps this arithmetic in one place for both layouts.
        int offsetA = pair.A * columnStride;
        int offsetB = pair.B * columnStride;
        double aScore = PairScore(yTrue, scores, offsetA, stride, labelA, labelB, labelA, scratch);
        double bScore = PairScore(yTrue, scores, offsetB, stride, labelA, labelB, labelB, scratch);

        pairScores[index] = (aScore + bScore) * 0.5;
        prevalence[index] = (double)size / n;
    }

    /// <summary>Every unordered class pair, in the order the nested loops produced.</summary>
    private static (int A, int B)[] Pairs(int k)
    {
        (int A, int B)[] pairs = new (int, int)[k * (k - 1) / 2];
        int next = 0;
        for (int a = 0; a < k; a++)
        {
            for (int b = a + 1; b < k; b++)
            {
                pairs[next++] = (a, b);
            }
        }
        return pairs;
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
