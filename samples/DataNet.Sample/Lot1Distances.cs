using DataNet.Text;
using DataNet.Text.Distances;
using DataNet.Text.Phonetics;
using DataNet.Text.Similarity;
using DataNet.Text.Stemming;

namespace DataNet.Sample;

/// <summary>
/// Lot 1 — everything DataNet.Text exposes that works on a pair of strings:
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
        Console.WriteLine($"  Levenshtein(kitten, sitting)        = {Levenshtein.Distance("kitten", "sitting")}");
        Console.WriteLine($"  Levenshtein normalized              = {Levenshtein.NormalizedSimilarity("kitten", "sitting"):F4}");
        Console.WriteLine($"  Levenshtein(a<emoji>, a) code points = {Levenshtein.Distance("a\U0001F600", "a", TextElement.CodePoint)}");
        Console.WriteLine($"  DamerauLevenshtein(ca, abc)         = {DamerauLevenshtein.Distance("ca", "abc")}");
        Console.WriteLine($"  Osa(ca, abc)                        = {Osa.Distance("ca", "abc")}");
        Console.WriteLine($"  Hamming(karolin, kathrin)           = {Hamming.Distance("karolin", "kathrin")}");
        Console.WriteLine($"  Indel(kitten, sitting)              = {Indel.Distance("kitten", "sitting")}");
        Console.WriteLine($"  Lcs subsequence(AGCAT, GAC)         = {Lcs.SubsequenceLength("AGCAT", "GAC")}");
        Console.WriteLine($"  Lcs substring(AGCAT, GAC)           = {Lcs.SubstringLength("AGCAT", "GAC")}");
        Console.WriteLine($"  Jaro(martha, marhta)                = {Jaro.Similarity("martha", "marhta"):F4}");
        Console.WriteLine($"  JaroWinkler(martha, marhta)         = {JaroWinkler.Similarity("martha", "marhta"):F4}");
        Console.WriteLine($"  RatcliffObershelp(pineapple, pen)   = {RatcliffObershelp.Similarity("pineapple", "pen"):F4}");

        // Set similarity over q-grams.
        const string A = "night";
        const string B = "nacht";
        Console.WriteLine($"  Jaccard(night, nacht)               = {Jaccard.Similarity(A, B):F4}");
        Console.WriteLine($"  SorensenDice(night, nacht)          = {SorensenDice.Similarity(A, B):F4}");
        Console.WriteLine($"  Overlap(night, nacht)               = {Overlap.Similarity(A, B):F4}");
        Console.WriteLine($"  Tversky(night, nacht)               = {Tversky.Similarity(A, B):F4}");
        Console.WriteLine($"  Cosine(night, nacht)                = {Cosine.Similarity(A, B):F4}");

        // Phonetic encoders.
        Console.WriteLine($"  Soundex(Robert)                     = {Soundex.Encode("Robert")}");
        Console.WriteLine($"  Metaphone(Thompson)                 = {Metaphone.Encode("Thompson")}");
        // MUTATION (#77, reverted in the next commit): Nysiis is now referenced by
        // nothing, so the packaging gate must fail.
        // Console.WriteLine($"  Nysiis(Knight)                      = {Nysiis.Encode("Knight")}");

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
