using System.Text;

namespace Lodestar.Text.Stemming;

// SonarLint S3776: cognitive complexity: faithful port of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express.
// CA1307 (specify StringComparison): the overload it asks for does not exist on
// netstandard2.0, which this assembly targets; both calls are ordinal anyway.
// CA1308 (normalize to uppercase): Snowball is *defined* on lowercase input — the
// published algorithm, the reference implementations and the oracle corpora all
// lowercase first, so ToUpperInvariant would return a wrong stem.
#pragma warning disable S3776, S3267, CA1307, CA1308

/// <summary>
/// The Dutch Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("dutch")</c>. An original
/// implementation of the published Snowball algorithm: accents are folded away, an initial
/// <c>y</c>, a <c>y</c> after a vowel and an <c>i</c> between vowels are held as consonants,
/// R1 is floored at three letters, and a doubled <c>kk</c>, <c>dd</c> or <c>tt</c> left by a
/// deletion is undoubled — see <c>docs/equivalence.md</c>'s stemming row. Thread-safe.
/// </remarks>
public static class DutchSnowballStemmer
{
    /// <summary>Returns the Dutch Snowball stem of <paramref name="word"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="word"/> is null.</exception>
    public static string Stem(string word)
    {
        Guard.NotNull(word);
        // Compose accents (NFC) so 'ë' is one code point, as the folding below expects.
        string s = word.ToLowerInvariant().Normalize(NormalizationForm.FormC);
        // Step 0 folds accents whatever the length, so a word too short to have regions
        // still comes back folded rather than untouched.
        string folded = FoldAccents(s);
        return folded.Length < 3 ? folded : new Worker(folded).Run();
    }

    /// <summary>Step 0's first half: the acute and the diaeresis carry no meaning here.</summary>
    private static string FoldAccents(string s)
    {
        var b = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            b.Append(c switch
            {
                'ä' or 'á' => 'a',
                'ë' or 'é' => 'e',
                'ï' or 'í' => 'i',
                'ö' or 'ó' => 'o',
                'ü' or 'ú' => 'u',
                _ => c,
            });
        }

        return b.ToString();
    }

    private sealed class Worker : SnowballWorkerBase
    {
        private static readonly Func<char, bool> Vowels = IsVowelChar;

        private static bool IsVowelChar(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y' or 'è';

        /// <summary>Step 0's second half: the letters that act as consonants are held in upper case.</summary>
        /// <remarks>
        /// A <c>y</c> initially or after a vowel, and an <c>i</c> between two vowels, are consonants
        /// for the purpose of the regions. Upper case is how the algorithm carries that, and
        /// <see cref="Undecorate"/> puts it back at the end.
        /// </remarks>
        private static string MarkConsonants(string s)
        {
            var b = new StringBuilder(s);
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] == 'y' && (i == 0 || IsVowelChar(b[i - 1])))
                {
                    b[i] = 'Y';
                }
                else if (b[i] == 'i' && i > 0 && i + 1 < b.Length
                         && IsVowelChar(b[i - 1]) && IsVowelChar(b[i + 1]))
                {
                    b[i] = 'I';
                }
            }

            return b.ToString();
        }

        private static string Undecorate(string s) => s.Replace('Y', 'y').Replace('I', 'i');

        private bool _eRemoved;

        // The region before R1 holds at least three letters, as in German.
        public Worker(string folded) : base(MarkConsonants(folded), Vowels, minR1: 3)
        {
        }

        public string Run()
        {
            Step1();
            Step2();
            Step3a();
            Step3b();
            Step4();
            return Undecorate(S);
        }

        /// <summary>A non-vowel other than <c>j</c> may precede a deleted <c>s</c>.</summary>
        private bool ValidSEnding(int suffixLen)
        {
            int at = S.Length - suffixLen - 1;
            return at >= 0 && !IsVowel(S[at]) && S[at] != 'j';
        }

        /// <summary>A non-vowel may precede a deleted <c>en</c>, unless the word ends <c>gem</c> there.</summary>
        private bool ValidEnEnding(int suffixLen)
        {
            int at = S.Length - suffixLen - 1;
            if (at < 0 || IsVowel(S[at]))
            {
                return false;
            }

            return !S.Substring(0, at + 1).EndsWith("gem", StringComparison.Ordinal);
        }

        /// <summary>Removes the second of a doubled <c>kk</c>, <c>dd</c> or <c>tt</c>.</summary>
        private void Undouble()
        {
            if (S.Length >= 2 && S[^1] == S[^2] && (S[^1] is 'k' or 'd' or 't'))
            {
                Delete(1);
            }
        }

        private void Step1()
        {
            if (Ends("heden"))
            {
                if (InR1(5))
                {
                    Replace(5, "heid");
                }

                return;
            }

            foreach (string suffix in new[] { "ene", "en" })
            {
                if (Ends(suffix) && InR1(suffix.Length) && ValidEnEnding(suffix.Length))
                {
                    Delete(suffix.Length);
                    Undouble();
                    return;
                }
            }

            foreach (string suffix in new[] { "se", "s" })
            {
                if (Ends(suffix) && InR1(suffix.Length) && ValidSEnding(suffix.Length))
                {
                    Delete(suffix.Length);
                    return;
                }
            }
        }

        private void Step2()
        {
            if (Ends("e") && InR1(1) && S.Length >= 2 && !IsVowel(S[^2]))
            {
                Delete(1);
                Undouble();
                _eRemoved = true;
            }
        }

        private void Step3a()
        {
            if (!Ends("heid") || !InR2(4) || (S.Length >= 5 && S[^5] == 'c'))
            {
                return;
            }

            Delete(4);
            if (Ends("en") && InR1(2) && ValidEnEnding(2))
            {
                Delete(2);
                Undouble();
            }
        }

        private void Step3b()
        {
            if (Ends("end") || Ends("ing"))
            {
                if (!InR2(3))
                {
                    return;
                }

                Delete(3);
                if (Ends("ig") && InR2(2) && !(S.Length >= 3 && S[^3] == 'e'))
                {
                    Delete(2);
                }
                else
                {
                    Undouble();
                }

                return;
            }

            if (Ends("ig"))
            {
                if (InR2(2) && !(S.Length >= 3 && S[^3] == 'e'))
                {
                    Delete(2);
                }

                return;
            }

            if (Ends("lijk"))
            {
                if (InR2(4))
                {
                    Delete(4);
                    Step2();
                }

                return;
            }

            if (Ends("baar"))
            {
                if (InR2(4))
                {
                    Delete(4);
                }

                return;
            }

            if (Ends("bar") && InR2(3) && _eRemoved)
            {
                Delete(3);
            }
        }

        /// <summary>Undoubles a vowel in a final <c>CVVD</c>, where D is a non-vowel other than <c>I</c>.</summary>
        private void Step4()
        {
            if (S.Length < 4)
            {
                return;
            }

            char d = S[^1], v2 = S[^2], v1 = S[^3], c = S[^4];
            if (!IsVowel(d) && d != 'I' && v1 == v2 && (v1 is 'a' or 'e' or 'o' or 'u') && !IsVowel(c))
            {
                S = S.Substring(0, S.Length - 3) + v1 + d;
            }
        }
    }
}
