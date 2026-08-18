using Lodestar.Text;
using Lodestar.Text.Distances;
using Lodestar.Text.Phonetics;
using Lodestar.Text.Similarity;
using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>
/// Lot 1 — everything Lodestar.Text exposes that works on a pair of strings:
/// edit distances, set similarity, phonetic encoders, stemmers.
/// </summary>
/// <remarks>
/// One call per public type, deliberately. The point of this file is that each
/// type is <em>reachable</em> from outside its assembly once packaged; teaching
/// the API is what docs/guides is for. See PackagingGate.
/// </remarks>
internal static class Lot1Distances
{
    public static void Run()
    {
        Console.WriteLine("lot 1 — distances, similarity, phonetics, stemming");

        // Edit distances. Every one of them takes an optional TextElement, which
        // selects UTF-16 units or code points as the unit of comparison.

        // Each pair named once: the normalized readings repeat them.
        const string Kitten = "kitten";
        const string Sitting = "sitting";
        const string Martha = "martha";
        const string Marhta = "marhta";
        const string Ca = "ca";
        const string Abc = "abc";
        const string Karolin = "karolin";
        const string Kathrin = "kathrin";
        const string Pineapple = "pineapple";
        const string Pen = "pen";

        Console.WriteLine($"  Levenshtein(kitten, sitting)        = {Levenshtein.Distance(Kitten, Sitting)}");
        Console.WriteLine($"  Levenshtein normalized              = {Inv.F4(Levenshtein.NormalizedSimilarity(Kitten, Sitting))}");
        Console.WriteLine($"  Levenshtein(a<emoji>, a) code points = {Levenshtein.Distance("a\U0001F600", "a", TextElement.CodePoint)}");
        Console.WriteLine($"  DamerauLevenshtein(ca, abc)         = {DamerauLevenshtein.Distance(Ca, Abc)}");
        Console.WriteLine($"  Osa(ca, abc)                        = {Osa.Distance(Ca, Abc)}");
        Console.WriteLine($"  Hamming(karolin, kathrin)           = {Hamming.Distance(Karolin, Kathrin)}");
        Console.WriteLine($"  Indel(kitten, sitting)              = {Indel.Distance(Kitten, Sitting)}");
        Console.WriteLine($"  Lcs subsequence(AGCAT, GAC)         = {Lcs.SubsequenceLength("AGCAT", "GAC")}");
        Console.WriteLine($"  Lcs substring(AGCAT, GAC)           = {Lcs.SubstringLength("AGCAT", "GAC")}");
        Console.WriteLine($"  Jaro(martha, marhta)                = {Inv.F4(Jaro.Similarity(Martha, Marhta))}");
        Console.WriteLine($"  JaroWinkler(martha, marhta)         = {Inv.F4(JaroWinkler.Similarity(Martha, Marhta))}");
        Console.WriteLine($"  RatcliffObershelp(pineapple, pen)   = {Inv.F4(RatcliffObershelp.Similarity(Pineapple, Pen))}");

        // Normalized readings, in [0, 1], and the similarity scorers as distances.
        Console.WriteLine($"  Levenshtein normalized              = {Inv.F4(Levenshtein.NormalizedDistance(Kitten, Sitting))} distance");
        Console.WriteLine($"  DamerauLevenshtein normalized       = {Inv.F4(DamerauLevenshtein.NormalizedDistance(Ca, Abc))} distance, "
            + $"{Inv.F4(DamerauLevenshtein.NormalizedSimilarity(Ca, Abc))} similarity");
        Console.WriteLine($"  Osa normalized                      = {Inv.F4(Osa.NormalizedDistance(Ca, Abc))} distance, "
            + $"{Inv.F4(Osa.NormalizedSimilarity(Ca, Abc))} similarity");
        Console.WriteLine($"  Indel normalized                    = {Inv.F4(Indel.NormalizedDistance(Kitten, Sitting))} distance, "
            + $"{Inv.F4(Indel.NormalizedSimilarity(Kitten, Sitting))} similarity");
        Console.WriteLine($"  Hamming normalized similarity       = {Inv.F4(Hamming.NormalizedSimilarity(Karolin, Kathrin))}");
        Console.WriteLine($"  Jaro / JaroWinkler as distances     = {Inv.F4(Jaro.Distance(Martha, Marhta))} / "
            + $"{Inv.F4(JaroWinkler.Distance(Martha, Marhta))}");
        Console.WriteLine($"  RatcliffObershelp as a distance     = {Inv.F4(RatcliffObershelp.Distance(Pineapple, Pen))}");

        // Set similarity over q-grams.
        const string A = "night";
        const string B = "nacht";
        Console.WriteLine($"  Jaccard(night, nacht)               = {Inv.F4(Jaccard.Similarity(A, B))}");
        Console.WriteLine($"  SorensenDice(night, nacht)          = {Inv.F4(SorensenDice.Similarity(A, B))}");
        Console.WriteLine($"  Overlap(night, nacht)               = {Inv.F4(Overlap.Similarity(A, B))}");
        Console.WriteLine($"  Tversky(night, nacht)               = {Inv.F4(Tversky.Similarity(A, B))}");
        Console.WriteLine($"  Cosine(night, nacht)                = {Inv.F4(Cosine.Similarity(A, B))}");

        // Phonetic encoders.
        Console.WriteLine($"  Soundex(Robert)                     = {Soundex.Encode("Robert")}");
        Console.WriteLine($"  Metaphone(Thompson)                 = {Metaphone.Encode("Thompson")}");
        Console.WriteLine($"  Nysiis(Knight)                      = {Nysiis.Encode("Knight")}");

        // Stemmers: the Porter original, then the six Snowball languages.
        Console.WriteLine($"  Porter(running)                     = {PorterStemmer.Stem("running")}");
        Console.WriteLine($"  en running                          = {EnglishSnowballStemmer.Stem("running")}");
        Console.WriteLine($"  fr continuellement                  = {FrenchSnowballStemmer.Stem("continuellement")}");
        Console.WriteLine($"  es hermosos                         = {SpanishSnowballStemmer.Stem("hermosos")}");
        Console.WriteLine($"  pt esperanca                        = {PortugueseSnowballStemmer.Stem("esperança")}");
        Console.WriteLine($"  it rapidamente                      = {ItalianSnowballStemmer.Stem("rapidamente")}");
        Console.WriteLine($"  de freundliche                      = {GermanSnowballStemmer.Stem("freundliche")}");
        Console.WriteLine();
    }
}
