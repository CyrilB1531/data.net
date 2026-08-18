using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for <see cref="Indel"/>, mirroring
/// <c>bench/python/bench_indel.py</c> over the same committed corpus as the
/// Levenshtein pair.
/// </summary>
/// <remarks>
/// <see cref="Indel"/> is <c>len(a) + len(b) - 2·LCS</c>, so this measures
/// <c>Lcs.SubsequenceLength</c> — which <c>fuzz.ratio</c> and every
/// <c>process.extract</c> also run (#273).
/// </remarks>
public static class IndelCrossLang
{
    private readonly struct Edits(TextElement mode) : PairsHarness.IPairMeasure
    {
        public int Measure(string a, string b) => Indel.Distance(a, b, mode);
    }

    public static void Run(string[] args)
    {
        TextElement mode = PairsHarness.ModeOf(args);
        Console.WriteLine($"C# Indel cross-lang bench (mode: {mode})");

        List<PairsHarness.BucketResult> results =
            PairsHarness.Run(PairsHarness.Load(), new Edits(mode));

        PairsHarness.Write("indel", "Lodestar.Text", mode, results);
    }
}
