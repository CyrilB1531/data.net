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
public class MyersGateBenchmarks : GateOperands
{
    /// <summary>The differing middle — what the bit vector actually spans after trimming.</summary>
    [Params(4, 6, 8, 10, 12, 16, 24, 32, 48, 64, 96)]
    public int Band { get; set; }

    // Its first two bands sit below the gate of 8, which the LCS twin's do not: both rows
    // take the DP there, on either alphabet, and the ratio of 1 is the dispatch saying so.
    [GlobalSetup]
    public void Setup() => Build(Band);

    /// <summary>The generic overload, which stays on the dynamic program by design.</summary>
    [Benchmark(Baseline = true)]
    public int Dp() => Levenshtein.Distance<char>(LatinA.AsSpan(), LatinB.AsSpan());

    /// <summary>The character overload, which takes the kernel once the band clears the gate.</summary>
    /// <remarks>
    /// TextElement is passed explicitly. With two arguments C# resolves to the generic
    /// overload, applicable in normal form, and this row would silently measure the DP twice.
    /// </remarks>
    [Benchmark]
    public int Kernel() => Levenshtein.Distance(LatinA.AsSpan(), LatinB.AsSpan(), TextElement.Utf16Unit);

    /// <summary>The CJK band on the overload that stays on the dynamic program.</summary>
    /// <remarks>
    /// The fallback this table exists to price, and the control on its own baseline: Myers'
    /// recurrence is the heavier of the two, so if an alphabet were going to reach the dynamic
    /// program's cost anywhere, it would show here first.
    /// </remarks>
    [Benchmark]
    public int Dp_Cjk() => Levenshtein.Distance<char>(CjkA.AsSpan(), CjkB.AsSpan());

    /// <summary>The CJK band on the kernel, which refused it before #302.</summary>
    /// <remarks>
    /// Read against <see cref="Dp_Cjk"/> at each band, this is where edit distance crosses over
    /// on wide input — later than the Latin rows cross, the side table's probe having raised the
    /// kernel's floor while leaving the dynamic program's cost untouched.
    /// </remarks>
    [Benchmark]
    public int Kernel_Cjk() => Levenshtein.Distance(CjkA.AsSpan(), CjkB.AsSpan(), TextElement.Utf16Unit);
}
