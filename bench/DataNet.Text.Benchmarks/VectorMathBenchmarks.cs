using BenchmarkDotNet.Attributes;
using DataNet.Embeddings.Search;

namespace DataNet.Text.Benchmarks;

/// <summary>
/// Micro-benchmarks for <see cref="VectorMath"/> — the sharpest measurable
/// difference between the two builds of the libraries.
/// </summary>
/// <remarks>
/// <see cref="VectorMath.Dot"/> takes a <see cref="System.Numerics.Vector{T}"/>
/// SIMD path under <c>#if NET5_0_OR_GREATER</c> and falls back to a scalar loop on
/// netstandard2.0, because the span-based <c>Vector&lt;T&gt;</c> constructor is
/// net-only. Running this class from both benchmark projects quantifies what the
/// broad-reach target costs.
/// </remarks>
[MemoryDiagnoser]
public class VectorMathBenchmarks
{
    private float[] _a = [];
    private float[] _b = [];

    /// <summary>Vector dimension (MiniLM 384, BERT-base 768, BERT-large 1024).</summary>
    [Params(384, 768, 1024)]
    public int Dim { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _a = new float[Dim];
        _b = new float[Dim];
        for (int i = 0; i < Dim; i++)
        {
            _a[i] = (float)(rng.NextDouble() * 2 - 1);
            _b[i] = (float)(rng.NextDouble() * 2 - 1);
        }
    }

    [Benchmark(Baseline = true)]
    public float Dot() => VectorMath.Dot(_a, _b);

    [Benchmark]
    public float L2Norm() => VectorMath.L2Norm(_a);
}
