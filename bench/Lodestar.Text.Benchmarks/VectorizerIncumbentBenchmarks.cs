using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Lodestar.Text.Vectorization;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Lodestar.Text.Benchmarks;

// CA1822: see LevenshteinIncumbentBenchmarks.
#pragma warning disable CA1822

/// <summary>One document, as ML.NET's `IDataView` wants it.</summary>
public sealed class TextRow
{
    /// <summary>The document.</summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>The dense feature vector `FeaturizeText` produces.</summary>
public sealed class FeaturizedRow
{
    // CA1819 (properties should not return arrays): ML.NET binds its output column by
    // reflecting over a settable float[] property, so the rule's advice is not available.
#pragma warning disable CA1819
    /// <summary>Word and character n-gram features, L2-normalized.</summary>
    [VectorType]
    public float[] Features { get; set; } = [];
#pragma warning restore CA1819
}

/// <summary>
/// <see cref="TfidfVectorizer"/> beside ML.NET's `FeaturizeText`, the incumbent issue
/// #438 names for this package.
/// </summary>
/// <remarks>
/// **Not a like-for-like comparison, and the table must not be read as one.**
/// bench/README.md section 15 has the measurement that says so and what each side
/// actually produces.
/// </remarks>
[MemoryDiagnoser]
public class VectorizerIncumbentBenchmarks
{
    private string[] _documents = [];
    private MLContext _ml = null!;

    /// <summary>How many documents of the corpus this row featurizes.</summary>
    [Params(200, 1000)]
    public int Documents { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string[] all = JsonSerializer.Deserialize<string[]>(
            File.ReadAllBytes(BenchCorpus.Path("documents.json")))!;
        _documents = [.. all.Take(Documents)];
        _ml = new MLContext(seed: 0);
    }

    [Benchmark(Baseline = true)]
    public int Lodestar() => new TfidfVectorizer().FitTransform(_documents).NonZeroCount;

    // Materialized rather than left as a lazy IDataView: ML.NET's transforms are lazy,
    // so a Fit alone would time the plan and not the work the sparse side does eagerly.
    [Benchmark]
    public int MlNet()
    {
        var data = _ml.Data.LoadFromEnumerable(_documents.Select(d => new TextRow { Text = d }));
        var model = _ml.Transforms.Text.FeaturizeText("Features", nameof(TextRow.Text)).Fit(data);
        int total = 0;
        foreach (var row in _ml.Data.CreateEnumerable<FeaturizedRow>(model.Transform(data), reuseRowObject: true))
        {
            total += row.Features.Length;
        }
        return total;
    }
}
