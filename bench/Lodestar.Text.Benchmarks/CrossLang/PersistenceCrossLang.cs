using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Search;
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for the #58 persistence work, mirroring
/// <c>bench/python/bench_persistence.py</c>: same corpus files, same
/// millisecond-per-operation metric, same auto-scaling best-of-N methodology
/// via <see cref="Harness"/>, a matched Stopwatch loop rather than
/// BenchmarkDotNet. Loaders are called through their path-based overloads —
/// the Python counterpart, <c>Tokenizer.from_file(path)</c>, reads the file
/// itself, and timing a C# in-memory parse against it would flatter C# for free.
/// </summary>
public static class PersistenceCrossLang
{
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

        EmbeddingIndex index = PersistenceBenchmarks.BuildIndex();
        byte[] indexArtifact;
        using (var stream = new MemoryStream())
        {
            index.Save(stream);
            indexArtifact = stream.ToArray();
        }

        // The recipe #378 measured and declined to build in: the caller wraps the
        // stream, both sides, and no library code knows compression happened.
        byte[] indexGzip;
        using (var stream = new MemoryStream())
        {
            using (var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            {
                index.Save(gzip);
            }

            indexGzip = stream.ToArray();
        }

        // No row measured the file path before #336, and it is the one a caller
        // takes: every published index figure came from a MemoryStream.
        string indexFile = Path.Combine(Path.GetTempPath(), $"lodestar-index-{Environment.ProcessId}.json");
        File.WriteAllBytes(indexFile, indexArtifact);

        // Its own path, not the one above: the save row writes where nothing reads,
        // so neither direction is measuring a file the other just touched.
        string saveFile = Path.Combine(Path.GetTempPath(), $"lodestar-index-out-{Environment.ProcessId}.json");

        var results = new List<Harness.OperationResult>
        {
            Harness.Measure("vocab_txt", () => VocabTxtLoader.Load(vocabTxt), new FileInfo(vocabTxt).Length),
            Harness.Measure("tokenizer_json_wordpiece", () => TokenizerJsonLoader.LoadWordPiece(wordPieceJson), new FileInfo(wordPieceJson).Length),
            Harness.Measure("tokenizer_json_unigram", () => TokenizerJsonLoader.LoadUnigram(unigramJson), new FileInfo(unigramJson).Length),
            Harness.Measure("spiece_model", () => SentencePieceModelLoader.Load(spiece), new FileInfo(spiece).Length),
            Harness.Measure("tfidf_save", () =>
            {
                using var stream = new MemoryStream(artifact.Length);
                fitted.Save(stream);
                return stream.Length;
            }, artifact.Length),
            Harness.Measure("tfidf_load", () =>
            {
                using var stream = new MemoryStream(artifact);
                return TfidfVectorizer.Load(stream);
            }, artifact.Length),
            Harness.Measure("embedding_index_save", () =>
            {
                using var stream = new MemoryStream(indexArtifact.Length);
                index.Save(stream);
                return stream.Length;
            }, indexArtifact.Length),
            // The only save row touching a filesystem, and the call a caller actually makes.
            // It priced pre-sizing the file -- #432, refused -- and is what would reprice it.
            Harness.Measure("embedding_index_save_file", () =>
            {
                index.Save(saveFile);

                // The path back, not its length: Save writes a file, so nothing here can
                // be elided, and a FileInfo would put a stat call inside the timed window.
                return saveFile;
            }, indexArtifact.Length),
            Harness.Measure("embedding_index_load", () =>
            {
                using var stream = new MemoryStream(indexArtifact);
                return EmbeddingIndex.Load(stream);
            }, indexArtifact.Length),
            Harness.Measure("embedding_index_load_file", () => EmbeddingIndex.Load(indexFile), indexArtifact.Length),
            Harness.Measure("embedding_index_load_memory", () => EmbeddingIndex.Load(indexArtifact.AsMemory()), indexArtifact.Length),
            // The floor both sides share, and neither is a load: viewing bytes as floats
            // parses no header and validates nothing. It bounds the rows above, not ranks them.
            Harness.Measure("embedding_index_view_floor", () => MemoryMarshal.Cast<byte, float>(indexArtifact.AsSpan()).Length, indexArtifact.Length),
            // What compression costs, beside what it saves: the size column is the
            // point of the pair, and the time column is the price on the same line.
            Harness.Measure("embedding_index_save_gzip", () =>
            {
                using var stream = new MemoryStream(indexGzip.Length);
                using (var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    index.Save(gzip);
                }

                return stream.Length;
            }, indexGzip.Length),
            Harness.Measure("embedding_index_load_gzip", () =>
            {
                using var stream = new MemoryStream(indexGzip);
                using var gzip = new GZipStream(stream, CompressionMode.Decompress);
                return EmbeddingIndex.Load(gzip);
            }, indexGzip.Length),
        };

        File.Delete(indexFile);
        File.Delete(saveFile);

        var payload = new Harness.Output
        {
            Metadata = new Harness.OutputMetadata
            {
                Side = "csharp",
                Library = "Lodestar",
                Runtime = Environment.Version.ToString(),
                Os = Environment.OSVersion.ToString(),
                MinTimeS = Harness.MinTimeSeconds,
                Repeats = Harness.RepeatCount,
            },
            Results = results,
        };

        Harness.Write(outPath, payload);
    }
}
