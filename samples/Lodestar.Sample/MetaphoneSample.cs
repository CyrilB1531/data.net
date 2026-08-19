using Lodestar.Text.Phonetics;

namespace Lodestar.Sample;

/// <summary>English pronunciation rules, not just consonants.</summary>
internal static class MetaphoneSample
{
    public static void Run()
    {
        Console.WriteLine($"  Metaphone(Thompson)            = {Metaphone.Encode("Thompson")}");
    }
}
