using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

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
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz ";
    private const int Shared = 24;

    private string _a = string.Empty;
    private string _b = string.Empty;

    /// <summary>Length of the differing middle — what the kernel would actually span.</summary>
    [Params(8, 12, 14, 16, 18, 20, 24, 32, 48, 64, 96)]
    public int Band { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(20260818);
        string prefix = Random(rng, Shared);
        string suffix = Random(rng, Shared);

        // Ends forced apart: drawn independently they share a first or last character
        // once in 27, and trimming then eats into the band and drops it below the gate.
        _a = prefix + WithEnds(rng, Band, 'a', 'b') + suffix;
        _b = prefix + WithEnds(rng, Band, 'c', 'd') + suffix;
    }

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

    /// <summary>A random middle whose first and last characters are imposed.</summary>
    private static string WithEnds(Random rng, int length, char first, char last)
    {
        char[] text = Random(rng, length).ToCharArray();
        text[0] = first;
        text[^1] = last;
        return new string(text);
    }

    private static string Random(Random rng, int length)
    {
        char[] text = new char[length];
        for (int i = 0; i < length; i++)
        {
            text[i] = Alphabet[rng.Next(Alphabet.Length)];
        }

        return new string(text);
    }
}
