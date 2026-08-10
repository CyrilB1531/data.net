using System.Text;

namespace DataNet.Text.Stemming;

// SonarLint S3776: cognitive complexity: faithful port of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express.
// CA1845 (use span-based string.Concat): that overload does not exist on
// netstandard2.0. The Substring form is what makes this file compile there.
#pragma warning disable CA1845
// CA1845 (use span-based string.Concat): that overload does not exist on
// netstandard2.0. The Substring form is what makes this file compile there.
#pragma warning disable CA1845
// CA1307 (specify StringComparison): the overload it asks for —
// string.IndexOf(char, StringComparison) / string.Replace(string, string?,
// StringComparison) — does not exist on netstandard2.0, which this assembly
// targets. Both calls are ordinal on every runtime that has them, so the
// suggestion would change nothing but the compilation. Same reason as the CA1845
// pragmas in DataNet.Embeddings/Tokenization/.
// CA1308 (normalize to uppercase): Snowball, Porter and WordPiece are *defined*
// on lowercase input — the published algorithms, the reference implementations
// and the oracle corpora this suite is checked against all lowercase first.
// ToUpperInvariant would return different stems, which is a wrong answer rather
// than a differently-cased one.
#pragma warning disable S3776, S3267, CA1307, CA1308

/// <summary>
/// The Portuguese Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("portuguese")</c>. An
/// original implementation of the published Snowball algorithm, using the RV/R1/R2
/// regions and the standard step ordering. Input is lowercased. Thread-safe.
/// </para>
/// <para>
/// Two quirks distinguish it from the Spanish algorithm. Nasal <c>ã</c> and
/// <c>õ</c> are expanded to <c>a~</c> and <c>o~</c> for the duration of the run so
/// the tilde is not treated as a vowel — which is why <c>geração</c> stems to
/// <c>geraçã</c> and not <c>geraç</c>. And acute accents are *kept*: unlike
/// Spanish, there is no final accent-stripping step, so <c>país</c> stems to
/// itself.
/// </para>
/// </remarks>
public static class PortugueseSnowballStemmer
{
    /// <summary>Returns the Portuguese Snowball stem of <paramref name="word"/>.</summary>
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
            c is 'a' or 'e' or 'i' or 'o' or 'u' or 'á' or 'é' or 'í' or 'ó' or 'ú' or 'â' or 'ê' or 'ô';


        // Nasals are expanded before the base computes regions: the tilde must act
        // as a consonant, or the vowel before it would close a region early.
        public Worker(string s) : base(s.Replace("ã", "a~").Replace("õ", "o~"), Vowels)
        {
        }

        public string Run()
        {
            string original = S;

            Step1();
            bool altered = S != original;

            if (!altered)
            {
                string beforeStep2 = S;
                Step2();
                altered = S != beforeStep2;
            }

            if (altered)
            {
                // Step 3: a final i left behind by step 1 or 2, when it follows a c.
                if (Ends("i") && InRv(1) && S.Length >= 2 && S[S.Length - 2] == 'c')
                {
                    Delete(1);
                }
            }
            else
            {
                Step4();
            }

            Step5();

            // Restore the nasals.
            return S.Replace("a~", "ã").Replace("o~", "õ");
        }



        // Step 1 groups. Note the nasal expansion: "ação" is spelled "aça~o" here.
        private static readonly string[] S1Delete =
        [
            "amentos", "imentos", "amento", "imento", "adoras", "ismos", "istas",
            "anças", "ança", "ezas", "icos", "icas", "osos", "osas", "ismo", "ista", "eza", "ico",
            "ica", "oso", "osa", "ável", "ível",
        ];
        private static readonly string[] S1DeleteThenIc = ["aça~o", "aço~es", "adores", "adora", "ador", "antes", "ância", "ante"];
        private static readonly string[] S1Logia = ["logias", "logia"];
        private static readonly string[] S1Ucao = ["uço~es", "uça~o"];
        private static readonly string[] S1Encia = ["ências", "ência"];
        private static readonly string[] S1Idade = ["idades", "idade"];
        private static readonly string[] S1Iva = ["ivas", "ivos", "iva", "ivo"];
        private static readonly string[] S1Ira = ["iras", "ira"];

        private void Step1() => ApplyLongestRule(
        [
            new(S1Delete, DeleteIfInR2),
            new(S1DeleteThenIc, n => DeleteInR2ThenStrip(n, ["ic"])),
            new(S1Logia, n => ReplaceIfInR2(n, "log")),
            new(S1Ucao, n => ReplaceIfInR2(n, "u")),
            new(S1Encia, n => ReplaceIfInR2(n, "ente")),
            new(S1Idade, n => DeleteInR2ThenStrip(n, ["abil", "ic", "iv"])),
            new(S1Iva, n => DeleteInR2ThenStrip(n, ["at"])),
            new(S1Ira, ReplaceIraAfterE),
            new(Adverbs, StepAdverb),
        ]);

        /// <summary>"ira"/"iras" become "ir", but only inside RV and only after an e.</summary>
        private void ReplaceIraAfterE(int n)
        {
            if (InRv(n) && S.Length > n && S[S.Length - n - 1] == 'e')
            {
                Replace(n, "ir");
            }
        }

        private static readonly string[] Adverbs = ["amente", "mente"];

        private void StepAdverb(int n)
        {
            if (n == 6)
            {
                StripAmente(["os", "ic", "ad"]);
            }
            else
            {
                StripMente(["ante", "avel", "ível"]);
            }
        }

        private static readonly string[] Step2Suffixes =
        [
            "aríamos", "eríamos", "iríamos", "ássemos", "êssemos", "íssemos", "aríeis", "eríeis",
            "iríeis", "ásseis", "ésseis", "ísseis", "áramos", "éramos", "íramos", "aremos",
            "eremos", "iremos", "ariam", "eriam", "iriam", "assem", "essem", "issem", "ara~o",
            "era~o", "ira~o", "arias", "erias", "irias", "ardes", "erdes", "irdes", "asses",
            "esses", "isses", "astes", "estes", "istes", "áreis", "areis", "éreis", "ereis",
            "íreis", "ireis", "áveis", "íamos", "armos", "ermos", "irmos", "aria", "eria",
            "iria", "asse", "esse", "isse", "aste", "este", "iste", "arei", "erei", "irei",
            "aram", "eram", "iram", "avam", "arem", "erem", "irem", "ando", "endo", "indo",
            "adas", "idas", "arás", "aras", "erás", "eras", "irás", "avas", "ares", "eres",
            "ires", "íeis", "ados", "idos", "ámos", "amos", "emos", "imos", "iras", "ada",
            "ida", "ará", "ara", "erá", "era", "irá", "ava", "iam", "ado", "ido", "ias",
            "ais", "eis", "ira", "ear", "ar", "er", "ir", "am", "ia", "ei", "am", "as",
            "es", "is", "eu", "iu", "ou",
        ];

        private void Step2()
        {
            // Longest-first, but a candidate rejected by RV does not end the search:
            // "amáveis" must fall through from "áveis" (outside RV) to "eis".
            string? hit = LongestSuffixInRv(Step2Suffixes);
            if (hit is not null)
            {
                Delete(hit.Length);
            }
        }

        private static readonly string[] Step4Suffixes = ["os", "a", "i", "o", "á", "í", "ó"];

        private void Step4()
        {
            string? hit = LongestSuffix(Step4Suffixes);
            if (hit is not null && InRv(hit.Length))
            {
                Delete(hit.Length);
            }
        }

        private void Step5()
        {
            string? e = LongestSuffix(["e", "é", "ê"]);
            if (e is not null && InRv(e.Length))
            {
                Delete(e.Length);
                // Both cases drop the same single character; the spec lists them
                // separately, but the action is identical.
                if ((Ends("gu") || Ends("ci")) && InRv(1))
                {
                    Delete(1);
                }
                return;
            }

            if (Ends("ç"))
            {
                Replace(1, "c");
            }
        }
    }
}
