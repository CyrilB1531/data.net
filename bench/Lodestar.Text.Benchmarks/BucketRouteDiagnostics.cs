using BenchmarkDotNet.Attributes;
using Lodestar.Text.Benchmarks.CrossLang;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks, so
// following this rule breaks the benchmarks at run time rather than compile time.
#pragma warning disable CA1822

/// <summary>Which of the two routes the length-32 bucket's cost actually sits on.</summary>
/// <remarks>
/// #208 called the 32 bucket a single-word Myers gap and it was not one: the pairs carry
/// three scattered edits, so trimming leaves a pattern of 16 on the median, and at the
/// gate of 16 that shipped then, 47% of them fell under it and took the DP — 70% of the
/// bucket's whole cost on 47% of its pairs. Splitting the real corpus on the dispatch's
/// own criterion is what showed that; <c>Gate</c> below has to track the shipped constant
/// or the two halves stop being the two routes.
/// </remarks>
[MemoryDiagnoser]
public class BucketRouteDiagnostics
{
    private const int Gate = 8; // Levenshtein.MyersMinPatternLength, which is private.

    private string[][] _dp = [];
    private string[][] _myers = [];

    [GlobalSetup]
    public void Setup()
    {
        List<string[]> pairs = PairsHarness.Load().Buckets.First(b => b.Length == 32).Pairs.ToList();
        _dp = [.. pairs.Where(p => Pattern(p[0], p[1]) < Gate)];
        _myers = [.. pairs.Where(p => Pattern(p[0], p[1]) >= Gate)];
        // console-print: the denominator of both rows below; [GlobalSetup], so untimed.
        Console.WriteLine($"DP group: {_dp.Length} pairs, Myers group: {_myers.Length} pairs");
    }

    [Benchmark]
    public int DpGroup() => Sum(_dp);

    [Benchmark]
    public int MyersGroup() => Sum(_myers);

    private static int Sum(string[][] pairs)
    {
        int acc = 0;
        foreach (string[] pair in pairs)
        {
            acc += Levenshtein.Distance(pair[0], pair[1]);
        }

        return acc;
    }

    /// <summary>The shorter operand after trimming — what the dispatch hands Myers.</summary>
    private static int Pattern(string a, string b)
    {
        int i = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i])
        {
            i++;
        }

        int j = 0;
        while (j < a.Length - i && j < b.Length - i && a[a.Length - 1 - j] == b[b.Length - 1 - j])
        {
            j++;
        }

        return Math.Min(a.Length - i - j, b.Length - i - j);
    }
}
