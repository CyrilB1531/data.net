using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks, so
// following this rule breaks the benchmarks at run time rather than compile time.
#pragma warning disable CA1822

/// <summary>Where the bit-parallel LCS kernel starts beating the dynamic program it gates out.</summary>
/// <remarks>
/// <see cref="IndelBenchmarks"/> cannot: its operands trim down to a band that is an
/// accident of where the generator put the mutations, so its length-16 row measured the
/// DP while appearing to measure the kernel. Here the band is the parameter — same
/// prefix and suffix, independent middles — so after <c>Affixes.Trim</c> the pattern is
/// exactly <see cref="Band"/> long, and with the DP as baseline in the same process the
/// crossing is read off the ratio (#273).
/// </remarks>
[MemoryDiagnoser]
public class LcsGateBenchmarks
{
    private string _a = string.Empty;
    private string _b = string.Empty;

    /// <summary>Length of the differing middle — what the kernel would actually span.</summary>
    [Params(8, 12, 14, 16, 18, 20, 24, 32, 48, 64, 96)]
    public int Band { get; set; }

    // The seed is BandedPair.GateSeed, so the numbers this published in #273 still
    // reproduce and the Myers twin measures the same characters.
    [GlobalSetup]
    public void Setup() => (_a, _b) = BandedPair.Build(Band);

    /// <summary>The generic overload, which stays on the dynamic program by design.</summary>
    [Benchmark(Baseline = true)]
    public int Dp() => Lcs.SubsequenceLength<char>(_a.AsSpan(), _b.AsSpan());

    /// <summary>The character overload, which takes the kernel once the band clears the gate.</summary>
    /// <remarks>
    /// TextElement is passed explicitly. With two arguments C# resolves to the generic
    /// overload, applicable in normal form, and this row would silently measure the DP twice.
    /// </remarks>
    [Benchmark]
    public int Kernel() => Lcs.SubsequenceLength(_a.AsSpan(), _b.AsSpan(), TextElement.Utf16Unit);
}
