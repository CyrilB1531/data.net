using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for pt.</summary>
internal static class PortugueseSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  pt esperança                        = {PortugueseSnowballStemmer.Stem("esperança")}");
    }
}
