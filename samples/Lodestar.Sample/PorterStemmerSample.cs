using Lodestar.Text.Stemming;

namespace Lodestar.Sample;

/// <summary>The 1980 original, still the baseline every other is compared to.</summary>
internal static class PorterStemmerSample
{
    public static void Run()
    {
        Console.WriteLine($"  Porter(running)                     = {PorterStemmer.Stem("running")}");
    }
}
