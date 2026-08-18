using System.Text;

namespace Lodestar.Text.Stemming;

// SonarLint S3776: cognitive complexity: faithful port of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express.
// CA1307 (specify StringComparison): the overload it asks for —
// string.IndexOf(char, StringComparison) / string.Replace(string, string?,
// StringComparison) — does not exist on netstandard2.0, which this assembly
// targets. Both calls are ordinal on every runtime that has them, so the
// suggestion would change nothing but the compilation.
// CA1308 (normalize to uppercase): Snowball and Porter are *defined* on
// lowercase input — the published algorithms, the reference implementations and
// the oracle corpora this suite is checked against all lowercase first.
// ToUpperInvariant would return different stems, which is a wrong answer rather
// than a differently-cased one.
#pragma warning disable S3776, S3267, CA1307, CA1308

/// <summary>
/// The German Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("german")</c>. An
/// original implementation of the published Snowball algorithm: no RV region, R1
/// floored so the region before it holds at least three letters, and suffix
/// conditions test the preceding letter rather than a region alone — see
/// <c>docs/equivalence.md</c>'s stemming row. Input is lowercased. Thread-safe.
/// </remarks>
public static class GermanSnowballStemmer
{
    /// <summary>Returns the German Snowball stem of <paramref name="word"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="word"/> is null.</exception>
    public static string Stem(string word)
    {
        Guard.NotNull(word);
        // Compose accents (NFC) so 'ä' etc. are single code points, as the rules expect.
        string s = word.ToLowerInvariant().Normalize(NormalizationForm.FormC);
        if (s.Length < 2)
        {
            return s;
        }
        return new Worker(s).Run();
    }

    private sealed class Worker : SnowballWorkerBase
    {
        private static readonly Func<char, bool> Vowels = c =>
            c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y' or 'ä' or 'ö' or 'ü';

        // The region before R1 must hold at least three letters, unlike the
        // Romance algorithms, which impose no floor.
        public Worker(string s) : base(Preprocess(s), Vowels, minR1: 3)
        {
        }

        public string Run()
        {
            Step1();
            Step2();
            Step3();
            return Finish(S);
        }

        /// <summary>Sharp s expands, then u/y between vowels are marked as consonants.</summary>
        private static string Preprocess(string s)
        {
            string expanded = s.Replace("ß", "ss");
            char[] a = expanded.ToCharArray();
            for (int i = 1; i + 1 < a.Length; i++)
            {
                if ((a[i] == 'u' || a[i] == 'y') && Vowels(expanded[i - 1]) && Vowels(expanded[i + 1]))
                {
                    a[i] = a[i] == 'u' ? 'U' : 'Y';
                }
            }
            return new string(a);
        }

        /// <summary>Unmark u/y, then drop the umlauts.</summary>
        private static string Finish(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                sb.Append(c switch
                {
                    'U' => 'u',
                    'Y' => 'y',
                    'ä' => 'a',
                    'ö' => 'o',
                    'ü' => 'u',
                    _ => c,
                });
            }
            return sb.ToString();
        }

        private static bool IsValidSEnding(char c) => c is 'b' or 'd' or 'f' or 'g' or 'h' or 'k' or 'l' or 'm' or 'n' or 'r' or 't';

        // Same set as the s-ending minus 'r'.
        private static bool IsValidStEnding(char c) => c is 'b' or 'd' or 'f' or 'g' or 'h' or 'k' or 'l' or 'm' or 'n' or 't';

        private static readonly string[] Step1Group1 = ["ern", "em", "er"];
        private static readonly string[] Step1Group2 = ["en", "es", "e"];

        private void Step1()
        {
            string? a = LongestSuffix(Step1Group1);
            string? b = LongestSuffix(Step1Group2);

            // Longest wins; on a tie the (a) group is the more specific rule.
            if (a is not null && (b is null || a.Length >= b.Length))
            {
                DeleteIfInR1(a.Length);
                return;
            }

            if (b is not null)
            {
                if (!InR1(b.Length))
                {
                    return;
                }
                Delete(b.Length);
                // A group (b) deletion that uncovers "niss" loses the final s.
                if (Ends("niss"))
                {
                    Delete(1);
                }
                return;
            }

            // (c) a bare s, but only after a valid s-ending — which need not be in R1.
            if (Ends("s") && InR1(1) && S.Length >= 2 && IsValidSEnding(S[S.Length - 2]))
            {
                Delete(1);
            }
        }

        private static readonly string[] Step2Suffixes = ["est", "en", "er"];

        private void Step2()
        {
            string? hit = LongestSuffix(Step2Suffixes);
            if (hit is not null)
            {
                DeleteIfInR1(hit.Length);
                return;
            }

            // "st" needs a valid st-ending with at least three letters before it,
            // so "st" is not stripped from short words like "ist".
            if (Ends("st") && InR1(2) && S.Length >= 6 && IsValidStEnding(S[S.Length - 3]))
            {
                Delete(2);
            }
        }

        private static readonly string[] Step3EndErn = ["end", "ern"];
        private static readonly string[] Step3IgIk = ["ig", "ik", "isch"];
        private static readonly string[] Step3LichHeit = ["lich", "heit"];
        private static readonly string[] Step3Keit = ["keit"];

        private void Step3() => ApplyLongestRule(
        [
            new(Step3EndErn, DeleteIfInR2),
            new(Step3IgIk, DeleteIgIk),
            new(Step3LichHeit, DeleteLichHeit),
            new(Step3Keit, n => DeleteInR2ThenStrip(n, ["lich", "ig"])),
        ]);

        /// <summary>"ig"/"ik"/"isch" go in R2, but never straight after an e.</summary>
        private void DeleteIgIk(int n)
        {
            if (S.Length > n && S[S.Length - n - 1] == 'e')
            {
                return;
            }
            DeleteIfInR2(n);
        }

        /// <summary>"lich"/"heit" go in R2; a preceding "er"/"en" then goes if in R1.</summary>
        private void DeleteLichHeit(int n)
        {
            if (!InR2(n))
            {
                return;
            }
            Delete(n);
            foreach (string pre in new[] { "er", "en" })
            {
                if (Ends(pre) && InR1(2))
                {
                    Delete(2);
                    return;
                }
            }
        }
    }
}
