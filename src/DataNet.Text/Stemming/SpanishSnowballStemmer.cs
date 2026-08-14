using System.Text;

namespace DataNet.Text.Stemming;

// SonarLint S3776: cognitive complexity: faithful port of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express.
// CA1845 (use span-based string.Concat): that overload does not exist on
// netstandard2.0. The Substring form is what makes this file compile there.
#pragma warning disable CA1845
// CA1308 (normalize to uppercase): Snowball and Porter are *defined* on
// lowercase input — the published algorithms, the reference implementations and
// the oracle corpora this suite is checked against all lowercase first.
// ToUpperInvariant would return different stems, which is a wrong answer rather
// than a differently-cased one.
#pragma warning disable S3776, S3267, CA1308

/// <summary>
/// The Spanish Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("spanish")</c>. An
/// original implementation of the published Snowball algorithm, using RV/R1/R2 and
/// the standard step order — see <c>docs/equivalence.md</c>'s stemming row for the
/// attached-pronoun step and the accent-stripping order. Input is lowercased,
/// thread-safe.
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

    private sealed class Worker : RomanceSnowballWorker
    {
        private static readonly Func<char, bool> Vowels = c =>
            c is 'a' or 'e' or 'i' or 'o' or 'u' or 'á' or 'é' or 'í' or 'ó' or 'ú' or 'ü';


        public Worker(string s) : base(s, Vowels)
        {
        }

        public string Run()
        {
            // Steps 0-1 always run; 2a only if 1 altered nothing, 2b only if 2a
            // didn't — Snowball semantics: altered, not merely suffix-matched.
            Step0();

            string before = S;
            Step1();
            if (S == before)
            {
                before = S;
                Step2a();
                if (S == before)
                {
                    Step2b();
                }
            }

            Step3();
            return RemoveAcuteAccents(S);
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

            string stem = S.Substring(0, S.Length - pronoun.Length);

            // (a) accented gerund/infinitive: delete the pronoun, then drop the accent.
            foreach (string suf in AccentedGerundInfinitive)
            {
                if (stem.EndsWith(suf, StringComparison.Ordinal) && InRv(suf.Length + pronoun.Length))
                {
                    S = stem.Substring(0, stem.Length - suf.Length) + Deaccent(suf);
                    return;
                }
            }

            // (b) plain gerund/infinitive: delete the pronoun only.
            foreach (string suf in PlainGerundInfinitive)
            {
                if (stem.EndsWith(suf, StringComparison.Ordinal) && InRv(suf.Length + pronoun.Length))
                {
                    S = stem;
                    return;
                }
            }

            // (c) "yendo" in RV, itself preceded by u (that u need not be in RV).
            if (stem.EndsWith("uyendo", StringComparison.Ordinal) && InRv(5 + pronoun.Length))
            {
                S = stem;
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

        private void Step1() => ApplyLongestRule(
        [
            new(Step1Delete, DeleteIfInR2),
            new(Step1DeleteThenIc, n => DeleteInR2ThenStrip(n, ["ic"])),
            new(Step1Logia, n => ReplaceIfInR2(n, "log")),
            new(Step1Ucion, n => ReplaceIfInR2(n, "u")),
            new(Step1Encia, n => ReplaceIfInR2(n, "ente")),
            new(Step1Idad, n => DeleteInR2ThenStrip(n, ["abil", "ic", "iv"])),
            new(Step1Iva, n => DeleteInR2ThenStrip(n, ["at"])),
            new(Adverbs, StepAdverb),
        ]);

        private static readonly string[] Adverbs = ["amente", "mente"];

        private void StepAdverb(int n)
        {
            if (n == 6)
            {
                StripAmente(["os", "ic", "ad"]);
            }
            else
            {
                StripMente(["ante", "able", "ible"]);
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
            int at = S.Length - hit.Length;
            if (at > 0 && S[at - 1] == 'u')
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
