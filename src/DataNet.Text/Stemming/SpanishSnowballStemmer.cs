using System.Text;

namespace DataNet.Text.Stemming;

// SonarLint S3776: cognitive complexity: faithful port of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express.
// CA1845 (use span-based string.Concat): that overload does not exist on
// netstandard2.0. The Substring form is what makes this file compile there.
#pragma warning disable CA1845
#pragma warning disable S3776, S3267

/// <summary>
/// The Spanish Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("spanish")</c>. An
/// original implementation of the published Snowball algorithm, using the RV/R1/R2
/// regions and the standard step ordering. Input is lowercased. Thread-safe.
/// </para>
/// <para>
/// Spanish adds a step the English algorithm has no equivalent for: attached
/// object pronouns (<c>dá<b>melo</b></c>, <c>hacién<b>dola</b></c>) are removed
/// before any suffix stripping. Acute accents are dropped at the very end, so
/// intermediate steps still see them.
/// </para>
/// </remarks>
public static class SpanishSnowballStemmer
{
    /// <summary>Returns the Spanish Snowball stem of <paramref name="word"/>.</summary>
    public static string Stem(string word)
    {
        Guard.NotNull(word);
        // Compose accents (NFC) so 'á' etc. are single code points, as the rules expect.
        string s = word.ToLowerInvariant().Normalize(NormalizationForm.FormC);
        if (s.Length < 2)
        {
            return s;
        }
        return new Worker(s).Run();
    }

    private sealed class Worker
    {
        private string _s;
        private readonly int _rv;
        private readonly int _r1;
        private readonly int _r2;

        public Worker(string s)
        {
            _s = s;
            _r1 = Region(_s, 0);
            _r2 = Region(_s, _r1);
            _rv = ComputeRv(_s);
        }

        public string Run()
        {
            // Steps 0 and 1 always run. Step 2a only if step 1 removed nothing,
            // step 2b only if step 2a removed nothing (Snowball semantics: what
            // matters is whether the word ALTERED, not whether a suffix matched).
            Step0();

            string before = _s;
            Step1();
            if (_s == before)
            {
                before = _s;
                Step2a();
                if (_s == before)
                {
                    Step2b();
                }
            }

            Step3();
            return RemoveAcuteAccents(_s);
        }

        private static bool IsVowel(char c) =>
            c is 'a' or 'e' or 'i' or 'o' or 'u' or 'á' or 'é' or 'í' or 'ó' or 'ú' or 'ü';

        /// <summary>The region after the first consonant following a vowel, from <paramref name="from"/>.</summary>
        private static int Region(string s, int from)
        {
            int i = from;
            while (i < s.Length && !IsVowel(s[i]))
            {
                i++;
            }
            while (i < s.Length && IsVowel(s[i]))
            {
                i++;
            }
            return i < s.Length ? i + 1 : s.Length;
        }

        private static int ComputeRv(string s)
        {
            int n = s.Length;
            if (n < 2)
            {
                return n;
            }

            // Second letter a consonant -> after the next following vowel.
            if (!IsVowel(s[1]))
            {
                int i = 2;
                while (i < n && !IsVowel(s[i]))
                {
                    i++;
                }
                return i < n ? i + 1 : n;
            }

            // First two letters both vowels -> after the next consonant.
            if (IsVowel(s[0]))
            {
                int i = 2;
                while (i < n && IsVowel(s[i]))
                {
                    i++;
                }
                return i < n ? i + 1 : n;
            }

            // Consonant-vowel -> after the third letter.
            return Math.Min(3, n);
        }

        private bool InRv(int suffixLen) => _s.Length - suffixLen >= _rv;
        private bool InR1(int suffixLen) => _s.Length - suffixLen >= _r1;
        private bool InR2(int suffixLen) => _s.Length - suffixLen >= _r2;
        private bool Ends(string suffix) => _s.EndsWith(suffix, StringComparison.Ordinal);
        private void Delete(int len) => _s = _s.Substring(0, _s.Length - len);
        private void Replace(int suffixLen, string repl) => _s = _s.Substring(0, _s.Length - suffixLen) + repl;

        /// <summary>Returns the longest element of <paramref name="candidates"/> that ends the word, or null.</summary>
        private string? LongestSuffix(string[] candidates)
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

        private static string RemoveAcuteAccents(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                sb.Append(c switch
                {
                    'á' => 'a',
                    'é' => 'e',
                    'í' => 'i',
                    'ó' => 'o',
                    'ú' => 'u',
                    _ => c,
                });
            }
            return sb.ToString();
        }

        // Attached object pronouns, longest first.
        private static readonly string[] Pronouns =
        [
            "selas", "selos", "sela", "selo", "las", "les", "los", "nos", "me", "se", "la", "le", "lo",
        ];

        // Verb forms the pronoun may attach to. Group (a) also loses its accent.
        private static readonly string[] AccentedGerundInfinitive = ["iéndo", "ándo", "ár", "ér", "ír"];
        private static readonly string[] PlainGerundInfinitive = ["ando", "iendo", "ar", "er", "ir"];

        private static string Deaccent(string suffix) => suffix switch
        {
            "iéndo" => "iendo",
            "ándo" => "ando",
            "ár" => "ar",
            "ér" => "er",
            "ír" => "ir",
            _ => suffix,
        };

        private void Step0()
        {
            string? pronoun = LongestSuffix(Pronouns);
            if (pronoun is null)
            {
                return;
            }

            string stem = _s.Substring(0, _s.Length - pronoun.Length);

            // (a) accented gerund/infinitive: delete the pronoun, then drop the accent.
            foreach (string suf in AccentedGerundInfinitive)
            {
                if (stem.EndsWith(suf, StringComparison.Ordinal) && InRv(suf.Length + pronoun.Length))
                {
                    _s = stem.Substring(0, stem.Length - suf.Length) + Deaccent(suf);
                    return;
                }
            }

            // (b) plain gerund/infinitive: delete the pronoun only.
            foreach (string suf in PlainGerundInfinitive)
            {
                if (stem.EndsWith(suf, StringComparison.Ordinal) && InRv(suf.Length + pronoun.Length))
                {
                    _s = stem;
                    return;
                }
            }

            // (c) "yendo" in RV, itself preceded by u (that u need not be in RV).
            if (stem.EndsWith("uyendo", StringComparison.Ordinal) && InRv(5 + pronoun.Length))
            {
                _s = stem;
            }
        }

        // Step 1 suffixes, grouped by the action they trigger. Matching is
        // longest-first ACROSS all groups, so the arrays are searched together.
        private static readonly string[] Step1Delete =
        [
            "amientos", "imientos", "amiento", "imiento", "anzas", "ibles", "istas", "ismos",
            "ables", "icos", "icas", "osos", "osas", "anza", "ible", "ista", "ismo", "able",
            "ico", "ica", "oso", "osa",
        ];
        private static readonly string[] Step1DeleteThenIc = ["aciones", "adoras", "adores", "ancias", "ación", "adora", "antes", "ancia", "ador", "ante"];
        private static readonly string[] Step1Logia = ["logías", "logía"];
        private static readonly string[] Step1Ucion = ["uciones", "ución"];
        private static readonly string[] Step1Encia = ["encias", "encia"];
        private static readonly string[] Step1Idad = ["idades", "idad"];
        private static readonly string[] Step1Iva = ["ivas", "ivos", "iva", "ivo"];

        private void Step1()
        {
            // Pick the single longest match over every group at once.
            string? hit = null;
            string[]? group = null;
            foreach (string[] g in new[] { Step1Delete, Step1DeleteThenIc, Step1Logia, Step1Ucion, Step1Encia, Step1Idad, Step1Iva })
            {
                string? candidate = LongestSuffix(g);
                if (candidate is not null && (hit is null || candidate.Length > hit.Length))
                {
                    hit = candidate;
                    group = g;
                }
            }

            // "amente" and "mente" are handled apart: they are longest-matched against
            // each other, and only compete with the groups above on length.
            string? adverb = LongestSuffix(["amente", "mente"]);
            if (adverb is not null && (hit is null || adverb.Length > hit.Length))
            {
                StepAdverb(adverb);
                return;
            }

            if (hit is null || group is null)
            {
                return;
            }

            int n = hit.Length;
            if (ReferenceEquals(group, Step1Delete))
            {
                if (InR2(n))
                {
                    Delete(n);
                }
            }
            else if (ReferenceEquals(group, Step1DeleteThenIc))
            {
                if (InR2(n))
                {
                    Delete(n);
                    if (Ends("ic") && InR2(2))
                    {
                        Delete(2);
                    }
                }
            }
            else if (ReferenceEquals(group, Step1Logia))
            {
                if (InR2(n))
                {
                    Replace(n, "log");
                }
            }
            else if (ReferenceEquals(group, Step1Ucion))
            {
                if (InR2(n))
                {
                    Replace(n, "u");
                }
            }
            else if (ReferenceEquals(group, Step1Encia))
            {
                if (InR2(n))
                {
                    Replace(n, "ente");
                }
            }
            else if (ReferenceEquals(group, Step1Idad))
            {
                if (InR2(n))
                {
                    Delete(n);
                    foreach (string pre in new[] { "abil", "ic", "iv" })
                    {
                        if (Ends(pre) && InR2(pre.Length))
                        {
                            Delete(pre.Length);
                            break;
                        }
                    }
                }
            }
            else if (InR2(n))
            {
                // Step1Iva
                Delete(n);
                if (Ends("at") && InR2(2))
                {
                    Delete(2);
                }
            }
        }

        private void StepAdverb(string adverb)
        {
            if (adverb == "amente")
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
                foreach (string pre in new[] { "os", "ic", "ad" })
                {
                    if (Ends(pre) && InR2(2))
                    {
                        Delete(2);
                        return;
                    }
                }
                return;
            }

            // "mente"
            if (!InR2(5))
            {
                return;
            }
            Delete(5);
            foreach (string pre in new[] { "ante", "able", "ible" })
            {
                if (Ends(pre) && InR2(pre.Length))
                {
                    Delete(pre.Length);
                    return;
                }
            }
        }

        private static readonly string[] Step2aSuffixes = ["yeron", "yendo", "yamos", "yais", "yan", "yen", "yas", "yes", "ya", "ye", "yo", "yó"];

        private void Step2a()
        {
            string? hit = LongestSuffix(Step2aSuffixes);
            if (hit is null || !InRv(hit.Length))
            {
                return;
            }
            // Delete only when preceded by u — which itself need not be in RV.
            int at = _s.Length - hit.Length;
            if (at > 0 && _s[at - 1] == 'u')
            {
                Delete(hit.Length);
            }
        }

        // Verb suffixes whose removal also drops a preceding "gu"'s u.
        private static readonly string[] Step2bGu = ["éis", "emos", "en", "es"];

        private static readonly string[] Step2bPlain =
        [
            "aríamos", "eríamos", "iríamos", "iéramos", "iésemos", "ábamos", "áramos", "ásemos",
            "aríais", "aremos", "eríais", "eremos", "iríais", "iremos", "ierais", "ieseis",
            "asteis", "isteis", "ábais", "arían", "arías", "eríais", "erían", "erías", "irían",
            "irías", "íamos", "abais", "arais", "aseis", "íais",
            "arán", "arás", "aría", "aréis", "erán", "erás", "ería", "eréis", "irán", "irás",
            "iría", "iréis", "aban", "aran", "asen", "aron", "ando", "abas", "adas", "idas",
            "aras", "ases", "íais", "ados", "idos", "amos", "imos", "iendo", "ieran", "iesen",
            "ieron", "ieras", "ieses", "ábam",
            "aba", "ada", "ida", "ara", "ase", "ían", "ado", "ido", "ías", "áis", "éis",
            "ía", "ad", "ed", "id", "an", "ió", "ar", "er", "ir", "as", "ís",
            "aste", "iste", "iera", "iese",
        ];

        private void Step2b()
        {
            string? gu = LongestSuffix(Step2bGu);
            string? plain = LongestSuffix(Step2bPlain);

            // Longest wins; on a tie the "gu" group is the more specific rule.
            if (gu is not null && (plain is null || gu.Length >= plain.Length))
            {
                if (!InRv(gu.Length))
                {
                    return;
                }
                Delete(gu.Length);
                if (Ends("gu"))
                {
                    Delete(1);
                }
                return;
            }

            if (plain is not null && InRv(plain.Length))
            {
                Delete(plain.Length);
            }
        }

        private static readonly string[] Step3Delete = ["os", "a", "o", "á", "í", "ó"];
        private static readonly string[] Step3Gu = ["e", "é"];

        private void Step3()
        {
            string? hit = LongestSuffix(Step3Delete);
            if (hit is not null && InRv(hit.Length))
            {
                Delete(hit.Length);
                return;
            }

            string? e = LongestSuffix(Step3Gu);
            if (e is null || !InRv(e.Length))
            {
                return;
            }
            Delete(e.Length);
            // Drop the u of a preceding "gu" only when that u lies in RV.
            if (Ends("gu") && InRv(1))
            {
                Delete(1);
            }
        }
    }
}
