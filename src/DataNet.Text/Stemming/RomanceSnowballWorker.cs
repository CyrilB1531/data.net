using System;

namespace DataNet.Text.Stemming;

/// <summary>
/// The Snowball scaffolding shared by the Romance stemmers.
/// </summary>
/// <remarks>
/// <para>
/// What lives here is the Snowball <em>framework</em>, not any language's
/// algorithm: the R1/R2/RV region definitions and the suffix primitives are
/// identical across Spanish, Portuguese and Italian by construction. Each
/// language keeps its own vowel set and its own steps — which suffixes, in which
/// order, under which region condition — so the one-to-one reading against the
/// published algorithm survives in the file where it matters.
/// </para>
/// <para>
/// French is deliberately not built on this: its RV rule carries the
/// <c>par</c>/<c>col</c>/<c>tap</c> prefix cases and does not match the shared one.
/// </para>
/// </remarks>
internal abstract class RomanceSnowballWorker
{
    private readonly Func<char, bool> _isVowel;

    /// <summary>The word being stemmed, mutated in place by the steps.</summary>
    protected string S;

    /// <summary>Region RV.</summary>
    protected int Rv { get; }

    /// <summary>Region R1.</summary>
    protected int R1 { get; }

    /// <summary>Region R2.</summary>
    protected int R2 { get; }

    /// <param name="word">
    /// The word, already carrying any language-specific preprocessing — Portuguese
    /// expands its nasals before regions are computed, and the regions must see the
    /// transformed form.
    /// </param>
    /// <param name="isVowel">That language's vowel set.</param>
    protected RomanceSnowballWorker(string word, Func<char, bool> isVowel)
    {
        S = word;
        _isVowel = isVowel;
        R1 = Region(word, 0, isVowel);
        R2 = Region(word, R1, isVowel);
        Rv = ComputeRv(word, isVowel);
    }

    /// <summary>Whether <paramref name="c"/> is a vowel in this language.</summary>
    protected bool IsVowel(char c) => _isVowel(c);

    /// <summary>The region after the first consonant that follows a vowel, from <paramref name="from"/>.</summary>
    protected static int Region(string s, int from, Func<char, bool> isVowel)
    {
        int i = from;
        while (i < s.Length && !isVowel(s[i]))
        {
            i++;
        }
        while (i < s.Length && isVowel(s[i]))
        {
            i++;
        }
        return i < s.Length ? i + 1 : s.Length;
    }

    /// <summary>
    /// RV as defined for the Romance algorithms: if the second letter is a
    /// consonant, the region after the next vowel; if the first two are vowels, the
    /// region after the next consonant; otherwise after the third letter.
    /// </summary>
    protected static int ComputeRv(string s, Func<char, bool> isVowel)
    {
        int n = s.Length;
        if (n < 2)
        {
            return n;
        }

        if (!isVowel(s[1]))
        {
            int i = 2;
            while (i < n && !isVowel(s[i]))
            {
                i++;
            }
            return i < n ? i + 1 : n;
        }

        if (isVowel(s[0]))
        {
            int i = 2;
            while (i < n && isVowel(s[i]))
            {
                i++;
            }
            return i < n ? i + 1 : n;
        }

        return Math.Min(3, n);
    }

    /// <summary>Whether a suffix of this length starts at or after RV.</summary>
    protected bool InRv(int suffixLen) => S.Length - suffixLen >= Rv;

    /// <summary>Whether a suffix of this length starts at or after R1.</summary>
    protected bool InR1(int suffixLen) => S.Length - suffixLen >= R1;

    /// <summary>Whether a suffix of this length starts at or after R2.</summary>
    protected bool InR2(int suffixLen) => S.Length - suffixLen >= R2;

    /// <summary>Whether the word currently ends with <paramref name="suffix"/>.</summary>
    protected bool Ends(string suffix) => S.EndsWith(suffix, StringComparison.Ordinal);

    /// <summary>Removes the last <paramref name="len"/> characters.</summary>
    protected void Delete(int len) => S = S.Substring(0, S.Length - len);

    /// <summary>Replaces a suffix of <paramref name="suffixLen"/> characters with <paramref name="repl"/>.</summary>
    protected void Replace(int suffixLen, string repl) => S = S.Substring(0, S.Length - suffixLen) + repl;

    /// <summary>The longest candidate that ends the word, or null.</summary>
    protected string? LongestSuffix(string[] candidates)
    {
        string? best = null;
        foreach (string c in candidates)
        {
            if (Ends(c) && (best is null || c.Length > best.Length))
            {
                best = c;
            }
        }
        return best;
    }

    /// <summary>
    /// The longest candidate that both ends the word and lies inside RV. A candidate
    /// rejected by RV does not end the search — Portuguese "amáveis" must fall
    /// through from "áveis", which is outside RV, to "eis".
    /// </summary>
    protected string? LongestSuffixInRv(string[] candidates)
    {
        string? best = null;
        foreach (string c in candidates)
        {
            if (Ends(c) && InRv(c.Length) && (best is null || c.Length > best.Length))
            {
                best = c;
            }
        }
        return best;
    }
}
