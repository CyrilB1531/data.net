namespace Lodestar.Text.Keywords;

/// <summary>How RAKE scores a word before the phrase sums it.</summary>
public enum RakeMetric
{
    /// <summary><c>deg(w) / freq(w)</c>. The paper's, and the reference implementation's default.</summary>
    DegreeToFrequencyRatio,

    /// <summary><c>deg(w)</c>: how many words it shares a candidate with, itself included, counted per occurrence.</summary>
    WordDegree,

    /// <summary><c>freq(w)</c>: how often it occurs at all.</summary>
    WordFrequency,
}

/// <summary>What <see cref="Rake"/> is built with.</summary>
public sealed record RakeOptions
{
    /// <summary>The stop words that delimit candidates. Null takes <c>StopWords.English</c>.</summary>
    public IReadOnlyCollection<string>? StopWords { get; init; }

    /// <summary>Which per-word score the phrase sums.</summary>
    public RakeMetric Metric { get; init; } = RakeMetric.DegreeToFrequencyRatio;

    /// <summary>Shortest candidate kept, in words, inclusive.</summary>
    public int MinLength { get; init; } = 1;

    /// <summary>Longest candidate kept, in words, inclusive.</summary>
    public int MaxLength { get; init; } = 100_000;

    /// <summary>When false, a candidate that occurs twice is reported once.</summary>
    public bool IncludeRepeatedPhrases { get; init; } = true;

    /// <summary>
    /// What counts as a word.
    /// </summary>
    /// <remarks>
    /// <c>\b\w+\b</c>, not the vectorizers' <c>\b\w\w+\b</c>: a one-letter word neighbours a
    /// boundary rather than being a stop word, and dropping it would merge two candidates.
    /// </remarks>
    public string TokenPattern { get; init; } = @"\b\w+\b";
}
