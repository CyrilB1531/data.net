using BenchmarkDotNet.Attributes;
using DataNet.Metrics;

namespace DataNet.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// Per-metric cost at three sizes and two class counts. The matrix is built
/// inside each benchmark rather than in the setup, because that is what a caller
/// pays for a single scalar call — the amortised path is ClassificationReport.
/// </summary>
[MemoryDiagnoser]
public class MetricsBenchmarks
{
    private int[] _yTrue = [];
    private int[] _yPred = [];
    private double[] _weight = [];

    [Params(1_000, 100_000, 1_000_000)]
    public int Samples { get; set; }

    [Params(2, 10)]
    public int Classes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(20260806);
        _yTrue = new int[Samples];
        _yPred = new int[Samples];
        _weight = new double[Samples];
        for (int i = 0; i < Samples; i++)
        {
            _yTrue[i] = rng.Next(Classes);
            _yPred[i] = rng.NextDouble() < 0.7 ? _yTrue[i] : rng.Next(Classes);
            _weight[i] = (rng.NextDouble() * 2.9) + 0.1;
        }
    }

    [Benchmark]
    public ConfusionMatrix Matrix() => ConfusionMatrix.Compute(_yTrue, _yPred);

    [Benchmark]
    public ConfusionMatrix MatrixWeighted() =>
        ConfusionMatrix.Compute(_yTrue, _yPred, default, _weight);

    [Benchmark]
    public double AccuracyScore() => Accuracy.Score(_yTrue, _yPred);

    [Benchmark]
    public double F1Macro() => F1.Score(_yTrue, _yPred, Averaging.Macro);

    [Benchmark]
    public string Report() => ClassificationReport.Compute(_yTrue, _yPred).ToText();
}
