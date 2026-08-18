namespace Lodestar.Metrics.Internal;

/// <summary>
/// The contingency table of two labellings: how many samples each (true, predicted)
/// pair holds, with the row and column totals every clustering metric reads.
/// </summary>
/// <remarks>
/// Sparse on purpose. A dense table is <c>classes × clusters</c>, which is <c>n²</c>
/// when every sample is its own cluster — a case scikit-learn scores rather than
/// refuses, so it has to cost <c>O(n)</c> here too.
/// </remarks>
internal sealed class Contingency
{
    private const double Epsilon = 2.220446049250313e-16;

    public Contingency(Dictionary<long, int> cells, int[] rows, int[] columns, int samples)
    {
        Cells = cells;
        Rows = rows;
        Columns = columns;
        Samples = samples;
    }

    /// <summary>The non-empty cells, keyed by row ordinal in the high word and column in the low.</summary>
    public Dictionary<long, int> Cells { get; }

    /// <summary>How many samples each true label holds.</summary>
    public int[] Rows { get; }

    /// <summary>How many samples each predicted label holds.</summary>
    public int[] Columns { get; }

    /// <summary>The number of samples, which is the sum of either total.</summary>
    public int Samples { get; }

    public static Contingency Build(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        Dictionary<int, int> rowOf = [];
        Dictionary<int, int> columnOf = [];
        List<int> rows = [];
        List<int> columns = [];
        Dictionary<long, int> cells = [];

        for (int i = 0; i < labelsTrue.Length; i++)
        {
            int row = Ordinal(rowOf, rows, labelsTrue[i]);
            int column = Ordinal(columnOf, columns, labelsPred[i]);
            rows[row]++;
            columns[column]++;
            long key = ((long)row << 32) | (uint)column;
            cells[key] = cells.TryGetValue(key, out int held) ? held + 1 : 1;
        }

        return new Contingency(cells, [.. rows], [.. columns], labelsTrue.Length);
    }

    /// <summary>The mutual information of the two labellings, in nats — scikit-learn's <c>mutual_info_score</c>.</summary>
    /// <remarks>
    /// The grouping is scikit-learn's and is load-bearing at <c>1e-9</c>: it takes
    /// <c>log(nij) - log(n)</c> rather than <c>log(nij / n)</c>, and adds the outer
    /// term as <c>-log(ai·bj) + log(n) + log(n)</c>. Written the obvious way the two
    /// disagree in the last places, which the frozen corpus would catch as drift.
    /// </remarks>
    public double MutualInformation()
    {
        double total = Samples;
        double logTotal = Math.Log(total);
        double sum = 0.0;

        foreach (KeyValuePair<long, int> cell in Cells)
        {
            int row = (int)(cell.Key >> 32);
            int column = (int)(cell.Key & 0xFFFFFFFF);
            double nij = cell.Value;
            double fraction = nij / total;
            double outer = -Math.Log((double)Rows[row] * Columns[column]) + logTotal + logTotal;
            double term = (fraction * (Math.Log(nij) - logTotal)) + (fraction * outer);

            // Zeroed per term before summing, as the reference does: a single cluster
            // cancels each term to ~1e-16, and the sum of those is what callers divide by.
            sum += Math.Abs(term) < Epsilon ? 0.0 : term;
        }

        return Math.Max(sum, 0.0);
    }

    /// <summary>The entropy of one labelling, in nats — scikit-learn's <c>entropy</c>.</summary>
    /// <remarks>
    /// One label is <c>0.0</c> and no label is <c>1.0</c>, both taken from the
    /// reference: the second is what makes an empty input perfect agreement rather
    /// than a division by zero.
    /// </remarks>
    public static double Entropy(int[] counts, int samples)
    {
        if (samples == 0)
        {
            return 1.0;
        }

        if (counts.Length == 1)
        {
            return 0.0;
        }

        double logTotal = Math.Log(samples);
        return -counts
            .Where(count => count > 0)
            .Sum(count => (count / (double)samples) * (Math.Log(count) - logTotal));
    }

    /// <summary>Checks the two labellings agree in length; empty is a case, not an error.</summary>
    /// <exception cref="ArgumentException">The two disagree in length.</exception>
    public static void Validate(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        if (labelsTrue.Length != labelsPred.Length)
        {
            throw new ArgumentException(
                $"labelsTrue has {labelsTrue.Length} entries and labelsPred has {labelsPred.Length}; " +
                "they must agree.",
                nameof(labelsPred));
        }
    }

    private static int Ordinal(Dictionary<int, int> known, List<int> counts, int label)
    {
        if (known.TryGetValue(label, out int ordinal))
        {
            return ordinal;
        }

        ordinal = counts.Count;
        known[label] = ordinal;
        counts.Add(0);
        return ordinal;
    }
}
