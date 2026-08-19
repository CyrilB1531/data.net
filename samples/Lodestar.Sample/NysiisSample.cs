using Lodestar.Text.Phonetics;

namespace Lodestar.Sample;

/// <summary>New York's variant, kinder to non-English names.</summary>
internal static class NysiisSample
{
    public static void Run()
    {
        Console.WriteLine($"  Nysiis(Knight)                 = {Nysiis.Encode("Knight")}");
    }
}
