using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Search;
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

        Console.WriteLine("C# persistence cross-lang bench");
        var results = new List<Harness.OperationResult>
        {
            Harness.Measure("vocab_txt", () => VocabTxtLoader.Load(vocabTxt)),
            Harness.Measure("tokenizer_json_wordpiece", () => TokenizerJsonLoader.LoadWordPiece(wordPieceJson)),
            Harness.Measure("tokenizer_json_unigram", () => TokenizerJsonLoader.LoadUnigram(unigramJson)),
            Harness.Measure("spiece_model", () => SentencePieceModelLoader.Load(spiece)),
            Harness.Measure("tfidf_save", () =>
            {
                using var stream = new MemoryStream(artifact.Length);
                fitted.Save(stream);
                return stream.Length;
            }),
            Harness.Measure("tfidf_load", () =>
            {
                using var stream = new MemoryStream(artifact);
                return TfidfVectorizer.Load(stream);
            }),
            Harness.Measure("embedding_index_save", () =>
            {
                using var stream = new MemoryStream(indexArtifact.Length);
                index.Save(stream);
                return stream.Length;
            }),
            Harness.Measure("embedding_index_load", () =>
            {
                using var stream = new MemoryStream(indexArtifact);
                return EmbeddingIndex.Load(stream);
            }),
        };

        var payload = new Harness.Output
        {
            Metadata = new Harness.OutputMetadata
            {
                Side = "csharp",
                Library = "DataNet",
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
