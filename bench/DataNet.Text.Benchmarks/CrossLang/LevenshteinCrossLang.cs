using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataNet.Text.Distances;

namespace DataNet.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for <see cref="Levenshtein"/>, mirroring
/// <c>bench/python/bench_levenshtein.py</c> exactly: same committed corpus, same
/// ns-per-pair metric, same auto-scaling and best-of-N methodology. This is a
/// deliberately matched Stopwatch loop rather than BenchmarkDotNet, so the two
/// languages are measured the same way and can be compared.
/// </summary>
public static class LevenshteinCrossLang
{
    private const double MinTimeSeconds = 0.5;
    private const int RepeatCount = 5;

    // Cached: JsonSerializerOptions is expensive to construct and caches metadata.
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Run(string[] args)
    {
        // Resolve paths relative to the current working directory (repo root when
        // launched via `dotnet run --project bench/...`), walking up as a fallback.
        string root = BenchCorpus.RepoRoot();
        string corpusPath = Path.Combine(root, "bench", "corpus", "pairs.json");
        string outPath = Path.Combine(root, "bench", "results", "csharp-levenshtein.json");

        TextElement mode = args.Contains("--codepoint") ? TextElement.CodePoint : TextElement.Utf16Unit;

        Corpus corpus = JsonSerializer.Deserialize<Corpus>(File.ReadAllText(corpusPath))
            ?? throw new InvalidDataException("corpus deserialized to null");

        Console.WriteLine($"C# Levenshtein cross-lang bench (mode: {mode})");
        var results = new List<BucketResult>();
        foreach (Bucket bucket in corpus.Buckets)
        {
            double ns = TimeBucket(bucket.Pairs, mode);
            results.Add(new BucketResult { Length = bucket.Length, Pairs = bucket.Pairs.Count, NsPerPair = ns });
            Console.WriteLine($"  len={bucket.Length,4}  {ns,10:F1} ns/pair");
        }

        var payload = new Output
        {
            Metadata = new OutputMetadata
            {
                Side = "csharp",
                Library = "DataNet.Text",
                Runtime = Environment.Version.ToString(),
                Os = Environment.OSVersion.ToString(),
                Mode = mode.ToString(),
                MinTimeS = MinTimeSeconds,
                Repeats = RepeatCount,
            },
            Results = results,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, JsonSerializer.Serialize(payload, JsonOptions) + "\n");
        Console.WriteLine($"-> {outPath}");
    }

    private static double TimeBucket(IReadOnlyList<string[]> pairs, TextElement mode)
    {
        int n = pairs.Count;
        double best = double.PositiveInfinity;

        for (int r = 0; r < RepeatCount; r++)
        {
            long iters = 1;
            while (true)
            {
                long start = Stopwatch.GetTimestamp();
                for (long it = 0; it < iters; it++)
                {
                    // Volatile-ish sink to prevent dead-code elimination.
                    int acc = 0;
                    for (int i = 0; i < n; i++)
                    {
                        acc += Levenshtein.Distance(pairs[i][0], pairs[i][1], mode);
                    }
                    GC.KeepAlive(acc);
                }
                double seconds = (double)(Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency;
                if (seconds >= MinTimeSeconds)
                {
                    best = Math.Min(best, seconds / (iters * n) * 1e9);
                    break;
                }
                iters *= 2;
            }
        }

        return best;
    }

    // --- corpus / output DTOs ---

    private sealed record Corpus
    {
        [JsonPropertyName("buckets")] public IReadOnlyList<Bucket> Buckets { get; init; } = [];
    }

    private sealed record Bucket
    {
        [JsonPropertyName("length")] public int Length { get; init; }
        [JsonPropertyName("pairs")] public IReadOnlyList<string[]> Pairs { get; init; } = [];
    }

    private sealed record Output
    {
        [JsonPropertyName("metadata")] public OutputMetadata Metadata { get; init; } = new();
        [JsonPropertyName("results")] public IReadOnlyList<BucketResult> Results { get; init; } = [];
    }

    private sealed record OutputMetadata
    {
        [JsonPropertyName("side")] public string Side { get; init; } = "";
        [JsonPropertyName("library")] public string Library { get; init; } = "";
        [JsonPropertyName("runtime")] public string Runtime { get; init; } = "";
        [JsonPropertyName("os")] public string Os { get; init; } = "";
        [JsonPropertyName("mode")] public string Mode { get; init; } = "";
        [JsonPropertyName("min_time_s")] public double MinTimeS { get; init; }
        [JsonPropertyName("repeats")] public int Repeats { get; init; }
    }

    private sealed record BucketResult
    {
        [JsonPropertyName("length")] public int Length { get; init; }
        [JsonPropertyName("pairs")] public int Pairs { get; init; }
        [JsonPropertyName("ns_per_pair")] public double NsPerPair { get; init; }
    }
}
