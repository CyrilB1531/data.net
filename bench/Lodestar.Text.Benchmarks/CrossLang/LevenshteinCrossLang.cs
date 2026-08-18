using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for <see cref="Levenshtein"/>, mirroring
/// <c>bench/python/bench_levenshtein.py</c> exactly: same committed corpus, same
/// ns-per-pair metric, same auto-scaling and best-of-N methodology. The timing
/// loop and the output shape live in <see cref="PairsHarness"/>, so this file is
/// the choice of distance and nothing else.
/// </summary>
public static class LevenshteinCrossLang
{
    private readonly struct Edits(TextElement mode) : PairsHarness.IPairMeasure
    {
        public int Measure(string a, string b) => Levenshtein.Distance(a, b, mode);
    }

    public static void Run(string[] args)
    {
        TextElement mode = PairsHarness.ModeOf(args);
        Console.WriteLine($"C# Levenshtein cross-lang bench (mode: {mode})");

        List<PairsHarness.BucketResult> results =
            PairsHarness.Run(PairsHarness.Load(), new Edits(mode));

        PairsHarness.Write("levenshtein", "Lodestar.Text", mode, results);
    }
}
