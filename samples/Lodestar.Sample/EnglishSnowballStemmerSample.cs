using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for en.</summary>
internal static class EnglishSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  en running                          = {EnglishSnowballStemmer.Stem("running")}");
    }
}
