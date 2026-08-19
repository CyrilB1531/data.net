using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for es.</summary>
internal static class SpanishSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  es hermosos                         = {SpanishSnowballStemmer.Stem("hermosos")}");
    }
}
