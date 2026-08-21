using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks, so
// following this rule breaks the benchmarks at run time rather than compile time.
#pragma warning disable CA1822

/// <summary>What the blocked kernels' equality table costs, on patterns the corpus never reaches.</summary>
/// <remarks>
/// The table is <c>(256 + slots) × ⌈m/64⌉</c> words, and #302 sized <c>slots</c> from the
/// pattern's length rather than from its characters above Latin-1, so an ASCII pattern paid for
/// an alphabet it never used. <c>Allocated</c> reads zero here and always will —
/// <c>ArrayPool.Rent</c> amortises the buffer across invocations — so what the rows show is the
/// <c>Clear()</c> of whatever was rented, which is the cost the sizing imposes (#413).
/// </remarks>
[MemoryDiagnoser]
public class BlockedTableBenchmarks
{
    private readonly Dictionary<int, (string A, string B)> _latin = [];
    private readonly Dictionary<int, (string A, string B)> _cjk = [];

    [GlobalSetup]
    public void Setup()
    {
        foreach (int length in (int[])[1000, 10000, 65536])
        {
            _latin[length] = Build(length, Alphabets.Latin);
            if (length <= 10000)
            {
                _cjk[length] = Build(length, Alphabets.Cjk);
            }
        }
    }

    /// <summary>The case the regression was about: no character above Latin-1, and no side rows.</summary>
    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(65536)]
    public int Latin(int length)
    {
        (string a, string b) = _latin[length];
        return Levenshtein.Distance(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit);
    }

    /// <summary>A pattern that genuinely needs the side table, so the rows are not waste.</summary>
    /// <remarks>
    /// 65 536 is deliberately absent: sized from occurrences rather than distinct symbols, a
    /// pattern that long allocates about a gigabyte, which is a finding rather than a row.
    /// </remarks>
    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    public int Cjk(int length)
    {
        (string a, string b) = _cjk[length];
        return Levenshtein.Distance(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit);
    }

    private static (string, string) Build(int length, string alphabet)
    {
        var rng = new Random(length);
        char[] a = new char[length];
        for (int i = 0; i < length; i++)
        {
            a[i] = alphabet[rng.Next(alphabet.Length)];
        }

        char[] b = (char[])a.Clone();
        // Both ends moved, so Affixes.Trim strips nothing and the pattern the kernel spans is
        // the whole length — which is what the table is sized from.
        b[0] = a[0] == alphabet[0] ? alphabet[1] : alphabet[0];
        b[^1] = a[^1] == alphabet[2] ? alphabet[3] : alphabet[2];
        return (new string(a), new string(b));
    }
}
