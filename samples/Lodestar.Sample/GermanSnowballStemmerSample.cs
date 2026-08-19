using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for de.</summary>
internal static class GermanSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  de freundliche                      = {GermanSnowballStemmer.Stem("freundliche")}");
    }
}
