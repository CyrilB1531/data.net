using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>Every knob the counting stage exposes, set away from its default.</summary>
internal static class CountVectorizerOptionsSample
{
    public static void Run()
    {
        CountVectorizerOptions options = TextCorpus.Counting();

        Console.WriteLine($"  CountVectorizerOptions: analyzer={options.Analyzer}, ngrams={options.NgramRange}, "
            + $"lowercase={options.Lowercase}, stripAccents={options.StripAccents}, binary={options.Binary}");
        Console.WriteLine($"  document frequency    : minDf={options.MinDf}, maxDf={Inv.F1(options.MaxDf)}, "
            + $"pattern={options.TokenPattern}");
    }
}
