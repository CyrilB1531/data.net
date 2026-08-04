using System;

namespace DataNet.Text.Stemming;

// SonarLint S3267: the suffix scans early-return and mutate in place, which
// Where cannot express — and they run per token.
#pragma warning disable S3267

/// <summary>
/// The RV region and the rules built on it, shared by Spanish, Portuguese and
/// Italian.
/// </summary>
/// <remarks>
/// French is deliberately not built on this: its RV carries the
/// <c>par</c>/<c>col</c>/<c>tap</c> prefix cases and is not the shared rule.
/// German has no RV at all and derives from <see cref="SnowballWorkerBase"/>.
/// </remarks>
internal abstract class RomanceSnowballWorker : SnowballWorkerBase
{
    /// <summary>Region RV.</summary>
    protected int Rv { get; }

    /// <param name="word">The word, already carrying any language-specific preprocessing.</param>
    /// <param name="isVowel">That language's vowel set.</param>
    protected RomanceSnowballWorker(string word, Func<char, bool> isVowel)
        : base(word, isVowel)
    {
        Rv = ComputeRv(word, isVowel);
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

    /// <summary>Deletes the suffix if it lies in RV.</summary>
    protected void DeleteIfInRv(int suffixLen)
    {
        if (InRv(suffixLen))
        {
            Delete(suffixLen);
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
