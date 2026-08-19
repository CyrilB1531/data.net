using Lodestar.Text.Phonetics;

namespace Lodestar.Sample;

/// <summary>The oldest of them, four characters wide.</summary>
internal static class SoundexSample
{
    public static void Run()
    {
        Console.WriteLine($"  Soundex(Robert)                = {Soundex.Encode("Robert")}");
    }
}
