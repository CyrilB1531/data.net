using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DataNet.Text.Vectorization;

/// <summary>The kind of tokens a vectorizer extracts.</summary>
public enum AnalyzerKind
{
    /// <summary>Word tokens (via the token pattern), then word n-grams.</summary>
    Word,

    /// <summary>Character n-grams over the whole preprocessed string.</summary>
    Char,

    /// <summary>Character n-grams that do not cross word boundaries (words padded with spaces).</summary>
    CharWordBoundary,
}

/// <summary>
/// Turns a document into its sequence of terms, mirroring the preprocessing and
/// analysis pipeline of scikit-learn's text vectorizers.
/// </summary>
internal sealed class TextAnalyzer
{
    private readonly bool _lowercase;
    private readonly bool _stripAccents;
    private readonly AnalyzerKind _kind;
    private readonly int _minN;
    private readonly int _maxN;
    private readonly Regex _tokenPattern;
    private readonly HashSet<string>? _stopWords;

    public TextAnalyzer(
        bool lowercase,
        bool stripAccents,
        AnalyzerKind kind,
        (int Min, int Max) ngramRange,
        string tokenPattern,
        IReadOnlyCollection<string>? stopWords)
    {
        if (ngramRange.Min < 1 || ngramRange.Max < ngramRange.Min)
        {
            throw new ArgumentException("Invalid ngram range.", nameof(ngramRange));
        }

        _lowercase = lowercase;
        _stripAccents = stripAccents;
        _kind = kind;
        _minN = ngramRange.Min;
        _maxN = ngramRange.Max;
        _tokenPattern = new Regex(tokenPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        _stopWords = stopWords is null ? null : new HashSet<string>(stopWords, StringComparer.Ordinal);
    }

    /// <summary>Produces the terms of <paramref name="document"/> (with repetition).</summary>
    public List<string> Analyze(string document)
    {
        string s = Preprocess(document);
        return _kind switch
        {
            AnalyzerKind.Char => CharNgrams(s),
            AnalyzerKind.CharWordBoundary => CharWordBoundaryNgrams(s),
            _ => WordNgrams(Tokenize(s)),
        };
    }

    private string Preprocess(string document)
    {
        string s = document;
        if (_lowercase)
        {
            s = s.ToLowerInvariant();
        }
        if (_stripAccents)
        {
            s = StripAccents(s);
        }
        return s;
    }

    private static string StripAccents(string s)
    {
        string decomposed = s.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private List<string> Tokenize(string s)
    {
        var tokens = new List<string>();
        foreach (Match m in _tokenPattern.Matches(s))
        {
            string tok = m.Value;
            if (_stopWords is null || !_stopWords.Contains(tok))
            {
                tokens.Add(tok);
            }
        }
        return tokens;
    }

    private List<string> WordNgrams(List<string> tokens)
    {
        if (_minN == 1 && _maxN == 1)
        {
            return tokens;
        }

        var terms = new List<string>();
        int count = tokens.Count;
        for (int n = _minN; n <= _maxN; n++)
        {
            if (n == 1)
            {
                terms.AddRange(tokens);
                continue;
            }
            for (int i = 0; i + n <= count; i++)
            {
                terms.Add(string.Join(" ", tokens.GetRange(i, n)));
            }
        }
        return terms;
    }

    private List<string> CharNgrams(string s)
    {
        // scikit-learn collapses runs of whitespace to a single space for char analysis.
        s = CollapseWhitespace(s);
        var terms = new List<string>();
        int len = s.Length;
        for (int n = _minN; n <= _maxN; n++)
        {
            for (int i = 0; i + n <= len; i++)
            {
                terms.Add(s.Substring(i, n));
            }
        }
        return terms;
    }

    private List<string> CharWordBoundaryNgrams(string s)
    {
        // Mirrors scikit-learn's _char_wb_ngrams: always emit w[0:n] (clamped),
        // then slide; a word shorter than n is emitted once and breaks the n-loop.
        var terms = new List<string>();
        foreach (string rawWord in s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            string w = " " + rawWord + " ";
            int len = w.Length;
            for (int n = _minN; n <= _maxN; n++)
            {
                terms.Add(w.Substring(0, Math.Min(n, len)));
                int offset = 0;
                while (offset + n < len)
                {
                    offset++;
                    terms.Add(w.Substring(offset, n));
                }
                if (offset == 0)
                {
                    break;
                }
            }
        }
        return terms;
    }

    private static string CollapseWhitespace(string s)
    {
        var sb = new StringBuilder(s.Length);
        bool inSpace = false;
        foreach (char c in s)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!inSpace)
                {
                    sb.Append(' ');
                    inSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                inSpace = false;
            }
        }
        return sb.ToString();
    }
}
