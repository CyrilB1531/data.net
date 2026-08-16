using System.Buffers;
using System.Runtime.ExceptionServices;

namespace Lodestar.Metrics.Internal;

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

        // 0 and 1 both mean sequential: see docs/decisions/0018 for why, and for
        // what changed in the drivers' allocation profile on this branch.
        int workers = Math.Max(1, options.MaxDegreeOfParallelism);

        if (options.Strategy == MultiClassStrategy.OneVsRest)
        {
            return workers == 1
                ? OneVsRest(yTrue, yScore, classes, average, options.SampleWeight)
                : OneVsRestParallel(yTrue, yScore, classes, average, options.SampleWeight, workers);
        }

        return workers == 1
            ? OneVsOne(yTrue, yScore, classes, average)
            : OneVsOneParallel(yTrue, yScore, classes, average, workers);
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
    /// A score matrix and the layout it is stored in, so a column can be read
    /// without the caller and the callee having to agree on two loose integers.
    /// The sequential path builds a row-major source over the caller's own span;
    /// the parallel path builds a column-major one over the transposed copy from
    /// <see cref="CopyForWorkers"/> — one <see langword="bool"/> picks the layout,
    /// rather than a pair of integers that could disagree with each other.
    /// </summary>
    private readonly ref struct ScoreSource
    {
        private readonly int _classCount;
        private readonly bool _columnMajor;

        public ScoreSource(
            ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int sampleCount, int classCount, bool columnMajor)
        {
            // sampleCount is explicit, not derived from yTrue.Length: two spans
            // sliced to a rented array's length can silently agree. See docs/decisions/0018.
            if (yTrue.Length != sampleCount)
            {
                throw new ArgumentException(
                    $"yTrue holds {yTrue.Length} entries but sampleCount is {sampleCount}. A span sliced to a rented "
                    + "array's length rather than the sample count lands here, and would otherwise shift every "
                    + "column at no visible cost.",
                    nameof(yTrue));
            }
            if (scores.Length != sampleCount * classCount)
            {
                throw new ArgumentException(
                    $"scores holds {scores.Length} entries; {sampleCount} samples over {classCount} classes needs "
                    + $"{sampleCount * classCount}. A span sliced to a rented array's length rather than the sample "
                    + "count lands here, and would otherwise read the wrong column at no visible cost.",
                    nameof(scores));
            }

            YTrue = yTrue;
            Scores = scores;
            _classCount = classCount;
            _columnMajor = columnMajor;
        }

        public ReadOnlySpan<int> YTrue { get; }

        public ReadOnlySpan<double> Scores { get; }

        /// <summary>Where class <paramref name="column"/>'s scores begin.</summary>
        public int Offset(int column) => _columnMajor ? column * YTrue.Length : column;

        /// <summary>How far apart consecutive samples of one column are.</summary>
        public int Step => _columnMajor ? 1 : _classCount;
    }

    /// <summary>
    /// One binary ROC-AUC over column <paramref name="column"/> of
    /// <paramref name="source"/>, where samples equal to
    /// <paramref name="positiveLabel"/> are the positive class.
    /// </summary>
    /// <remarks>
    /// <paramref name="column"/> is the class's position in the score matrix and
    /// <paramref name="positiveLabel"/> is the label value compared against
    /// <c>yTrue</c>; one-vs-rest needs both because they are not always the same
    /// number once <see cref="MultiClassRocOptions.Labels"/> is used.
    /// </remarks>
    private static double ClassScore(
        ScoreSource source, int column, int positiveLabel, ReadOnlySpan<double> sampleWeight,
        BinaryRoc.Scratch scratch, out double positiveWeight)
    {
        ReadOnlySpan<int> yTrue = source.YTrue;
        int offset = source.Offset(column);
        int step = source.Step;
        int n = yTrue.Length;
        int[] binary = scratch.Binary;
        double[] scoreColumn = scratch.Column;
        bool weighted = !sampleWeight.IsEmpty;
        positiveWeight = 0.0;

        for (int i = 0; i < n; i++)
        {
            bool positive = yTrue[i] == positiveLabel;
            binary[i] = positive ? 1 : 0;
            scoreColumn[i] = source.Scores[offset + (i * step)];
            if (positive)
            {
                positiveWeight += weighted ? sampleWeight[i] : 1.0;
            }
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, n), scoreColumn.AsSpan(0, n), 1, sampleWeight, scratch);
    }

    /// <summary>
    /// One ordering of one Hand &amp; Till pair: the samples of two classes only,
    /// scored with column <paramref name="column"/> — <paramref name="positiveLabel"/>'s
    /// column, which is one of <paramref name="labelA"/>'s or <paramref name="labelB"/>'s.
    /// </summary>
    private static double PairScore(
        ScoreSource source, int column, int labelA, int labelB, int positiveLabel, BinaryRoc.Scratch scratch)
    {
        ReadOnlySpan<int> yTrue = source.YTrue;
        int offset = source.Offset(column);
        int step = source.Step;
        int[] binary = scratch.Binary;
        double[] scoreColumn = scratch.Column;
        int next = 0;

        for (int i = 0; i < yTrue.Length; i++)
        {
            if (yTrue[i] != labelA && yTrue[i] != labelB)
            {
                continue;
            }

            binary[next] = yTrue[i] == positiveLabel ? 1 : 0;
            scoreColumn[next] = source.Scores[offset + (i * step)];
            next++;
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, next), scoreColumn.AsSpan(0, next), 1, default, scratch);
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
            ScoreSource source = new(yTrue, yScore, yTrue.Length, k, columnMajor: false);
            for (int c = 0; c < k; c++)
            {
                scores[c] = ClassScore(source, c, classes[c], sampleWeight, scratch, out double positiveWeight);
                weights[c] = positiveWeight;
            }
        }
        finally
        {
            scratch.Return();
        }

        return average == Averaging.Macro ? Mean(scores) : WeightedMean(scores, weights);
    }

    /// <summary>
    /// The inputs, in a shape a worker thread can be handed: <c>yTrue</c>, the
    /// weights if any, and the score matrix transposed so each class's column is
    /// contiguous for the worker that reads it.
    /// </summary>
    /// <remarks>
    /// A copy is the only legal option, and every span sliced from the result
    /// must use the sample count, never the rented length. See
    /// docs/decisions/0018 for both arguments.
    /// </remarks>
    private static (int[] YTrue, double[] ColumnMajor, double[] Weights) CopyForWorkers(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
        // Named yTrueCopy, not "labels": MultiClassRocOptions.Labels is the
        // k-entry class vocabulary, a same-length array only by coincidence.
        int[] yTrueCopy = ArrayPool<int>.Shared.Rent(n);
        double[] columnMajor = ArrayPool<double>.Shared.Rent(n * classCount);
        double[] weights = sampleWeight.IsEmpty
            ? []
            : ArrayPool<double>.Shared.Rent(n);

        yTrue.CopyTo(yTrueCopy.AsSpan(0, n));
        if (!sampleWeight.IsEmpty)
        {
            sampleWeight.CopyTo(weights.AsSpan(0, n));
        }

        for (int i = 0; i < n; i++)
        {
            int row = i * classCount;
            for (int c = 0; c < classCount; c++)
            {
                columnMajor[(c * n) + i] = yScore[row + c];
            }
        }

        return (yTrueCopy, columnMajor, weights);
    }

    private static void ReturnToPool((int[] YTrue, double[] ColumnMajor, double[] Weights) copy)
    {
        ArrayPool<int>.Shared.Return(copy.YTrue);
        ArrayPool<double>.Shared.Return(copy.ColumnMajor);
        if (copy.Weights.Length > 0)
        {
            // Length 0 is the no-weights case: [] was never rented, so handing it
            // back would give the pool an array it does not own.
            ArrayPool<double>.Shared.Return(copy.Weights);
        }
    }

    /// <summary>
    /// Runs indices <c>0 .. count - 1</c> over at most <paramref name="workers"/>
    /// threads, one <see cref="BinaryRoc.Scratch"/> per worker, and rethrows the
    /// failure of the lowest index once every index has been attempted.
    /// </summary>
    /// <remarks>
    /// The determinism lives here, not in each driver, so a second parallel
    /// driver — the one-vs-one pair loop — cannot re-derive it differently.
    /// <paramref name="body"/> returns its caught exception rather than being
    /// wrapped in a <c>catch</c> here, so a broken internal invariant in its own
    /// setup still escapes as the defect it is. See docs/decisions/0018.
    /// </remarks>
    private static void RunPerIndex(
        int count, int workers, int scratchLength, Func<int, BinaryRoc.Scratch, ArgumentException?> body)
    {
        ArgumentException?[] failures = new ArgumentException?[count];
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(workers, count) };

        Parallel.For(
            0,
            count,
            parallelOptions,
            () => BinaryRoc.Scratch.Rent(scratchLength),
            (index, _, scratch) =>
            {
                // Its own slot, so which worker lost the race cannot decide which
                // exception the caller sees.
                failures[index] = body(index, scratch);
                return scratch;
            },
            scratch => scratch.Return());

        RethrowFirst(failures);
    }

    /// <summary>
    /// One-vs-rest with the per-class loop spread over workers. Bit-identical to
    /// <see cref="OneVsRest"/>: class <c>c</c> writes <c>scores[c]</c> and
    /// <c>weights[c]</c> and nothing else, and the averaging below runs on this
    /// thread in array order, so no thread's timing can reach a sum.
    /// </summary>
    private static double OneVsRestParallel(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes,
        Averaging average, ReadOnlySpan<double> sampleWeight, int workers)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        bool weighted = !sampleWeight.IsEmpty;
        double[] scores = new double[k];
        double[] weights = new double[k];
        var copy = CopyForWorkers(yTrue, yScore, k, sampleWeight);

        try
        {
            RunPerIndex(k, workers, n, (c, scratch) =>
            {
                // Per worker (a span cannot cross into a lambda), and above the try
                // so a slicing bug escapes instead of being reported as bad input.
                ScoreSource source = new(
                    copy.YTrue.AsSpan(0, n), copy.ColumnMajor.AsSpan(0, n * k), n, k, columnMajor: true);

                // default, not a zero-length slice: ClassScore reads IsEmpty to
                // decide whether weighting applies at all.
                ReadOnlySpan<double> classWeight = weighted ? copy.Weights.AsSpan(0, n) : default;

                try
                {
                    // classes[c], not c: the column and the positive label are
                    // the same number only when the labels happen to be 0..k-1.
                    scores[c] = ClassScore(source, c, classes[c], classWeight, scratch, out double positiveWeight);
                    weights[c] = positiveWeight;
                    return null;
                }
                catch (ArgumentException ex)
                {
                    return ex;
                }
            });
        }
        finally
        {
            ReturnToPool(copy);
        }

        return average == Averaging.Macro ? Mean(scores) : WeightedMean(scores, weights);
    }

    /// <summary>
    /// One-vs-one with the per-pair loop spread over workers, bit-identical to
    /// <see cref="OneVsOne"/> — each pair writes only its own two slots.
    /// </summary>
    /// <remarks>
    /// Reads pairs from the same <see cref="Pairs"/> table <see cref="OneVsOne"/>
    /// walks, rather than decoding a triangular index, so the two orders cannot
    /// disagree. No weights: <see cref="Validate"/> refuses them here, as
    /// scikit-learn does; see docs/decisions/0018 for the copy this drives.
    /// </remarks>
    private static double OneVsOneParallel(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average, int workers)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        (int A, int B)[] pairs = Pairs(k);
        double[] pairScores = new double[pairs.Length];
        double[] prevalence = new double[pairs.Length];
        var copy = CopyForWorkers(yTrue, yScore, k, default);

        try
        {
            RunPerIndex(pairs.Length, workers, n, (pair, scratch) =>
            {
                // Per worker, and above the try so a slicing bug escapes instead
                // of being reported as bad input — as in OneVsRestParallel.
                ScoreSource source = new(
                    copy.YTrue.AsSpan(0, n), copy.ColumnMajor.AsSpan(0, n * k), n, k, columnMajor: true);

                try
                {
                    // The pair tuple already carries both columns, so the worker
                    // asks for nothing new — ScorePair is at S107's seven-parameter limit.
                    ScorePair(source, classes, pairs[pair], pair, pairScores, prevalence, scratch);
                    return null;
                }
                catch (ArgumentException ex)
                {
                    // Belongs here, not in RunPerIndex: deleting it is a live
                    // mutation only Reports_the_lowest_offending_pair_not_the_fastest_worker catches.
                    return ex;
                }
            });
        }
        finally
        {
            ReturnToPool(copy);
        }

        return average == Averaging.Macro ? Mean(pairScores) : WeightedMean(pairScores, prevalence);
    }

    /// <summary>
    /// Rethrows the failure of the lowest index, so a bad input produces the same
    /// exception the sequential path would have produced.
    /// </summary>
    /// <remarks>
    /// See docs/decisions/0018 for why <see cref="RunPerIndex"/> never stops
    /// early and why <see cref="ExceptionDispatchInfo"/> rethrows the original
    /// instance instead of wrapping it in an <see cref="AggregateException"/>.
    /// </remarks>
    private static void RethrowFirst(ArgumentException?[] failures)
    {
        // An indexed loop, ascending: "lowest index wins" is then a property of
        // this code rather than of a library method's documented scan order.
        for (int i = 0; i < failures.Length; i++)
        {
            ArgumentException? failure = failures[i];
            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }

    private static double OneVsOne(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average)
    {
        int k = classes.Length;
        (int A, int B)[] pairs = Pairs(k);
        double[] pairScores = new double[pairs.Length];
        double[] prevalence = new double[pairs.Length];
        BinaryRoc.Scratch scratch = BinaryRoc.Scratch.Rent(yTrue.Length);

        try
        {
            ScoreSource source = new(yTrue, yScore, yTrue.Length, k, columnMajor: false);
            for (int pair = 0; pair < pairs.Length; pair++)
            {
                ScorePair(source, classes, pairs[pair], pair, pairScores, prevalence, scratch);
            }
        }
        finally
        {
            scratch.Return();
        }

        return average == Averaging.Macro ? Mean(pairScores) : WeightedMean(pairScores, prevalence);
    }

    /// <summary>
    /// The body of one pair, kept separate so the arithmetic exists in one
    /// place regardless of what iterates the pairs. Writes only its own two
    /// slots.
    /// </summary>
    private static void ScorePair(
        ScoreSource source, int[] classes, (int A, int B) pair, int index,
        double[] pairScores, double[] prevalence, BinaryRoc.Scratch scratch)
    {
        ReadOnlySpan<int> yTrue = source.YTrue;
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
        // and the two are averaged.
        double aScore = PairScore(source, pair.A, labelA, labelB, labelA, scratch);
        double bScore = PairScore(source, pair.B, labelA, labelB, labelB, scratch);

        pairScores[index] = (aScore + bScore) * 0.5;
        prevalence[index] = (double)size / n;
    }

    /// <summary>Every unordered class pair, in the order this method's nested loops produce them.</summary>
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
