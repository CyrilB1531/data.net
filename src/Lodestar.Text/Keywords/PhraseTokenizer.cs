using System.Text.RegularExpressions;
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Keywords;

// CA1308 (normalize to uppercase): tokens are matched against StopWordSet, which
// compares lower-case ordinal; ToUpperInvariant would change what a stop word matches.
#pragma warning disable CA1308

/// <summary>
/// Splits a document into the word runs between stop words and punctuation.
/// </summary>
/// <remarks>
/// Not <c>TextAnalyzer</c>: that one discards stop words, and a run's boundary is
/// exactly where one stood. RAKE's candidates are these runs.
/// </remarks>
internal sealed class PhraseTokenizer
{
    // Anything that is not a token character ends a run, which is what makes
    // "red, green" two candidates rather than one two-word phrase.
    private readonly Regex _token;
    private readonly StopWordSet _stopWords;

    public PhraseTokenizer(IReadOnlyCollection<string> stopWords, string tokenPattern)
    {
        _stopWords = StopWordSet.Adopt(stopWords);
        _token = new Regex(tokenPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);
    }

    /// <summary>Every token of the document, lower-cased, in order, stop words included.</summary>
    public IReadOnlyList<string> Words(string text)
    {
        var words = new List<string>();
        foreach (Match m in _token.Matches(text.ToLowerInvariant()))
        {
            words.Add(m.Value);
        }
        return words;
    }

    /// <summary>The runs of non-stop-word tokens, in order, split at every stop word and every gap.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Split(string text)
    {
        string lowered = text.ToLowerInvariant();
        var runs = new List<IReadOnlyList<string>>();
        var current = new List<string>();
        int previousEnd = -1;

        foreach (Match m in _token.Matches(lowered))
        {
            bool gap = previousEnd >= 0 && HasNonSpace(lowered, previousEnd, m.Index);
            if (gap || _stopWords.Contains(m.Value))
            {
                Flush(runs, current);
            }

            if (!_stopWords.Contains(m.Value))
            {
                current.Add(m.Value);
            }
            previousEnd = m.Index + m.Length;
        }

        Flush(runs, current);
        return runs;
    }

    private static bool HasNonSpace(string s, int from, int to)
    {
        for (int i = from; i < to; i++)
        {
            if (!char.IsWhiteSpace(s[i]))
            {
                return true;
            }
        }
        return false;
    }

    private static void Flush(List<IReadOnlyList<string>> runs, List<string> current)
    {
        if (current.Count > 0)
        {
            runs.Add(current.ToArray());
            current.Clear();
        }
    }
}
