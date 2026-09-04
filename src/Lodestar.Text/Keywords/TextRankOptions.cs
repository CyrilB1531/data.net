namespace Lodestar.Text.Keywords;

/// <summary>What <see cref="TextRank"/> is built with.</summary>
public sealed record TextRankOptions
{
    /// <summary>The stop words dropped before the graph is built. Null takes <c>StopWords.English</c>.</summary>
    public IReadOnlyCollection<string>? StopWords { get; init; }

    /// <summary>How many tokens share a co-occurrence window. 2 pairs adjacent tokens only.</summary>
    public int Window { get; init; } = 2;

    /// <summary>The random-surfer damping of the reference implementation.</summary>
    public double Damping { get; init; } = 0.85;

    /// <summary>
    /// How close two successive iterates must be before the ranking is taken as converged.
    /// </summary>
    /// <remarks>
    /// This implementation's, not the reference's: summa solves the eigenproblem outright and
    /// has no tolerance to expose.
    /// </remarks>
    public double Tolerance { get; init; } = 1e-12;

    /// <summary>Iterations allowed before <c>Extract</c> gives up rather than return a half-ranked vector.</summary>
    public int MaxIterations { get; init; } = 1_000;

    /// <summary>What proportion of the ranked words to keep. Ignored when <see cref="Words"/> is set.</summary>
    public double Ratio { get; init; } = 0.2;

    /// <summary>How many ranked words to keep, overriding <see cref="Ratio"/>.</summary>
    public int? Words { get; init; }

    /// <summary>What counts as a word.</summary>
    public string TokenPattern { get; init; } = @"\b\w+\b";
}
