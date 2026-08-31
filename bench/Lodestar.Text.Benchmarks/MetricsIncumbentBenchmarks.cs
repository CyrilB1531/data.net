using BenchmarkDotNet.Attributes;
using Lodestar.Metrics;
using Microsoft.ML;

// Aliased rather than imported: Microsoft.ML.Data also has a ConfusionMatrix, and
// importing the namespace would make every mention of ours ambiguous.
using MlBinaryMetrics = Microsoft.ML.Data.BinaryClassificationMetrics;

namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

// CA1822: see LevenshteinIncumbentBenchmarks.
#pragma warning disable CA1822

/// <summary>One scored prediction, in the shape ML.NET's binary evaluator reads.</summary>
public sealed class ScoredRow
{
    /// <summary>The true class.</summary>
    public bool Label { get; set; }

    /// <summary>The raw margin. ML.NET thresholds this at zero — it is not a probability.</summary>
    public float Score { get; set; }

    /// <summary>The predicted class.</summary>
    public bool PredictedLabel { get; set; }
}

/// <summary>What a row of the incumbent metrics table asks for.</summary>
public enum MetricsRequest
{
    /// <summary>Every number ML.NET's binary evaluator returns.</summary>
    Bundle,

    /// <summary>Accuracy alone — which ML.NET has no call for.</summary>
    AccuracyAlone,
}

/// <summary>
/// <see cref="Lodestar.Metrics"/> against ML.NET's binary evaluator, the incumbent
/// issue #438 names for this package.
/// </summary>
/// <remarks>
/// Both sides were checked to agree first — identical accuracy and confusion matrix,
/// AUC to 5e-9. bench/README.md section 15 has that check and what the two rows mean.
/// </remarks>
[MemoryDiagnoser]
public class MetricsIncumbentBenchmarks
{
    private int[] _yTrue = [];
    private int[] _yPred = [];
    private double[] _yScore = [];
    private ScoredRow[] _rows = [];
    private MLContext _ml = null!;

    /// <summary>How many scored predictions the row summarises.</summary>
    [Params(100_000, 1_000_000)]
    public int Samples { get; set; }

    /// <summary>What the row asks each library for.</summary>
    [Params(MetricsRequest.Bundle, MetricsRequest.AccuracyAlone)]
    public MetricsRequest Request { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(20260806);
        _yTrue = new int[Samples];
        _yPred = new int[Samples];
        _yScore = new double[Samples];
        _rows = new ScoredRow[Samples];
        for (int i = 0; i < Samples; i++)
        {
            _yTrue[i] = rng.Next(2);
            double score = Math.Clamp((rng.NextDouble() * 0.6) + (_yTrue[i] == 1 ? 0.35 : 0.05), 0, 1);
            _yScore[i] = score;
            _yPred[i] = score >= 0.5 ? 1 : 0;
            _rows[i] = new ScoredRow
            {
                Label = _yTrue[i] == 1,
                Score = (float)(score - 0.5),
                PredictedLabel = _yPred[i] == 1,
            };
        }

        _ml = new MLContext(seed: 0);
    }

    [Benchmark(Baseline = true)]
    public double Lodestar()
    {
        if (Request == MetricsRequest.AccuracyAlone)
        {
            return Accuracy.Score(_yTrue, _yPred);
        }

        ConfusionMatrix cm = ConfusionMatrix.Compute(_yTrue, _yPred);
        return Accuracy.Score(cm)
            + RocAuc.Score(_yTrue, _yScore)
            + AveragePrecision.Score(_yTrue, _yScore)
            + F1.Score(cm)
            + Precision.Score(cm)
            + Recall.Score(cm);
    }

    // The same call in both rows, deliberately: ML.NET evaluates the whole bundle or
    // nothing, so "accuracy alone" costs a caller the bundle. That is the measurement.
    [Benchmark]
    public double MlNet()
    {
        var data = _ml.Data.LoadFromEnumerable(_rows);
        MlBinaryMetrics metrics = _ml.BinaryClassification.EvaluateNonCalibrated(data);
        return Request == MetricsRequest.AccuracyAlone
            ? metrics.Accuracy
            : metrics.Accuracy
                + metrics.AreaUnderRocCurve
                + metrics.AreaUnderPrecisionRecallCurve
                + metrics.F1Score
                + metrics.PositivePrecision
                + metrics.PositiveRecall;
    }
}
