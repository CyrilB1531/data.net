namespace Lodestar.Metrics.Internal;

/// <summary>The discounted gain of one ranked row, with and without tie averaging.</summary>
/// <remarks>
/// The gains are linear — <c>Σ relevance / log(rank + 1)</c> — where much of the literature
/// uses <c>2^relevance − 1</c>. That is scikit-learn's choice and it is the one reproduced:
/// measured, the same row scores 4.7618595071429155 linearly and 9.392789260714373
/// exponentially, so a reader comparing against a paper will find the other number.
/// </remarks>
internal static class Ranking
{
    /// <summary>The positional discounts, zeroed past <paramref name="k"/>.</summary>
    /// <remarks>
    /// A <paramref name="k"/> below 1 is refused rather than zeroing every discount and
    /// returning 0, which is what the arithmetic would otherwise do quietly. scikit-learn
    /// refuses it too, with "must be an int in the range [1, inf) or None".
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> is below 1, or <paramref name="logBase"/> is outside <c>(0, ∞)</c>.</exception>
    public static double[] Discounts(int count, int? k, double logBase)
    {
        if (k < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(k), k, "k must be at least 1, or null for the whole row.");
        }

        // Math.Log turns zero and the negatives into a NaN the caller reads as a score.
        // NaN itself fails both comparisons, which is the refusal it wants.
        if (!(logBase > 0.0 && logBase < double.PositiveInfinity))
        {
            throw new ArgumentOutOfRangeException(
                nameof(logBase), logBase, "logBase must be in the range (0, inf).");
        }

        double scale = Math.Log(logBase);
        double[] discounts = new double[count];
        int keep = k ?? count;
        for (int i = 0; i < count && i < keep; i++)
        {
            discounts[i] = scale / Math.Log(i + 2.0);
        }

        return discounts;
    }

    /// <summary>One row's discounted gain, ranking equal scores arbitrarily.</summary>
    public static double Gain(
        ReadOnlySpan<double> relevance, ReadOnlySpan<double> scores, double[] discounts)
    {
        int[] order = Descending(scores);
        double total = 0.0;
        for (int i = 0; i < order.Length; i++)
        {
            total += relevance[order[i]] * discounts[i];
        }

        return total;
    }

    /// <summary>One row's discounted gain, averaged over the permutations of equal scores.</summary>
    /// <remarks>
    /// Ranking tied documents arbitrarily makes the score depend on the order they happened
    /// to arrive in. scikit-learn averages over every permutation of a tied group, which has
    /// a closed form and needs no enumeration: within a group the mean relevance is what each
    /// position sees on average, so the group contributes that mean times the sum of the
    /// discounts of the positions it occupies. Measured, this is a 30% difference on a row
    /// whose scores are all equal — 0.8069 against 0.6138.
    /// </remarks>
    public static double TieAveragedGain(
        ReadOnlySpan<double> relevance, ReadOnlySpan<double> scores, double[] discounts)
    {
        int[] order = Descending(scores);
        double total = 0.0;
        int start = 0;
        while (start < order.Length)
        {
            int end = NextGroup(scores, order, start);
            double gains = 0.0;
            double discount = 0.0;
            for (int i = start; i < end; i++)
            {
                gains += relevance[order[i]];
                discount += discounts[i];
            }

            total += gains / (end - start) * discount;
            start = end;
        }

        return total;
    }

    /// <summary>The row's gain over the gain of its own perfect ranking, or 0 when nothing is relevant.</summary>
    /// <remarks>
    /// The ideal is computed without tie averaging, as scikit-learn does: ranking a row by its
    /// own relevance leaves ties only between equal gains, which no ordering can separate.
    /// </remarks>
    public static double Normalized(
        ReadOnlySpan<double> relevance, ReadOnlySpan<double> scores, double[] discounts, bool ignoreTies)
    {
        double ideal = Gain(relevance, relevance, discounts);
        if (ideal <= 0.0)
        {
            return 0.0;
        }

        double actual = ignoreTies
            ? Gain(relevance, scores, discounts)
            : TieAveragedGain(relevance, scores, discounts);
        return actual / ideal;
    }

    /// <summary>The end of the run of equal scores that starts at <paramref name="start"/>.</summary>
    private static int NextGroup(ReadOnlySpan<double> scores, int[] order, int start)
    {
        int end = start + 1;

        // S1244: a tie is exact equality of the score, which is what the reference's
        // `np.unique` groups on. A tolerance would merge scores it keeps apart.
#pragma warning disable S1244
        while (end < order.Length && scores[order[end]] == scores[order[start]])
#pragma warning restore S1244
        {
            end++;
        }

        return end;
    }

    /// <summary>Whether any relevance is negative, which <c>ndcg_score</c> refuses.</summary>
    public static bool HasNegative(ReadOnlySpan<double> relevance)
    {
        foreach (double value in relevance)
        {
            if (value < 0.0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Refuses a row too short to rank, in scikit-learn's own sentence.</summary>
    /// <remarks>
    /// The two names travel in because the callers spell the first argument differently —
    /// <c>yTrue</c> for the two gains, <c>relevance</c> for the reciprocal rank — and an
    /// <c>ArgumentException</c> naming a parameter the caller cannot see is worse than none.
    /// </remarks>
    /// <exception cref="ArgumentException">The row holds fewer than two documents.</exception>
    public static void Validate(
        ReadOnlySpan<double> relevance,
        ReadOnlySpan<double> scores,
        int labelCount,
        string relevanceName,
        string scoresName)
    {
        if (labelCount < 2)
        {
            throw new ArgumentException(
                "Computing NDCG is only meaningful when there is more than 1 document. " +
                $"Got {labelCount} instead.",
                nameof(labelCount));
        }

        if (relevance.Length != scores.Length)
        {
            throw new ArgumentException(
                $"{relevanceName} holds {relevance.Length} values and {scoresName} holds " +
                $"{scores.Length}; they must agree.",
                scoresName);
        }

        if (relevance.Length == 0 || relevance.Length % labelCount != 0)
        {
            throw new ArgumentException(
                $"{relevanceName} holds {relevance.Length} values, which is not a whole " +
                $"number of rows of {labelCount}.",
                relevanceName);
        }
    }

    /// <summary>The indices of one row, by descending score, ties by descending index.</summary>
    /// <remarks>
    /// What <c>top_k_accuracy_score</c>'s <c>kind="mergesort"</c> plus a reversal gives.
    /// <c>Array.Sort</c> alone does not: it is an introsort, stable only below its
    /// 16-element insertion-sort threshold — measured, a row of 17 equal scores comes back
    /// in an arbitrary order — so each tied group is reordered afterwards. Only the
    /// tie-averaged path is indifferent to that; <c>ignoreTies</c> is not, and neither is
    /// top-k accuracy.
    /// </remarks>
    public static int[] Descending(ReadOnlySpan<double> scores)
    {
        int[] order = new int[scores.Length];
        double[] copy = new double[scores.Length];
        for (int i = 0; i < scores.Length; i++)
        {
            order[i] = i;
            copy[i] = scores[i];
        }

        Array.Sort(copy, order);
        Array.Reverse(order);
        StabilizeTies(scores, order);
        return order;
    }

    /// <summary>Puts each run of equal scores back into descending index order.</summary>
    private static void StabilizeTies(ReadOnlySpan<double> scores, int[] order)
    {
        int start = 0;
        while (start < order.Length)
        {
            int end = NextGroup(scores, order, start);
            if (end - start > 1)
            {
                Array.Sort(order, start, end - start);
                Array.Reverse(order, start, end - start);
            }

            start = end;
        }
    }
}
