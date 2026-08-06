using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Text.Vectorization;

namespace DataNet.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for the #58 persistence work, mirroring
/// <c>bench/python/bench_persistence.py</c> exactly: same corpus files, same
/// millisecond-per-operation metric, same auto-scaling best-of-N methodology.
/// Timing itself is <see cref="Harness"/>, a matched Stopwatch loop rather than
/// BenchmarkDotNet, so both languages are measured the same way.
/// </summary>
/// <remarks>
/// Loaders are called through their path-based overloads on purpose. The Python
/// counterpart is <c>Tokenizer.from_file(path)</c>, which reads the file itself;
/// timing a C# in-memory parse against it would flatter C# for free.
/// </remarks>
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
