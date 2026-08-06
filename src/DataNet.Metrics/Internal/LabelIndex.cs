namespace DataNet.Metrics.Internal;

/// <summary>
/// The set of labels a metric is computed over, plus the map from a label value
/// to its ordinal in that set.
/// </summary>
/// <remarks>
/// <para>
/// When the caller supplies an explicit label subset, the ordinal set this
/// index actually covers is <em>extended</em> beyond that subset: the
/// requested labels first, in the caller's order, then every other label
/// observed in the data, appended in ascending order — scikit-learn's own
/// rule inside <c>multilabel_confusion_matrix</c>
/// (<c>np.hstack([labels, np.setdiff1d(present_labels, labels)])</c>). This
/// is what lets a confusion matrix built over a subset still recover the
/// correct precision/recall denominators, which count predictions and truths
/// against labels outside the subset too. <see cref="RequestedCount"/> is the
/// caller-facing count; <see cref="Count"/> is the extended one. They are
/// equal whenever <c>labels</c> was omitted, or every observed label was
/// already requested.
/// </para>
/// <para>
/// Two lookup strategies, chosen from the data rather than fixed: a direct
/// offset table when the label values are packed closely enough that the table
/// is cheaper than the samples it will serve, and a binary search over the
/// sorted values otherwise. A dictionary is never the right answer here — the
/// lookup runs twice per sample, and both strategies beat hashing an int.
/// </para>
/// </remarks>
internal sealed class LabelIndex
{
    // Above this, the offset table stops being a table and starts being a leak.
    private const int MaxDirectTableSize = 1 << 22;

    private readonly int[] _labels;
    private readonly int[]? _direct;     // (value - _min) -> ordinal, -1 when absent
    private readonly int _min;
    private readonly int[]? _sorted;     // ascending label values
    private readonly int[]? _ordinals;   // _sorted[i] -> ordinal in _labels

    private LabelIndex(int[] labels, int requestedCount, bool isExplicit)
    {
        _labels = labels;
        RequestedCount = requestedCount;
        Explicit = isExplicit;

        (int min, int max) = MinMax(labels);
        long range = (long)max - min + 1;

        if (range <= MaxDirectTableSize)
        {
            _min = min;
            _direct = BuildDirectTable(labels, min, (int)range);
            return;
        }

        (_sorted, _ordinals) = BuildSortedIndex(labels);
    }

    private static (int Min, int Max) MinMax(int[] labels)
    {
        int min = labels[0];
        int max = labels[0];
        foreach (int value in labels)
        {
            if (value < min) { min = value; }
            if (value > max) { max = value; }
        }
        return (min, max);
    }

    private static int[] BuildDirectTable(int[] labels, int min, int range)
    {
        int[] direct = new int[range];
        for (int i = 0; i < direct.Length; i++) { direct[i] = -1; }
        for (int i = 0; i < labels.Length; i++)
        {
            int slot = labels[i] - min;
            if (direct[slot] >= 0)
            {
                throw new ArgumentException(
                    $"Label {labels[i]} appears more than once.", nameof(labels));
            }
            direct[slot] = i;
        }
        return direct;
    }

    private static (int[] Sorted, int[] Ordinals) BuildSortedIndex(int[] labels)
    {
        int[] sorted = (int[])labels.Clone();
        int[] ordinals = new int[labels.Length];
        for (int i = 0; i < ordinals.Length; i++) { ordinals[i] = i; }
        Array.Sort(sorted, ordinals);
        for (int i = 1; i < sorted.Length; i++)
        {
            if (sorted[i] == sorted[i - 1])
            {
                throw new ArgumentException(
                    $"Label {sorted[i]} appears more than once.", nameof(labels));
            }
        }
        return (sorted, ordinals);
    }

    /// <summary>
    /// The full extended label set this index resolves ordinals over: the
    /// requested labels first, then any other observed label. Length
    /// <see cref="Count"/>. Callers that only want the reported labels take
    /// the first <see cref="RequestedCount"/> entries.
    /// </summary>
    public int[] Labels => _labels;

    /// <summary>How many labels the extended set holds.</summary>
    public int Count => _labels.Length;

    /// <summary>
    /// How many labels were actually requested — the reporting count. Equal to
    /// <see cref="Count"/> unless an explicit label subset left some observed
    /// label out, in which case <see cref="Count"/> is larger.
    /// </summary>
    public int RequestedCount { get; }

    /// <summary>True when the caller supplied the label set explicitly.</summary>
    public bool Explicit { get; }

    /// <summary>The ordinal of <paramref name="label"/>, or -1 when it is not in the set.</summary>
    public int IndexOf(int label)
    {
        if (_direct is not null)
        {
            int slot = label - _min;
            return (uint)slot < (uint)_direct.Length ? _direct[slot] : -1;
        }

        int found = Array.BinarySearch(_sorted!, label);
        return found < 0 ? -1 : _ordinals![found];
    }

    /// <summary>
    /// Resolves the label set: the caller's order when supplied, extended with
    /// every other observed label (see the class remarks); the ascending
    /// sorted union of both inputs, with nothing to extend, when omitted.
    /// </summary>
    public static LabelIndex Create(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ReadOnlySpan<int> labels)
    {
        if (labels.IsEmpty)
        {
            int[] union = SortedUnion(yTrue, yPred);
            return new LabelIndex(union, union.Length, isExplicit: false);
        }

        int[] requested = labels.ToArray();
        int[] extended = AppendObserved(requested, SortedUnion(yTrue, yPred));
        return new LabelIndex(extended, requested.Length, isExplicit: true);
    }

    /// <summary>
    /// Appends every label in <paramref name="observed"/> that is not already
    /// in <paramref name="requested"/>, preserving <paramref name="observed"/>'s
    /// ascending order — <c>np.setdiff1d(present_labels, labels)</c> stacked
    /// after <paramref name="requested"/>.
    /// </summary>
    private static int[] AppendObserved(int[] requested, int[] observed)
    {
        var seen = new HashSet<int>(requested);
        int[] extra = [.. observed.Where(seen.Add)];
        if (extra.Length == 0)
        {
            return requested;
        }

        int[] result = new int[requested.Length + extra.Length];
        Array.Copy(requested, result, requested.Length);
        Array.Copy(extra, 0, result, requested.Length, extra.Length);
        return result;
    }

    private static int[] SortedUnion(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred)
    {
        int min = yTrue[0];
        int max = yTrue[0];
        Extend(yTrue, ref min, ref max);
        Extend(yPred, ref min, ref max);

        long range = (long)max - min + 1;
        if (range <= MaxDirectTableSize && range <= (4L * yTrue.Length) + 1024)
        {
            // Dense enough: mark presence in one pass, then read the marks in
            // order. O(n + range) with no sort and no hashing.
            bool[] seen = new bool[(int)range];
            Mark(yTrue, seen, min);
            Mark(yPred, seen, min);

            // SonarLint S3267 wants this rewritten with Where/Count(), which
            // this codebase avoids on paths like this one: this loop runs
            // once per LabelIndex construction, but over `seen`, an array
            // sized to the observed label range — up to MaxDirectTableSize
            // entries — so it is not a fixed-size cost, and LabelIndex.Create
            // itself runs once per ConfusionMatrix.Compute call, on the same
            // path a later task benchmarks these metrics against scikit-learn
            // on, with a merge gate of beating it on processor time.
            // Where/Count() would allocate an iterator and add a delegate
            // call per element of that array; a plain counting loop does
            // neither.
#pragma warning disable S3267
            int count = 0;
            foreach (bool present in seen) { if (present) { count++; } }
#pragma warning restore S3267

            int[] union = new int[count];
            int next = 0;
            for (int i = 0; i < seen.Length; i++)
            {
                if (seen[i]) { union[next++] = min + i; }
            }
            return union;
        }

        int[] all = new int[yTrue.Length + yPred.Length];
        yTrue.CopyTo(all);
        yPred.CopyTo(all.AsSpan(yTrue.Length));
        Array.Sort(all);

        int unique = 1;
        for (int i = 1; i < all.Length; i++)
        {
            if (all[i] != all[i - 1]) { all[unique++] = all[i]; }
        }
        int[] result = new int[unique];
        Array.Copy(all, result, unique);
        return result;
    }

    private static void Extend(ReadOnlySpan<int> values, ref int min, ref int max)
    {
        foreach (int value in values)
        {
            if (value < min) { min = value; }
            if (value > max) { max = value; }
        }
    }

    private static void Mark(ReadOnlySpan<int> values, bool[] seen, int min)
    {
        foreach (int value in values) { seen[value - min] = true; }
    }
}
