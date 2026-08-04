using System;

namespace DataNet.Text.Stemming;


// CA1845 (use span-based string.Concat): that overload does not exist on
// netstandard2.0. The Substring form is what makes this file compile there —
// the same reason the four language stemmers carry this suppression.
// SonarLint S3267: the suffix scans early-return and mutate in place, which
// Where cannot express — and they run per token, where a LINQ pipeline would
// allocate on every call.
#pragma warning disable S3267
#pragma warning disable CA1845
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

    /// <summary>What to do with a matched suffix of the given length.</summary>
    protected delegate void SuffixAction(int suffixLength);

    /// <summary>One line of a Snowball step: a set of suffixes and the action they trigger.</summary>
    protected readonly struct SuffixRule
    {
        /// <summary>The suffixes this rule matches.</summary>
        public string[] Suffixes { get; }

        /// <summary>What to do when one of them is the longest match.</summary>
        public SuffixAction Action { get; }

        /// <summary>Creates a rule.</summary>
        public SuffixRule(string[] suffixes, SuffixAction action)
        {
            Suffixes = suffixes;
            Action = action;
        }
    }

    /// <summary>
    /// Runs the action of whichever rule owns the longest suffix ending the word.
    /// </summary>
    /// <remarks>
    /// The published steps are tables of "longest among these suffixes, then do
    /// this", and the groups overlap — Spanish "amente" has to beat "mente",
    /// Italian "azione" has to beat "ione". Matching per group in sequence gets
    /// that wrong, so the longest is chosen across every rule at once.
    /// </remarks>
    protected void ApplyLongestRule(SuffixRule[] rules)
    {
        string? best = null;
        SuffixAction? action = null;
        foreach (SuffixRule rule in rules)
        {
            string? candidate = LongestSuffix(rule.Suffixes);
            if (candidate is not null && (best is null || candidate.Length > best.Length))
            {
                best = candidate;
                action = rule.Action;
            }
        }
        action?.Invoke(best!.Length);
    }

    /// <summary>Deletes the suffix if it lies in R2.</summary>
    protected void DeleteIfInR2(int suffixLen)
    {
        if (InR2(suffixLen))
        {
            Delete(suffixLen);
        }
    }

    /// <summary>Deletes the suffix if it lies in RV.</summary>
    protected void DeleteIfInRv(int suffixLen)
    {
        if (InRv(suffixLen))
        {
            Delete(suffixLen);
        }
    }

    /// <summary>Replaces the suffix if it lies in R2.</summary>
    protected void ReplaceIfInR2(int suffixLen, string replacement)
    {
        if (InR2(suffixLen))
        {
            Replace(suffixLen, replacement);
        }
    }

    /// <summary>
    /// Deletes the suffix if it lies in R2, then strips the first of
    /// <paramref name="preceders"/> that now ends the word and also lies in R2.
    /// </summary>
    protected void DeleteInR2ThenStrip(int suffixLen, string[] preceders)
    {
        if (!InR2(suffixLen))
        {
            return;
        }
        Delete(suffixLen);
        foreach (string pre in preceders)
        {
            if (Ends(pre) && InR2(pre.Length))
            {
                Delete(pre.Length);
                return;
            }
        }
    }

    /// <summary>
    /// The "-amente" branch, identical in shape across the Romance algorithms:
    /// delete in R1, then strip "iv" (and "at" behind it) in R2, otherwise strip the
    /// first of <paramref name="others"/> in R2. Only the last list differs.
    /// </summary>
    protected void StripAmente(string[] others)
    {
        if (!InR1(6))
        {
            return;
        }
        Delete(6);
        if (Ends("iv") && InR2(2))
        {
            Delete(2);
            if (Ends("at") && InR2(2))
            {
                Delete(2);
            }
            return;
        }
        foreach (string pre in others)
        {
            if (Ends(pre) && InR2(pre.Length))
            {
                Delete(pre.Length);
                return;
            }
        }
    }

    /// <summary>The "-mente" branch: delete in R2, then strip a preceding marker in R2.</summary>
    protected void StripMente(string[] preceders) => DeleteInR2ThenStrip(5, preceders);

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
