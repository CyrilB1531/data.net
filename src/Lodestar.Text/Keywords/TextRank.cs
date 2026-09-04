using System.Text.RegularExpressions;
using Lodestar.Text.Stemming;
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Keywords;

// CA1308 (normalize to uppercase): Scan asks whether the source already spelled a token
// lower-case, the same question PhraseTokenizer asks of stop words; ToUpperInvariant
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
    private readonly StopWordSet _stopWords;
    private readonly Regex _rawToken;

    /// <summary>Builds an extractor.</summary>
    /// <param name="options">Null takes every default.</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>Window</c> is below 1, <c>Damping</c> is outside <c>(0, 1)</c>, <c>Ratio</c> is outside <c>(0, 1]</c>, <c>MaxIterations</c> is below 1, or <c>Words</c> is set and negative.</exception>
    public TextRank(TextRankOptions? options = null)
    {
        _options = options ?? new TextRankOptions();
        Guard.NotLessThan(_options.Window, 1);
        Guard.NotLessThan(_options.MaxIterations, 1);
        if (_options.Words is { } words)
        {
            Guard.NotLessThan(words, 0);
        }
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
        _rawToken = new Regex(_options.TokenPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);
    }

    /// <summary>Extracts the ranked keywords of one document.</summary>
    /// <param name="text">The document.</param>
    /// <returns>
    /// Keywords in descending score, glued where their parts were adjacent; a tie keeps the
    /// order gluing produced it in -- summa's own sort is Python's, which is stable.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The ranking did not converge within <c>MaxIterations</c>.</exception>
    public IReadOnlyList<KeywordMatch> Extract(string text)
    {
        Guard.NotNull(text);

        (string[] words, bool[] clean) = Scan(text);
        string?[] stream = BuildStream(words);

        var graph = new WordGraph(stream, _options.Window);
        if (graph.Nodes.Count == 0)
        {
            return [];
        }

        double[] ranked = graph.Rank(_options.Damping, _options.Tolerance, _options.MaxIterations);
        Dictionary<string, double> scoreByStem = TopStems(graph.Nodes, ranked);

        return Glue(stream, words, clean, scoreByStem);
    }

    // One pass over the source: each match yields its spelling and cleanliness together,
    // so the two can never disagree the way two passes over differently-cased text could.
    private (string[] Words, bool[] Clean) Scan(string text)
    {
        var words = new List<string>();
        var clean = new List<bool>();
        foreach (Match m in _rawToken.Matches(text))
        {
            words.Add(m.Value.ToLowerInvariant());
            bool precededByGap = m.Index == 0 || char.IsWhiteSpace(text[m.Index - 1]);
            int end = m.Index + m.Length;
            bool followedByGap = end == text.Length || char.IsWhiteSpace(text[end]);
            clean.Add(precededByGap && followedByGap && string.Equals(m.Value, m.Value.ToLowerInvariant(), StringComparison.Ordinal));
        }

        return (words.ToArray(), clean.ToArray());
    }

    // One entry per raw token, never compacted: the stem where the word is kept, null
    // where a stop word stood, so the co-occurrence window still counts its position.
    private string?[] BuildStream(string[] words)
    {
        var stream = new string?[words.Length];
        for (int i = 0; i < words.Length; i++)
        {
            if (_stopWords.Contains(words[i]))
            {
                continue;
            }

            stream[i] = EnglishSnowballStemmer.Stem(words[i]);
        }

        return stream;
    }

    private Dictionary<string, double> TopStems(IReadOnlyList<string> nodes, double[] ranked)
    {
        // Words is guarded non-negative at construction; only the upper bound is left to enforce here.
        int take = Math.Min(_options.Words ?? (int)(nodes.Count * _options.Ratio), nodes.Count);

        return nodes
            .Select((stem, i) => (stem, score: ranked[i]))
            .OrderByDescending(p => p.score)
            .Take(take)
            .ToDictionary(p => p.stem, p => p.score, StringComparer.Ordinal);
    }

    // Adjacent selected stems become one phrase, scored by the mean of their parts. A
    // spelling already spent by an earlier phrase behaves as unselected — summa's `pop`.
    private static List<KeywordMatch> Glue(
        string?[] stream,
        string[] words,
        bool[] clean,
        Dictionary<string, double> scoreByStem)
    {
        var hits = new List<KeywordMatch>();
        var consumed = new HashSet<string>(StringComparer.Ordinal);
        int i = 0;
        while (i < stream.Length)
        {
            if (stream[i] is not string head || !scoreByStem.ContainsKey(head) || consumed.Contains(words[i]))
            {
                i++;
                continue;
            }

            (KeywordMatch? hit, int next) = GlueRun(stream, words, clean, i, scoreByStem, consumed);
            if (hit is { } value)
            {
                hits.Add(value);
            }
            i = next;
        }

        // summa's sort is Python's, which is stable, so a tie keeps Glue's own order --
        // an unstable in-place sort (introsort) must not stand in for it here.
        return [.. hits.OrderByDescending(hit => hit.Score)];
    }

    // A continuation must be clean, its own spelling unconsumed, and new to this run. A
    // run that only stops because the document ran out reports and spends nothing.
    private static (KeywordMatch? Hit, int Next) GlueRun(
        string?[] stream,
        string[] words,
        bool[] clean,
        int i,
        Dictionary<string, double> scoreByStem,
        HashSet<string> consumed)
    {
        int j = i;
        double total = 0;
        var parts = new List<string>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        while (j < stream.Length && (j == i || clean[j])
               && stream[j] is string stem
               && scoreByStem.TryGetValue(stem, out double score)
               && !consumed.Contains(words[j])
               && used.Add(words[j]))
        {
            parts.Add(words[j]);
            total += score;
            j++;
        }

        if (j == stream.Length && parts.Count > 1)
        {
            return (null, i + 1);
        }

        consumed.UnionWith(used);
        string phrase = string.Join(" ", parts);
        return (new KeywordMatch(phrase, total / parts.Count), j);
    }
}
