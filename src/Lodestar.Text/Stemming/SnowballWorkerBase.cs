using System;

namespace Lodestar.Text.Stemming;

// SonarLint S3267: the suffix scans early-return and mutate in place, which
// Where cannot express — and they run per token, where a LINQ pipeline would
// allocate on every call.
#pragma warning disable S3267
// CA1845 (use span-based string.Concat): that overload does not exist on
// netstandard2.0. The Substring form is what makes this file compile there.
#pragma warning disable CA1845

/// <summary>
/// The Snowball scaffolding common to every language: the R1/R2 regions, the
/// suffix primitives, and the rule table the published steps are written as.
/// </summary>
/// <remarks>
/// This holds the framework, never an algorithm. Which suffixes are stripped, in
/// what order and under which condition stays in each language's own file, so it
/// still reads one-to-one against the published description.
/// </remarks>
internal abstract class SnowballWorkerBase
{
    private readonly Func<char, bool> _isVowel;

    /// <summary>The word being stemmed, mutated in place by the steps.</summary>
    protected string S;

    /// <summary>Region R1.</summary>
    protected int R1 { get; }

    /// <summary>Region R2.</summary>
    protected int R2 { get; }

    /// <param name="word">The word, already carrying any language-specific preprocessing.</param>
    /// <param name="isVowel">That language's vowel set.</param>
    /// <param name="minR1">
    /// A floor for R1. German requires the region before R1 to hold at least three
    /// letters; the Romance algorithms impose no floor.
    /// </param>
    protected SnowballWorkerBase(string word, Func<char, bool> isVowel, int minR1 = 0)
    {
        S = word;
        _isVowel = isVowel;
        R1 = Math.Max(Region(word, 0, isVowel), minR1);
        R2 = Region(word, R1, isVowel);
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

    /// <summary>Runs the action of whichever rule owns the longest suffix ending the word.</summary>
    /// <remarks>
    /// The published steps are tables of "longest among these suffixes, then do
    /// this", and the groups overlap — Spanish "amente" has to beat "mente".
    /// Matching per group in sequence gets that wrong, so the longest is chosen
    /// across every rule at once.
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

    /// <summary>Deletes the suffix if it lies in R1.</summary>
    protected void DeleteIfInR1(int suffixLen)
    {
        if (InR1(suffixLen))
        {
            Delete(suffixLen);
        }
    }

    /// <summary>Deletes the suffix if it lies in R2.</summary>
    protected void DeleteIfInR2(int suffixLen)
    {
        if (InR2(suffixLen))
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
}
