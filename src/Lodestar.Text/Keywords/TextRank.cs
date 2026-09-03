using System.Text.RegularExpressions;
using Lodestar.Text.Stemming;
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Keywords;

// CA1308 (normalize to uppercase): ScanClean asks whether the source already spelled a
// token lower-case, the same question PhraseTokenizer asks of stop words; ToUpperInvariant
// would answer a different question.
#pragma warning disable CA1308

/// <summary>
/// TextRank over a co-occurrence graph: rank the stems, keep the best, and re-glue the
/// ones that stood next to each other in the source.
/// </summary>
/// <remarks>
/// A glued phrase scores the mean of its parts and need not be grammatical — the
/// reference's behaviour, reproduced on purpose. A word extends a run only when the
/// source spelled it exactly as it lower-cases to, matching summa's own
/// <c>text.split()</c> equality check.
/// </remarks>
public sealed class TextRank
{
    private readonly TextRankOptions _options;
    private readonly PhraseTokenizer _tokenizer;
    private readonly StopWordSet _stopWords;
    private readonly Regex _rawToken;

    /// <summary>Builds an extractor.</summary>
    /// <param name="options">Null takes every default.</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>Window</c> is below 1, <c>Damping</c> is outside <c>(0, 1)</c>, <c>Ratio</c> is outside <c>(0, 1]</c>, or <c>MaxIterations</c> is below 1.</exception>
    public TextRank(TextRankOptions? options = null)
    {
        _options = options ?? new TextRankOptions();
        Guard.NotLessThan(_options.Window, 1);
        Guard.NotLessThan(_options.MaxIterations, 1);
        if (_options.Damping <= 0 || _options.Damping >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.Damping, "Damping must lie in (0, 1).");
        }
        if (_options.Ratio <= 0 || _options.Ratio > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.Ratio, "Ratio must lie in (0, 1].");
        }

        IReadOnlyCollection<string> stop = _options.StopWords ?? StopWords.English;
        _stopWords = StopWordSet.Adopt(stop);
        _tokenizer = new PhraseTokenizer(stop, _options.TokenPattern);
        _rawToken = new Regex(_options.TokenPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);
    }

    /// <summary>Extracts the ranked keywords of one document.</summary>
    /// <param name="text">The document.</param>
    /// <returns>Keywords in descending score, glued where their parts were adjacent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The ranking did not converge within <c>MaxIterations</c>.</exception>
    public IReadOnlyList<KeywordMatch> Extract(string text)
    {
        Guard.NotNull(text);

        IReadOnlyList<string> words = _tokenizer.Words(text);
        (string?[] stream, Dictionary<string, Dictionary<string, int>> surface) = BuildStream(words);

        var graph = new WordGraph(stream, _options.Window);
        if (graph.Nodes.Count == 0)
        {
            return [];
        }

        double[] ranked = graph.Rank(_options.Damping, _options.Tolerance, _options.MaxIterations);
        Dictionary<string, double> scoreByStem = TopStems(graph.Nodes, ranked);

        bool[] clean = ScanClean(text, words.Count);
        return Glue(stream, clean, scoreByStem, surface);
    }

    // One entry per raw token, never compacted: the stem where the word is kept, null
    // where a stop word stood, so the co-occurrence window still counts its position.
    private (string?[] Stream, Dictionary<string, Dictionary<string, int>> Surface) BuildStream(
        IReadOnlyList<string> words)
    {
        var stream = new string?[words.Count];
        var surface = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i];
            if (_stopWords.Contains(word))
            {
                continue;
            }

            string stem = EnglishSnowballStemmer.Stem(word);
            stream[i] = stem;
            RecordSurface(surface, stem, word);
        }

        return (stream, surface);
    }

    private static void RecordSurface(Dictionary<string, Dictionary<string, int>> surface, string stem, string word)
    {
        if (!surface.TryGetValue(stem, out Dictionary<string, int>? counts))
        {
            surface[stem] = counts = new Dictionary<string, int>(StringComparer.Ordinal);
        }
        counts[word] = counts.TryGetValue(word, out int c) ? c + 1 : 1;
    }

    // Clean: spelled exactly as its lower-cased form, whitespace on both edges — no
    // attached punctuation, no stray case. Only a clean token may extend a glued run.
    private bool[] ScanClean(string text, int expectedCount)
    {
        var clean = new bool[expectedCount];
        int i = 0;
        foreach (Match m in _rawToken.Matches(text))
        {
            if (i >= expectedCount)
            {
                break;
            }

            bool precededByGap = m.Index == 0 || char.IsWhiteSpace(text[m.Index - 1]);
            int end = m.Index + m.Length;
            bool followedByGap = end == text.Length || char.IsWhiteSpace(text[end]);
            clean[i] = precededByGap && followedByGap && string.Equals(m.Value, m.Value.ToLowerInvariant(), StringComparison.Ordinal);
            i++;
        }

        return clean;
    }

    private Dictionary<string, double> TopStems(IReadOnlyList<string> nodes, double[] ranked)
    {
        int take = _options.Words ?? (int)(nodes.Count * _options.Ratio);
        // netstandard2.0 has no Math.Clamp; nodes.Count bounds take from both sides.
        take = take < 0 ? 0 : Math.Min(take, nodes.Count);

        return nodes
            .Select((stem, i) => (stem, score: ranked[i]))
            .OrderByDescending(p => p.score)
            .Take(take)
            .ToDictionary(p => p.stem, p => p.score, StringComparer.Ordinal);
    }

    // Adjacent selected stems become one phrase, scored by the mean of their parts. A
    // null or a dirty continuation (see ScanClean) both break the run.
    private static List<KeywordMatch> Glue(
        string?[] stream,
        bool[] clean,
        Dictionary<string, double> scoreByStem,
        Dictionary<string, Dictionary<string, int>> surface)
    {
        var hits = new List<KeywordMatch>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int i = 0;
        while (i < stream.Length)
        {
            if (stream[i] is not string head || !scoreByStem.ContainsKey(head))
            {
                i++;
                continue;
            }

            (KeywordMatch hit, int next) = GlueRun(stream, clean, i, scoreByStem, surface);
            if (seen.Add(hit.Phrase))
            {
                hits.Add(hit);
            }
            i = next;
        }

        hits.Sort((a, b) => b.Score.CompareTo(a.Score));
        return hits;
    }

    // The run starting at i: every stem in scoreByStem, consecutive in the raw stream,
    // with every entry past the head required clean.
    private static (KeywordMatch Hit, int Next) GlueRun(
        string?[] stream,
        bool[] clean,
        int i,
        Dictionary<string, double> scoreByStem,
        Dictionary<string, Dictionary<string, int>> surface)
    {
        int j = i;
        double total = 0;
        var parts = new List<string>();
        while (j < stream.Length && (j == i || clean[j])
               && stream[j] is string stem && scoreByStem.TryGetValue(stem, out double score))
        {
            parts.Add(Best(surface[stem]));
            total += score;
            j++;
        }

        string phrase = string.Join(" ", parts);
        return (new KeywordMatch(phrase, total / parts.Count), j);
    }

    private static string Best(Dictionary<string, int> counts) =>
        counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).First().Key;
}
