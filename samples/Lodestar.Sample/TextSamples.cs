namespace Lodestar.Sample;

/// <summary>Every Lodestar.Text sample, in the order a reader meets the package.</summary>
/// <remarks>
/// The grouping the lot numbering used to carry, kept here where it is the only thing
/// it was good for: which types belong together. Finding one type's example is the file
/// named after it, per decision 0041.
/// </remarks>
internal static class TextSamples
{
    public static void Run()
    {
        Console.WriteLine("Lodestar.Text");
        Console.WriteLine("  distances, one pair of strings at a time");
        LevenshteinSample.Run();
        DamerauLevenshteinSample.Run();
        OsaSample.Run();
        HammingSample.Run();
        IndelSample.Run();
        LcsSample.Run();
        JaroSample.Run();
        JaroWinklerSample.Run();
        RatcliffObershelpSample.Run();
        Console.WriteLine();
        Console.WriteLine("  set similarity over q-grams");
        JaccardSample.Run();
        SorensenDiceSample.Run();
        OverlapSample.Run();
        TverskySample.Run();
        CosineSample.Run();
        Console.WriteLine();
        Console.WriteLine("  phonetic encoders");
        SoundexSample.Run();
        MetaphoneSample.Run();
        NysiisSample.Run();
        MatchRatingApproachSample.Run();
        Console.WriteLine();
        Console.WriteLine("  stemmers — the Porter original, then the six Snowball languages");
        PorterStemmerSample.Run();
        EnglishSnowballStemmerSample.Run();
        FrenchSnowballStemmerSample.Run();
        SpanishSnowballStemmerSample.Run();
        PortugueseSnowballStemmerSample.Run();
        ItalianSnowballStemmerSample.Run();
        GermanSnowballStemmerSample.Run();
        Console.WriteLine();
        Console.WriteLine("  vectorization, and the artifacts it persists");
        CountVectorizerOptionsSample.Run();
        CountVectorizerSample.Run();
        CsrMatrixSample.Run();
        TfidfOptionsSample.Run();
        TfidfTransformerSample.Run();
        TfidfVectorizerOptionsSample.Run();
        TfidfVectorizerSample.Run();
        HashingVectorizerOptionsSample.Run();
        HashingVectorizerSample.Run();
        StopWordsSample.Run();
        ArtifactLoadOptionsSample.Run();
        Console.WriteLine();
    }
}
