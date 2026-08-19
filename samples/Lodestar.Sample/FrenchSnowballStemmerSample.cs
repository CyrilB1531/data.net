using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for fr.</summary>
internal static class FrenchSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  fr continuellement                  = {FrenchSnowballStemmer.Stem("continuellement")}");
    }
}
