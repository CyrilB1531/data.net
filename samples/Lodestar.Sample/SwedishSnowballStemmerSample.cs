using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for sv.</summary>
internal static class SwedishSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  sv verksamheterna                   = {SwedishSnowballStemmer.Stem("verksamheterna")}");
    }
}
