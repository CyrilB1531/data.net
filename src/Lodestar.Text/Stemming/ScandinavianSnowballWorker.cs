namespace Lodestar.Text.Stemming;

// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express.
#pragma warning disable S3267

/// <summary>What the Danish, Norwegian and Swedish Snowball algorithms share.</summary>
/// <remarks>
/// The three are one shape with three vocabularies: R1 only, floored at three letters, a
/// first step that deletes the longest suffix from a list or an <c>s</c> preceded by a
/// letter the language calls valid, and a second that drops the last letter of a short
/// ending. The lists differ; the machinery does not, which is why it is here rather than
/// in each — the reason <see cref="RomanceSnowballWorker"/> exists for the Romance three.
/// </remarks>
internal abstract class ScandinavianSnowballWorker : SnowballWorkerBase
{
    private readonly Func<char, bool> _isSEnding;

    /// <param name="word">The word, already lowercased.</param>
    /// <param name="isVowel">That language's vowel set.</param>
    /// <param name="isSEnding">The letters a deletable <c>s</c> may follow.</param>
    protected ScandinavianSnowballWorker(string word, Func<char, bool> isVowel, Func<char, bool> isSEnding)
        : base(word, isVowel, minR1: 3)
    {
        _isSEnding = isSEnding;
    }

    /// <summary>Whether the letter before a final <c>s</c> lets that <c>s</c> go.</summary>
    /// <remarks>
    /// Norwegian widens this with a rule of its own — a <c>k</c> counts only when no vowel
    /// precedes it — so the test is virtual rather than the set being the whole answer.
    /// </remarks>
    protected virtual bool ValidSEnding()
    {
        int at = S.Length - 2;
        return at >= 0 && _isSEnding(S[at]);
    }

    /// <summary>Deletes the longest listed suffix lying in R1, or a valid final <c>s</c>.</summary>
    /// <remarks>
    /// The <c>s</c> is tried only when no listed suffix matched: it is the shortest rule of
    /// the step, and the algorithm takes the longest match across the whole step.
    /// </remarks>
    protected void Step1(string[] suffixes)
    {
        string? found = LongestSuffixInR1(suffixes);
        if (found is not null && found != "s")
        {
            Delete(found.Length);
            return;
        }

        if (Ends("s") && InR1(1) && ValidSEnding())
        {
            Delete(1);
        }
    }

    /// <summary>Drops the last letter when the word ends, inside R1, with one of these.</summary>
    protected void DropLastOf(string[] endings)
    {
        foreach (string ending in endings)
        {
            if (Ends(ending) && InR1(ending.Length))
            {
                Delete(1);
                return;
            }
        }
    }
}
