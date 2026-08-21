using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks, so
// following this rule breaks the benchmarks at run time rather than compile time.
#pragma warning disable CA1822

/// <summary>Where the Myers kernel starts beating the dynamic program it gates out.</summary>
/// <remarks>
/// The edit-distance twin of <see cref="LcsGateBenchmarks"/>, for its reason:
/// <see cref="LevenshteinBenchmarks"/>'s operands trim to a band that is an accident of
/// where the generator put the mutations, so a row can measure the DP while appearing to
/// measure the kernel. Here the band is the parameter. It sizes the win, and cannot place
/// the gate — below it both rows take the DP. <c>bench/README.md</c> has what does (#208).
/// The CJK rows price the refusal that #302 lifted (#383).
/// </remarks>
[MemoryDiagnoser]
public class MyersGateBenchmarks
{
    private string _a = string.Empty;
    private string _b = string.Empty;
    private string _wideA = string.Empty;
    private string _wideB = string.Empty;

    /// <summary>The differing middle — what the bit vector actually spans after trimming.</summary>
    [Params(4, 6, 8, 10, 12, 16, 24, 32, 48, 64, 96)]
    public int Band { get; set; }

    // Both pairs take BandedPair.GateSeed, so the CJK band differs from the Latin one
    // in its alphabet and in nothing else.
    [GlobalSetup]
    public void Setup()
    {
        (_a, _b) = BandedPair.Build(Band);
        (_wideA, _wideB) = BandedPair.Build(Band, alphabet: Alphabets.Cjk);
    }

    /// <summary>The generic overload, which stays on the dynamic program by design.</summary>
    [Benchmark(Baseline = true)]
    public int Dp() => Levenshtein.Distance<char>(_a.AsSpan(), _b.AsSpan());

    /// <summary>The character overload, which takes the kernel once the band clears the gate.</summary>
    /// <remarks>
    /// TextElement is passed explicitly. With two arguments C# resolves to the generic
    /// overload, applicable in normal form, and this row would silently measure the DP twice.
    /// </remarks>
    [Benchmark]
    public int Kernel() => Levenshtein.Distance(_a.AsSpan(), _b.AsSpan(), TextElement.Utf16Unit);

    /// <summary>The same band drawn from CJK, on the overload that stays on the dynamic program.</summary>
    /// <remarks>
    /// The fallback these two rows exist to price. It is here rather than assumed equal to
    /// <see cref="Dp"/> because that equality is the claim: the dynamic program compares
    /// characters and should not care which alphabet they come from (#383).
    /// </remarks>
    [Benchmark]
    public int Dp_Cjk() => Levenshtein.Distance<char>(_wideA.AsSpan(), _wideB.AsSpan());

    /// <summary>The kernel on a band it refused before #302, which now reaches it through the side table.</summary>
    /// <remarks>
    /// Read against <see cref="Dp_Cjk"/> this is what the wide path buys, measured rather than
    /// argued from decision 0004's standing figure; read against <see cref="Kernel"/> it is what
    /// the side table costs a band that has to use it.
    /// </remarks>
    [Benchmark]
    public int Kernel_Cjk() => Levenshtein.Distance(_wideA.AsSpan(), _wideB.AsSpan(), TextElement.Utf16Unit);
}
