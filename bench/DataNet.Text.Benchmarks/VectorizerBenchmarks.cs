using BenchmarkDotNet.Attributes;
using DataNet.Text.Vectorization;

namespace DataNet.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>Fit+transform throughput for the sparse vectorizers over a synthetic corpus.</summary>
[MemoryDiagnoser]
public class VectorizerBenchmarks
{
    private string[] _docs = [];

    /// <summary>Number of documents in the corpus.</summary>
    [Params(200, 1000)]
    public int Documents { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Deterministic pseudo-text: a small vocabulary recombined.
        var rng = new Random(1234);
        string[] words = "le chat chien mange dort court vite grand petit noir blanc bois maison arbre ciel eau feu terre vent pierre".Split(' ');
        _docs = new string[Documents];
        for (int d = 0; d < Documents; d++)
        {
            int len = 8 + rng.Next(24);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < len; i++)
            {
                sb.Append(words[rng.Next(words.Length)]).Append(' ');
            }
            _docs[d] = sb.ToString();
        }
    }

    [Benchmark(Baseline = true)]
    public CsrMatrix Count() => new CountVectorizer().FitTransform(_docs);

    [Benchmark]
    public CsrMatrix Tfidf() => new TfidfVectorizer().FitTransform(_docs);

    [Benchmark]
    public CsrMatrix CountBigrams() =>
        new CountVectorizer(new CountVectorizerOptions { NgramRange = (1, 2) }).FitTransform(_docs);

    [Benchmark]
    public CsrMatrix Hashing() =>
        new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 1 << 18 }).Transform(_docs);
}
