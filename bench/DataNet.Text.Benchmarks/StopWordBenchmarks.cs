using BenchmarkDotNet.Attributes;
using DataNet.Text.Vectorization;

namespace DataNet.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// Fit+transform throughput with stop-word removal on, over a corpus whose
/// stop-word density matches running English prose (~40%, the point of the
/// benchmark: stop words are by definition the most frequent tokens, so the
/// filter's per-token cost lands on most of the work). <see cref="StopWordDensity"/>
/// is checked against the corpus actually built, so a quietly-drifted corpus
/// cannot print confident numbers for a path it stopped exercising.
/// </summary>
[MemoryDiagnoser]
public class StopWordBenchmarks
{
    /// <summary>Share of tokens drawn from the stop-word list.</summary>
    private const double StopWordDensity = 0.40;

    /// <summary>Content words: no stop word among them, so the density is exactly what is drawn.</summary>
    private static readonly string[] ContentWords =
        ("river forest mountain harbour village bridge market garden window candle letter journey " +
         "summer winter autumn morning evening shadow silence thunder island valley meadow orchard " +
         "hunter sailor farmer weaver painter builder teacher soldier traveller merchant")
        .Split(' ');

    private string[] _docs = [];

    /// <summary>Number of documents in the corpus.</summary>
    [Params(200, 1000)]
    public int Documents { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // \b\w\w+\b drops single-character tokens, so a one-letter stop word would
        // never reach the filter and would dilute the density it is meant to produce.
        string[] stopWords = [.. StopWords.English.Where(w => w.Length > 1).OrderBy(w => w, StringComparer.Ordinal)];

        var rng = new Random(20260806);
        _docs = new string[Documents];
        long stopTokens = 0;
        long totalTokens = 0;
        for (int d = 0; d < Documents; d++)
        {
            int len = 40 + rng.Next(120);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < len; i++)
            {
                bool stop = rng.NextDouble() < StopWordDensity;
                sb.Append(stop ? stopWords[rng.Next(stopWords.Length)] : ContentWords[rng.Next(ContentWords.Length)])
                  .Append(' ');
                stopTokens += stop ? 1 : 0;
            }
            totalTokens += len;
            _docs[d] = sb.ToString();
        }

        double density = (double)stopTokens / totalTokens;
        if (Math.Abs(density - StopWordDensity) > 0.02)
        {
            throw new InvalidOperationException(
                $"The corpus is {density:P1} stop words, not the intended {StopWordDensity:P0} — " +
                "it no longer measures what this benchmark is named for.");
        }
    }

    /// <summary>The same corpus with no filter: the cost stop-word removal is added to.</summary>
    [Benchmark(Baseline = true)]
    public CsrMatrix Count() => new CountVectorizer().FitTransform(_docs);

    /// <summary>A shipped list, handed to the vectorizer as-is.</summary>
    [Benchmark]
    public CsrMatrix CountWithStopWords() =>
        new CountVectorizer(new CountVectorizerOptions { StopWords = StopWords.English }).FitTransform(_docs);

    /// <summary>No vocabulary to learn, so analysis is a larger share of the work.</summary>
    [Benchmark]
    public CsrMatrix Hashing() =>
        new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 1 << 18 }).Transform(_docs);

    /// <summary>The pair that isolates what the filter costs per token.</summary>
    [Benchmark]
    public CsrMatrix HashingWithStopWords() =>
        new HashingVectorizer(new HashingVectorizerOptions
        {
            NumFeatures = 1 << 18,
            Count = new CountVectorizerOptions { StopWords = StopWords.English },
        }).Transform(_docs);
}
