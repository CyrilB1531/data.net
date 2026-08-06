namespace DataNet.Metrics.Internal;

/// <summary>
/// The set of labels a metric is computed over, plus the map from a label value
/// to its ordinal in that set.
/// </summary>
/// <remarks>
/// Two lookup strategies, chosen from the data rather than fixed: a direct
/// offset table when the label values are packed closely enough that the table
/// is cheaper than the samples it will serve, and a binary search over the
/// sorted values otherwise. A dictionary is never the right answer here — the
/// lookup runs twice per sample, and both strategies beat hashing an int.
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

    private LabelIndex(int[] labels, bool isExplicit)
    {
        _labels = labels;
        Explicit = isExplicit;

        int min = labels[0];
        int max = labels[0];
        foreach (int value in labels)
        {
            if (value < min) { min = value; }
            if (value > max) { max = value; }
        }

        long range = (long)max - min + 1;
        if (range <= MaxDirectTableSize)
        {
            _min = min;
            _direct = new int[(int)range];
            for (int i = 0; i < _direct.Length; i++) { _direct[i] = -1; }
            for (int i = 0; i < labels.Length; i++)
            {
                int slot = labels[i] - min;
                if (_direct[slot] >= 0)
                {
                    throw new ArgumentException(
                        $"Label {labels[i]} appears more than once.", nameof(labels));
                }
                _direct[slot] = i;
            }
            return;
        }

        _sorted = (int[])labels.Clone();
        _ordinals = new int[labels.Length];
        for (int i = 0; i < _ordinals.Length; i++) { _ordinals[i] = i; }
        Array.Sort(_sorted, _ordinals);
        for (int i = 1; i < _sorted.Length; i++)
        {
            if (_sorted[i] == _sorted[i - 1])
            {
                throw new ArgumentException(
                    $"Label {_sorted[i]} appears more than once.", nameof(labels));
            }
        }
    }

    /// <summary>The labels, in the order metrics report them.</summary>
    public int[] Labels => _labels;

    /// <summary>How many labels the set holds.</summary>
    public int Count => _labels.Length;

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
    /// Resolves the label set: the caller's order when supplied, otherwise the
    /// ascending sorted union of both inputs — scikit-learn's rule exactly.
    /// </summary>
    public static LabelIndex Create(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ReadOnlySpan<int> labels)
    {
        if (!labels.IsEmpty)
        {
            return new LabelIndex(labels.ToArray(), isExplicit: true);
        }

        return new LabelIndex(SortedUnion(yTrue, yPred), isExplicit: false);
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

            int count = 0;
            foreach (bool present in seen) { if (present) { count++; } }

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
