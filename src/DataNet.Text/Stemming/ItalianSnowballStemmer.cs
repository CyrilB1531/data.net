using System.Text;

namespace DataNet.Text.Stemming;

// SonarLint S3776: cognitive complexity: faithful port of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express.
#pragma warning disable S3776, S3267

/// <summary>
/// The Italian Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("italian")</c>. An
/// original implementation of the published Snowball algorithm, sharing the
/// RV/R1/R2 machinery of the other Romance stemmers via
/// <see cref="RomanceSnowballWorker"/>. Input is lowercased. Thread-safe.
/// </para>
/// <para>
/// Two pieces of preprocessing shape everything after them. Acute accents are
/// folded to grave, so <c>perché</c> and <c>perchè</c> stem alike. And <c>u</c>
/// after <c>q</c>, plus <c>u</c>/<c>i</c> between vowels, are upper-cased so the
/// regions treat them as consonants; they are lower-cased again at the end.
/// </para>
/// </remarks>
public static class ItalianSnowballStemmer
{
    /// <summary>Returns the Italian Snowball stem of <paramref name="word"/>.</summary>
    public static string Stem(string word)
    {
        Guard.NotNull(word);
        // Compose accents (NFC) so 'à' etc. are single code points, as the rules expect.
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
            c is 'a' or 'e' or 'i' or 'o' or 'u' or 'à' or 'è' or 'ì' or 'ò' or 'ù';

        public Worker(string s) : base(MarkNonVowels(FoldAcuteAccents(s)), Vowels)
        {
        }

        public string Run()
        {
            Step0();

            string before = S;
            Step1();
            if (S == before)
            {
                Step2();
            }

            Step3a();
            Step3b();
            return Unmark(S);
        }

        /// <summary>Acute accents fold to grave, so perché and perchè stem alike.</summary>
        private static string FoldAcuteAccents(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                sb.Append(c switch
                {
                    'á' => 'à',
                    'é' => 'è',
                    'í' => 'ì',
                    'ó' => 'ò',
                    'ú' => 'ù',
                    _ => c,
                });
            }
            return sb.ToString();
        }

        /// <summary>Upper-cases u after q and u/i between vowels, so regions read them as consonants.</summary>
        private static string MarkNonVowels(string s)
        {
            char[] a = s.ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                char c = a[i];
                if (c != 'u' && c != 'i')
                {
                    continue;
                }
                if (c == 'u' && i > 0 && s[i - 1] == 'q')
                {
                    a[i] = 'U';
                    continue;
                }
                bool between = i > 0 && i + 1 < a.Length && Vowels(s[i - 1]) && Vowels(s[i + 1]);
                if (between)
                {
                    a[i] = c == 'u' ? 'U' : 'I';
                }
            }
            return new string(a);
        }

        private static string Unmark(string s) => s.Replace('I', 'i').Replace('U', 'u');

        // Attached pronouns, and the verb forms they may follow.
        private static readonly string[] Pronouns =
        [
            "gliela", "gliele", "glieli", "glielo", "gliene",
            "sene", "mela", "mele", "meli", "melo", "mene",
            "tela", "tele", "teli", "telo", "tene",
            "cela", "cele", "celi", "celo", "cene",
            "vela", "vele", "veli", "velo", "vene",
            "gli", "ci", "la", "le", "li", "lo", "mi", "ne", "si", "ti", "vi",
        ];
        private static readonly string[] Gerunds = ["ando", "endo"];
        private static readonly string[] Infinitives = ["ar", "er", "ir"];

        private void Step0()
        {
            string? pronoun = LongestSuffix(Pronouns);
            if (pronoun is null)
            {
                return;
            }

            string stem = S.Substring(0, S.Length - pronoun.Length);

            foreach (string suf in Gerunds)
            {
                if (stem.EndsWith(suf, StringComparison.Ordinal) && InRv(suf.Length + pronoun.Length))
                {
                    S = stem;
                    return;
                }
            }

            // After an infinitive the pronoun goes and an "e" is restored:
            // mandarci -> mandare.
            foreach (string suf in Infinitives)
            {
                if (stem.EndsWith(suf, StringComparison.Ordinal) && InRv(suf.Length + pronoun.Length))
                {
                    S = stem + "e";
                    return;
                }
            }
        }

        private static readonly string[] S1Delete =
        [
            "atrici", "atrice", "abile", "abili", "ibile", "ibili", "mente",
            "anza", "anze", "iche", "ichi", "ismo", "ismi", "ista", "iste", "isti",
            "istà", "istè", "istì", "ante", "anti", "ico", "ici", "ica", "ice",
            "oso", "osi", "osa", "ose",
        ];
        private static readonly string[] S1DeleteThenIc = ["azione", "azioni", "atore", "atori"];
        private static readonly string[] S1Logia = ["logia", "logie"];
        private static readonly string[] S1Uzione = ["uzione", "uzioni", "usione", "usioni"];
        private static readonly string[] S1Enza = ["enza", "enze"];
        private static readonly string[] S1Amento = ["amento", "amenti", "imento", "imenti"];
        private static readonly string[] S1Ita = ["ità"];
        private static readonly string[] S1Ivo = ["ivo", "ivi", "iva", "ive"];

        private void Step1() => ApplyLongestRule(
        [
            new(S1Delete, DeleteIfInR2),
            new(S1DeleteThenIc, n => DeleteInR2ThenStrip(n, ["ic"])),
            new(S1Logia, n => ReplaceIfInR2(n, "log")),
            new(S1Uzione, n => ReplaceIfInR2(n, "u")),
            // nltk replaces "enza"/"enze" with "te", not "ente" as the published
            // description reads: esistenza -> esistte, trimmed by step 3a to esistt.
            // nltk is the reference the corpus is frozen from; see decision 0008.
            new(S1Enza, n => ReplaceIfInR2(n, "te")),
            // The only step-1 group gated on RV rather than R2.
            new(S1Amento, DeleteIfInRv),
            new(S1Ita, n => DeleteInR2ThenStrip(n, ["abil", "ic", "iv"])),
            new(S1Ivo, DeleteIvo),
            new(Adverbs, StepAmente),
        ]);

        /// <summary>"ivo"/"iva" delete in R2, then "at", then "ic" — two levels, unlike the others.</summary>
        private void DeleteIvo(int n)
        {
            if (!InR2(n))
            {
                return;
            }
            Delete(n);
            if (Ends("at") && InR2(2))
            {
                Delete(2);
                if (Ends("ic") && InR2(2))
                {
                    Delete(2);
                }
            }
        }

        // Italian has no bare "-mente" rule in this position; "mente" sits in S1Delete.
        private static readonly string[] Adverbs = ["amente"];

        private void StepAmente(int n)
        {
            _ = n;
            StripAmente(["abil", "os", "ic"]);
        }

        private static readonly string[] Step2Suffixes =
        [
            "irebbero", "erebbero", "assero", "assimo", "eranno", "erebbe", "eremmo", "ereste",
            "eresti", "essero", "iranno", "irebbe", "iremmo", "ireste", "iresti", "iscano",
            "iscono", "issero", "arono", "avamo", "avano", "avate", "eremo", "erete", "erono",
            "evamo", "evano", "evate", "iremo", "irete", "irono", "ivamo", "ivano", "ivate",
            "ammo", "ando", "asse", "assi", "emmo", "enda", "ende", "endi", "endo", "erai",
            "erei", "Yamo", "iamo", "immo", "irai", "irei", "isca", "isce", "isci", "isco",
            "ano", "are", "ata", "ate", "ati", "ato", "ava", "avi", "avo", "erà", "ere",
            "erò", "ete", "eva", "evi", "evo", "irà", "ire", "irò", "ita", "ite", "iti",
            "ito", "iva", "ivi", "ivo", "ono", "uta", "ute", "uti", "uto",
            "ar", "er", "ir",
        ];

        private void Step2()
        {
            string? hit = LongestSuffixInRv(Step2Suffixes);
            if (hit is not null)
            {
                Delete(hit.Length);
            }
        }

        private static readonly string[] Step3aSuffixes = ["a", "e", "i", "o", "à", "è", "ì", "ò"];

        private void Step3a()
        {
            string? hit = LongestSuffixInRv(Step3aSuffixes);
            if (hit is null)
            {
                return;
            }
            Delete(hit.Length);
            if (Ends("i") && InRv(1))
            {
                Delete(1);
            }
        }

        private void Step3b()
        {
            if (Ends("ch") && InRv(2))
            {
                Replace(2, "c");
            }
            else if (Ends("gh") && InRv(2))
            {
                Replace(2, "g");
            }
        }
    }
}
