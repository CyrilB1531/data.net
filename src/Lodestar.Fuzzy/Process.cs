namespace Lodestar.Fuzzy;

/// <summary>A single extraction hit: the matched choice, its score and its index in the input.</summary>
public readonly record struct ExtractResult(string Choice, double Score, int Index);

/// <summary>
/// Finds the best matches for a query within a collection of choices, reproducing
/// <c>rapidfuzz.process</c>.
/// </summary>
/// <remarks>
/// The default scorer is <see cref="Fuzz.WRatio"/> (like rapidfuzz). Results are
/// sorted by score descending, ties broken by original index, filtered by a score
/// cutoff and capped at a limit.
/// </remarks>
public static class Process
{
    /// <summary>
    /// Returns the best matches for <paramref name="query"/> among <paramref name="choices"/>.
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <param name="choices">The candidate strings.</param>
    /// <param name="scorer">Similarity scorer (default <see cref="Fuzz.WRatio"/>), returning a value in [0, 100].</param>
    /// <param name="limit">Maximum number of results (default 5); <c>null</c> returns all above the cutoff.</param>
    /// <param name="scoreCutoff">Minimum score to keep (inclusive). Default 0.</param>
    public static IReadOnlyList<ExtractResult> Extract(
        string query,
        IEnumerable<string> choices,
        Func<string, string, double>? scorer = null,
        int? limit = 5,
        double scoreCutoff = 0.0)
    {
        Guard.NotNull(query);
        Guard.NotNull(choices);
        scorer ??= Fuzz.WRatio;

        var hits = new List<ExtractResult>();
        int index = 0;
        foreach (string choice in choices)
        {
            double score = scorer(query, choice);
            if (score >= scoreCutoff)
            {
                hits.Add(new ExtractResult(choice, score, index));
            }
            index++;
        }

        // Stable order: score descending, ties by original index.
        hits.Sort(static (x, y) =>
        {
            int c = y.Score.CompareTo(x.Score);
            return c != 0 ? c : x.Index.CompareTo(y.Index);
        });

        if (limit is { } max && hits.Count > max)
        {
            hits.RemoveRange(max, hits.Count - max);
        }
        return hits;
    }

    /// <summary>Returns the single best match, or <c>null</c> if none clears the cutoff.</summary>
    public static ExtractResult? ExtractOne(
        string query,
        IEnumerable<string> choices,
        Func<string, string, double>? scorer = null,
        double scoreCutoff = 0.0)
    {
        IReadOnlyList<ExtractResult> best = Extract(query, choices, scorer, limit: 1, scoreCutoff);
        return best.Count > 0 ? best[0] : null;
    }
}
