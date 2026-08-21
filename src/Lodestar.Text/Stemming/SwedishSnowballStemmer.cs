namespace Lodestar.Text.Stemming;

// CA1308 (normalize to uppercase): Snowball is *defined* on lowercase input — the
// published algorithm, the reference implementations and the oracle corpora all
// lowercase first, so ToUpperInvariant would return a wrong stem.
#pragma warning disable CA1308

/// <summary>
/// The Swedish Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("swedish")</c>. An original
/// implementation of the published Snowball algorithm: R1 only, floored at three letters,
/// and no R2 or RV region at all — see <c>docs/equivalence.md</c>'s stemming row. Shares
/// <see cref="ScandinavianSnowballWorker"/> with the Danish and Norwegian algorithms.
/// Thread-safe.
/// </remarks>
public static class SwedishSnowballStemmer
{
    /// <summary>Returns the Swedish Snowball stem of <paramref name="word"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="word"/> is null.</exception>
    public static string Stem(string word)
    {
        Guard.NotNull(word);
        string s = word.ToLowerInvariant();
        return s.Length < 3 ? s : new Worker(s).Run();
    }

    private sealed class Worker : ScandinavianSnowballWorker
    {
        private static readonly Func<char, bool> Vowels = c =>
            c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y' or 'ä' or 'å' or 'ö';

        /// <summary>The letters a deletable final <c>s</c> may follow.</summary>
        /// <remarks>Every consonant of the alphabet plus <c>o</c> and <c>y</c>, as the list names.</remarks>
        private static readonly Func<char, bool> SEndings = c =>
            c is 'b' or 'c' or 'd' or 'f' or 'g' or 'h' or 'j' or 'k' or 'l' or 'm'
              or 'n' or 'o' or 'p' or 'r' or 't' or 'v' or 'y';

        private static readonly string[] Step1Suffixes =
        [
            "heterna", "hetens", "arnas", "ernas", "ornas", "andes", "arens", "andet",
            "heten", "heter", "anden", "arne", "arna", "erna", "orna", "aren", "ande",
            "aste", "ades", "erns", "het", "ade", "are", "ast", "ens", "ern", "and",
            "ad", "ar", "er", "or", "as", "es", "at", "en", "a", "e", "s",
        ];

        private static readonly string[] Step2Endings = ["dd", "gd", "nn", "dt", "gt", "kt", "tt"];

        private static readonly string[] S3All = ["fullt", "löst", "lig", "els", "ig"];

        public Worker(string s) : base(s, Vowels, SEndings)
        {
        }

        public string Run()
        {
            Step1(Step1Suffixes);
            DropLastOf(Step2Endings);
            Step3();
            return S;
        }

        private void Step3()
        {
            string? found = LongestSuffixInR1(S3All);
            switch (found)
            {
                case null: return;
                case "fullt": Replace(5, "full"); return;
                case "löst": Replace(4, "lös"); return;
                default: Delete(found.Length); return;
            }
        }
    }
}
