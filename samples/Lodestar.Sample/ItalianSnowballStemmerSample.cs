using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The Snowball stemmer for it.</summary>
internal static class ItalianSnowballStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  it rapidamente                      = {ItalianSnowballStemmer.Stem("rapidamente")}");
    }
}
