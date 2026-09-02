using Lodestar.Text.Indexing;

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

    /// <summary>
    /// Scores only the choices a <see cref="BkTree"/> puts within <paramref name="maxDistance"/>
    /// of the query, then ranks them exactly as <see cref="Extract"/> does.
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <param name="index">A tree already holding the choices.</param>
    /// <param name="maxDistance">The tree's radius, in that tree's own metric.</param>
    /// <param name="scorer">Similarity scorer (default <see cref="Fuzz.WRatio"/>), in [0, 100].</param>
    /// <param name="limit">Maximum number of results (default 5); <c>null</c> returns all above the cutoff.</param>
    /// <param name="scoreCutoff">Minimum score to keep (inclusive). Default 0.</param>
    /// <remarks>
    /// This is a prefilter, not a faster <see cref="Extract"/>. It returns what <see cref="Extract"/> returns
    /// <b>only if</b> every choice further than <paramref name="maxDistance"/> would have scored below <paramref name="scoreCutoff"/>.
    /// The tree filters on an integer distance; the scorer is a similarity, and the default <see cref="Fuzz.WRatio"/>
    /// is not a function of that distance — so a caller who leaves the cutoff at 0 gets a subset, silently. Choosing the pair is the caller's job, and it is the whole contract.
    /// </remarks>
    public static IReadOnlyList<ExtractResult> ExtractIndexed(
        string query,
        BkTree index,
        int maxDistance,
        Func<string, string, double>? scorer = null,
        int? limit = 5,
        double scoreCutoff = 0.0)
    {
        Guard.NotNull(query);
        Guard.NotNull(index);
        Guard.NotLessThan(maxDistance, 0);

        IReadOnlyList<BkTreeMatch> candidates = index.WithinDistance(query, maxDistance);
        var choices = new string[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            choices[i] = candidates[i].Item;
        }

        return Extract(query, choices, scorer, limit, scoreCutoff);
    }
}
