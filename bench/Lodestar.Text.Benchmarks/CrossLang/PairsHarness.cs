using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// The matched <see cref="Stopwatch"/> loop the pair-corpus harnesses use: one
/// committed corpus of length buckets, ns per pair, auto-scaled to a minimum
/// time and taken best-of-N.
/// </summary>
/// <remarks>
/// Factored out of <c>LevenshteinCrossLang</c> for the reason <see cref="Harness"/>
/// gives for its own extraction in #58: a copy would be free to drift while still
/// reporting comparable-looking numbers. The Python side mirrors these constants.
/// </remarks>
internal static class PairsHarness
{
    public const double MinTimeSeconds = 0.5;
    public const int RepeatCount = 5;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Reads the committed pair corpus every harness in this family shares.</summary>
    public static Corpus Load()
    {
        string path = Path.Combine(BenchCorpus.RepoRoot(), "bench", "corpus", "pairs.json");
        return JsonSerializer.Deserialize<Corpus>(File.ReadAllText(path))
            ?? throw new InvalidDataException("corpus deserialized to null");
    }

    /// <summary>One distance, as a struct so the JIT can devirtualize and inline the call.</summary>
    /// <remarks>
    /// A <c>Func&lt;string, string, int&gt;</c> would read better and cost a non-inlinable
    /// indirection on every pair — under 0.01% of the 512 bucket, but 3–5% of the 8 bucket,
    /// where the whole budget is about 36 ns. A harness may not spend a measurable fraction
    /// of what it measures, so the delegate is a constrained generic instead.
    /// </remarks>
    public interface IPairMeasure
    {
        int Measure(string a, string b);
    }

    /// <summary>Times <paramref name="measure"/> over every bucket, printing each as it lands.</summary>
    public static List<BucketResult> Run<TMeasure>(Corpus corpus, TMeasure measure)
        where TMeasure : struct, IPairMeasure
    {
        List<BucketResult> results = [];
        foreach (Bucket bucket in corpus.Buckets)
        {
            double ns = TimeBucket(bucket.Pairs, measure);
            results.Add(new BucketResult { Length = bucket.Length, Pairs = bucket.Pairs.Count, NsPerPair = ns });
            Console.WriteLine($"  len={bucket.Length,4}  {ns,10:F1} ns/pair");
        }

        return results;
    }

    private static double TimeBucket<TMeasure>(IReadOnlyList<string[]> pairs, TMeasure measure)
        where TMeasure : struct, IPairMeasure
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
                        acc += measure.Measure(pairs[i][0], pairs[i][1]);
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

    /// <summary>Writes the payload where <c>bench/compare.py</c> expects to read it.</summary>
    public static void Write(string bench, string library, TextElement mode, List<BucketResult> results)
    {
        var payload = new Output
        {
            Metadata = new OutputMetadata
            {
                Side = "csharp",
                Library = library,
                Runtime = Environment.Version.ToString(),
                Os = Environment.OSVersion.ToString(),
                Mode = mode.ToString(),
                MinTimeS = MinTimeSeconds,
                Repeats = RepeatCount,
            },
            Results = results,
        };

        string outPath = Path.Combine(BenchCorpus.RepoRoot(), "bench", "results", $"csharp-{bench}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, JsonSerializer.Serialize(payload, JsonOptions) + "\n");
        Console.WriteLine($"-> {outPath}");
    }

    /// <summary>The mode a `--codepoint` flag selects, shared by every harness in this family.</summary>
    public static TextElement ModeOf(string[] args) =>
        args.Contains("--codepoint") ? TextElement.CodePoint : TextElement.Utf16Unit;

    // --- corpus / output DTOs, the shape bench/corpus/pairs.json and bench/compare.py agree on ---

    public sealed record Corpus
    {
        [JsonPropertyName("buckets")] public IReadOnlyList<Bucket> Buckets { get; init; } = [];
    }

    public sealed record Bucket
    {
        [JsonPropertyName("length")] public int Length { get; init; }
        [JsonPropertyName("pairs")] public IReadOnlyList<string[]> Pairs { get; init; } = [];
    }

    public sealed record Output
    {
        [JsonPropertyName("metadata")] public OutputMetadata Metadata { get; init; } = new();
        [JsonPropertyName("results")] public IReadOnlyList<BucketResult> Results { get; init; } = [];
    }

    public sealed record OutputMetadata
    {
        [JsonPropertyName("side")] public string Side { get; init; } = "";
        [JsonPropertyName("library")] public string Library { get; init; } = "";
        [JsonPropertyName("runtime")] public string Runtime { get; init; } = "";
        [JsonPropertyName("os")] public string Os { get; init; } = "";
        [JsonPropertyName("mode")] public string Mode { get; init; } = "";
        [JsonPropertyName("min_time_s")] public double MinTimeS { get; init; }
        [JsonPropertyName("repeats")] public int Repeats { get; init; }
    }

    public sealed record BucketResult
    {
        [JsonPropertyName("length")] public int Length { get; init; }
        [JsonPropertyName("pairs")] public int Pairs { get; init; }
        [JsonPropertyName("ns_per_pair")] public double NsPerPair { get; init; }
    }
}
