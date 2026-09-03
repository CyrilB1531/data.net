using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Keywords;

/// <summary>
/// Rapid Automatic Keyword Extraction: candidates are the runs between stop words,
/// scored by summing a per-word score over the run.
/// </summary>
/// <remarks>
/// The co-occurrence degree is counted per candidate: a word in an <c>n</c>-word run
/// gains <c>n</c> degree from it, itself included, which is what makes a long run
/// outscore a repeated single word.
/// </remarks>
public sealed class Rake
{
    private readonly RakeOptions _options;
    private readonly PhraseTokenizer _tokenizer;

    /// <summary>Builds an extractor.</summary>
    /// <param name="options">Null takes every default.</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>MinLength</c> is below 1.</exception>
    /// <exception cref="ArgumentException"><c>MaxLength</c> is below <c>MinLength</c>, so nothing can match.</exception>
    public Rake(RakeOptions? options = null)
    {
        _options = options ?? new RakeOptions();
        Guard.NotLessThan(_options.MinLength, 1);
        if (_options.MaxLength < _options.MinLength)
        {
            throw new ArgumentException(
                $"MaxLength {_options.MaxLength} is below MinLength {_options.MinLength}, so no candidate can match.",
                nameof(options));
        }

        _tokenizer = new PhraseTokenizer(_options.StopWords ?? StopWords.English, _options.TokenPattern);
    }

    /// <summary>Extracts the ranked candidates of one document.</summary>
    /// <param name="text">The document.</param>
    /// <returns>Candidates in descending score. Empty when the document has none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public IReadOnlyList<KeywordMatch> Extract(string text)
    {
        Guard.NotNull(text);

        IEnumerable<IReadOnlyList<string>> runs = _tokenizer.Split(text)
            .Where(run => run.Count >= _options.MinLength && run.Count <= _options.MaxLength);

        // Deduplication happens here, ahead of the tables: the reference measures the
        // repeated phrase's words at degree 2 and frequency 1, not 4 and 2.
        if (!_options.IncludeRepeatedPhrases)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            runs = runs.Where(run => seen.Add(string.Join(" ", run)));
        }

        IReadOnlyList<string>[] candidates = runs.ToArray();

        (Dictionary<string, int> degree, Dictionary<string, int> frequency) = CountCooccurrence(candidates);
        List<KeywordMatch> scored = ScoreCandidates(candidates, degree, frequency);
        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        return scored;
    }

    // Degree is counted per candidate: a word in an n-word run gains n from it,
    // itself included, which is what makes a long run outscore a repeated single word.
    private static (Dictionary<string, int> Degree, Dictionary<string, int> Frequency) CountCooccurrence(
        IReadOnlyList<IReadOnlyList<string>> candidates)
    {
        var degree = new Dictionary<string, int>(StringComparer.Ordinal);
        var frequency = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (IReadOnlyList<string> run in candidates)
        {
            foreach (string word in run)
            {
                degree[word] = degree.TryGetValue(word, out int d) ? d + run.Count : run.Count;
                frequency[word] = frequency.TryGetValue(word, out int f) ? f + 1 : 1;
            }
        }
        return (degree, frequency);
    }

    private List<KeywordMatch> ScoreCandidates(
        IReadOnlyList<string>[] candidates,
        Dictionary<string, int> degree,
        Dictionary<string, int> frequency)
    {
        var scored = new List<KeywordMatch>(candidates.Length);
        foreach (IReadOnlyList<string> run in candidates)
        {
            string phrase = string.Join(" ", run);
            scored.Add(new KeywordMatch(phrase, ScorePhrase(run, degree, frequency)));
        }
        return scored;
    }

    private double ScorePhrase(
        IReadOnlyList<string> run,
        Dictionary<string, int> degree,
        Dictionary<string, int> frequency)
    {
        double score = 0;
        foreach (string word in run)
        {
            score += _options.Metric switch
            {
                RakeMetric.WordDegree => degree[word],
                RakeMetric.WordFrequency => frequency[word],
                _ => (double)degree[word] / frequency[word],
            };
        }
        return score;
    }
}
