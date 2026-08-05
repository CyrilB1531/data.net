using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataNet.Embeddings.Persistence;
using DataNet.Text.Vectorization;

namespace DataNet.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for the #58 persistence work, mirroring
/// <c>bench/python/bench_persistence.py</c> exactly: same corpus files, same
/// millisecond-per-operation metric, same auto-scaling best-of-N methodology.
/// A matched Stopwatch loop rather than BenchmarkDotNet, so both languages are
/// measured the same way.
/// </summary>
/// <remarks>
/// Loaders are called through their path-based overloads on purpose. The Python
/// counterpart is <c>Tokenizer.from_file(path)</c>, which reads the file itself;
/// timing a C# in-memory parse against it would flatter C# for free.
/// </remarks>
public static class PersistenceCrossLang
{
    private const double MinTimeSeconds = 0.5;
    private const int RepeatCount = 5;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Run()
    {
        string root = BenchCorpus.RepoRoot();
        string outPath = Path.Combine(root, "bench", "results", "csharp-persistence.json");

        string[] documents = JsonSerializer.Deserialize<string[]>(
            File.ReadAllBytes(BenchCorpus.Path("documents.json")))!;
        TfidfVectorizer fitted = new TfidfVectorizer().Fit(documents);
        byte[] artifact;
        using (var stream = new MemoryStream())
        {
            fitted.Save(stream);
            artifact = stream.ToArray();
        }

        string vocabTxt = BenchCorpus.Path("vocab_30k.txt");
        string wordPieceJson = BenchCorpus.Path("tokenizer_30k_wordpiece.json");
        string unigramJson = BenchCorpus.Path("tokenizer_30k_unigram.json");
        string spiece = BenchCorpus.Path("spiece_30k.model");

        Console.WriteLine("C# persistence cross-lang bench");
        var results = new List<OperationResult>
        {
            Measure("vocab_txt", () => VocabTxtLoader.Load(vocabTxt)),
            Measure("tokenizer_json_wordpiece", () => TokenizerJsonLoader.LoadWordPiece(wordPieceJson)),
            Measure("tokenizer_json_unigram", () => TokenizerJsonLoader.LoadUnigram(unigramJson)),
            Measure("spiece_model", () => SentencePieceModelLoader.Load(spiece)),
            Measure("tfidf_save", () =>
            {
                using var stream = new MemoryStream(artifact.Length);
                fitted.Save(stream);
                return stream.Length;
            }),
            Measure("tfidf_load", () =>
            {
                using var stream = new MemoryStream(artifact);
                return TfidfVectorizer.Load(stream);
            }),
        };

        var payload = new Output
        {
            Metadata = new OutputMetadata
            {
                Side = "csharp",
                Library = "DataNet",
                Runtime = Environment.Version.ToString(),
                Os = Environment.OSVersion.ToString(),
                MinTimeS = MinTimeSeconds,
                Repeats = RepeatCount,
            },
            Results = results,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, JsonSerializer.Serialize(payload, JsonOptions) + "\n");
        Console.WriteLine($"-> {outPath}");
    }

    /// <summary>
    /// Times one operation, recording both elapsed time and processor time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Wall time alone flatters this runtime. .NET's background collector does its
    /// work on other threads, so an operation that allocates heavily finishes in
    /// less elapsed time than it costs: measured here, every DataNet operation burns
    /// 1.3–1.45 processor-seconds per elapsed second, while the Python side is
    /// strictly single-threaded at 1.00. Comparing only elapsed time would report a
    /// parity that disappears the moment two models load at once.
    /// </para>
    /// <para>
    /// Both figures come from the same run — the one with the lowest elapsed time —
    /// so the pair is internally consistent rather than two separate best-ofs.
    /// </para>
    /// </remarks>
    private static OperationResult Measure(string operation, Func<object> action)
    {
        Process process = Process.GetCurrentProcess();
        double bestWall = double.PositiveInfinity;
        double cpuOfBest = double.NaN;

        for (int r = 0; r < RepeatCount; r++)
        {
            long iters = 1;
            while (true)
            {
                TimeSpan cpuBefore = process.TotalProcessorTime;
                long start = Stopwatch.GetTimestamp();
                for (long it = 0; it < iters; it++)
                {
                    GC.KeepAlive(action());
                }
                double seconds = (double)(Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency;
                process.Refresh();
                double cpuSeconds = (process.TotalProcessorTime - cpuBefore).TotalSeconds;

                if (seconds >= MinTimeSeconds)
                {
                    double wallMs = seconds / iters * 1e3;
                    if (wallMs < bestWall)
                    {
                        bestWall = wallMs;
                        cpuOfBest = cpuSeconds / iters * 1e3;
                    }
                    break;
                }
                iters *= 2;
            }
        }

        Console.WriteLine($"  {operation,-28} {bestWall,10:F3} ms/op  cpu {cpuOfBest,8:F3} ms/op  ({cpuOfBest / bestWall:F2}x cores)");
        return new OperationResult { Operation = operation, MsPerOp = bestWall, CpuMsPerOp = cpuOfBest };
    }

    private sealed record Output
    {
        [JsonPropertyName("metadata")] public OutputMetadata Metadata { get; init; } = new();
        [JsonPropertyName("results")] public IReadOnlyList<OperationResult> Results { get; init; } = [];
    }

    private sealed record OutputMetadata
    {
        [JsonPropertyName("side")] public string Side { get; init; } = "";
        [JsonPropertyName("library")] public string Library { get; init; } = "";
        [JsonPropertyName("runtime")] public string Runtime { get; init; } = "";
        [JsonPropertyName("os")] public string Os { get; init; } = "";
        [JsonPropertyName("min_time_s")] public double MinTimeS { get; init; }
        [JsonPropertyName("repeats")] public int Repeats { get; init; }
    }

    private sealed record OperationResult
    {
        [JsonPropertyName("operation")] public string Operation { get; init; } = "";
        [JsonPropertyName("ms_per_op")] public double MsPerOp { get; init; }
        [JsonPropertyName("cpu_ms_per_op")] public double CpuMsPerOp { get; init; }
    }
}
