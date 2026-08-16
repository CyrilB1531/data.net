namespace DataNet.Metrics.Internal;

/// <summary>The computation homogeneity and completeness share.</summary>
internal static class Cluster
{
    /// <summary>The mutual information over the entropy of <paramref name="over"/>.</summary>
    /// <remarks>
    /// Homogeneity when the truth is passed first, completeness when it is passed second — the two
    /// are one function called both ways round, which is scikit-learn's own definition. An entropy
    /// of zero returns <c>1.0</c> rather than dividing: one class, or none, leaves nothing for the
    /// clustering to get wrong, and that is what makes an empty input score perfectly.
    /// </remarks>
    public static double Homogeneity(ReadOnlySpan<int> over, ReadOnlySpan<int> against)
    {
        Contingency.Validate(over, against);
        if (over.Length == 0)
        {
            return 1.0;
        }

        Contingency table = Contingency.Build(over, against);
        double entropy = Contingency.Entropy(table.Rows, table.Samples);
        return entropy <= 0.0 ? 1.0 : table.MutualInformation() / entropy;
    }
}
