using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// The matched <see cref="Stopwatch"/> loop every cross-language harness in this
/// project uses: same minimum time, same repeat count, same best-of-N selection,
/// wall and processor time recorded together from the same run. Factored out of
/// <c>PersistenceCrossLang</c> (issue #58) so that a second harness — this one,
/// for issue #61 — calls it rather than writing a second timing loop that could
/// drift from the first.
/// </summary>
internal static class Harness
{
    public const double MinTimeSeconds = 0.5;
    public const int RepeatCount = 5;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Times one operation, recording both elapsed time and processor time. Wall
    /// time alone flatters this runtime: .NET's background collector works on
    /// other threads, so a heavily-allocating operation finishes in less elapsed
    /// time than it costs. Both figures come from the same run — the one with the
    /// lowest elapsed time — so the pair is internally consistent rather than two
    /// separate best-ofs.
    /// </summary>
    /// <param name="operation">The row name, matched against the Python side.</param>
    /// <param name="action">The operation, its result kept alive against elision.</param>
    /// <param name="artifactBytes">What the operation moved, <c>0</c> when it moves no artifact.</param>
    public static OperationResult Measure(string operation, Func<object> action, long artifactBytes = 0)
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

        string size = artifactBytes > 0 ? $"  {artifactBytes,12:N0} B" : "";
        Console.WriteLine($"  {operation,-28} {bestWall,10:F3} ms/op  cpu {cpuOfBest,8:F3} ms/op  ({cpuOfBest / bestWall:F2}x cores){size}");
        return new OperationResult
        {
            Operation = operation,
            MsPerOp = bestWall,
            CpuMsPerOp = cpuOfBest,
            ArtifactBytes = artifactBytes,
        };
    }

    /// <summary>Writes the payload as indented JSON, creating the directory if needed.</summary>
    public static void Write(string outPath, Output payload)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, JsonSerializer.Serialize(payload, JsonOptions) + "\n");
        Console.WriteLine($"-> {outPath}");
    }

    public sealed record Output
    {
        [JsonPropertyName("metadata")] public OutputMetadata Metadata { get; init; } = new();
        [JsonPropertyName("results")] public IReadOnlyList<OperationResult> Results { get; init; } = [];
    }

    public sealed record OutputMetadata
    {
        [JsonPropertyName("side")] public string Side { get; init; } = "";
        [JsonPropertyName("library")] public string Library { get; init; } = "";
        [JsonPropertyName("runtime")] public string Runtime { get; init; } = "";
        [JsonPropertyName("os")] public string Os { get; init; } = "";
        [JsonPropertyName("min_time_s")] public double MinTimeS { get; init; }
        [JsonPropertyName("repeats")] public int Repeats { get; init; }

        /// <summary>
        /// What a partial run left out, or <see langword="null"/> when the run was
        /// complete. A filtered run writes the same file to the same path holding
        /// fewer rows, and <c>bench/compare.py</c> silently skips the operations it
        /// cannot pair — so without this field a three-row file reads as a green
        /// merge gate over every operation and every size. The comparison refuses
        /// to run when it is present.
        /// </summary>
        [JsonPropertyName("filtered")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Filtered { get; init; }
    }

    public sealed record OperationResult
    {
        [JsonPropertyName("operation")] public string Operation { get; init; } = "";
        [JsonPropertyName("ms_per_op")] public double MsPerOp { get; init; }
        [JsonPropertyName("cpu_ms_per_op")] public double CpuMsPerOp { get; init; }

        /// <summary>
        /// The artifact the operation wrote or read, in bytes, absent when it moves none.
        /// #100's size figures were taken by hand and could not be re-checked; a row that
        /// carries its own size makes the bytes a published number the way the time is.
        /// </summary>
        [JsonPropertyName("artifact_bytes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long ArtifactBytes { get; init; }
    }
}
