using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for nl.</summary>
internal static class DutchSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  nl mogelijkheden                    = {DutchSnowballStemmer.Stem("mogelijkheden")}");
    }
}
