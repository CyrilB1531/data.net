using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>The six shipped lists, by size.</summary>
internal static class StopWordsSample
{
    public static void Run()
    {
        Console.WriteLine($"  stop-word lists  : en={StopWords.English.Count} fr={StopWords.French.Count} de={StopWords.German.Count} "
            + $"es={StopWords.Spanish.Count} it={StopWords.Italian.Count} pt={StopWords.Portuguese.Count}");
    }
}
