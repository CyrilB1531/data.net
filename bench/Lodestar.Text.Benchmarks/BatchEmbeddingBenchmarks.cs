using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Lodestar.Embeddings.Tokenization;
using Lodestar.Onnx;

namespace Lodestar.Text.Benchmarks;

/// <summary>
/// What batching an ONNX encoder buys over one call per sequence. Uses
/// <c>tiny_embedder.onnx</c> — no weights to commit, and free arithmetic — so
/// this isolates per-call overhead (graph dispatch, thread-pool wake-up, tensor
/// wrapping) as an upper bound on the speed-up: a real encoder pays the same
/// overhead as a smaller share of a larger total. Measured numbers and caveats:
/// docs/guides/performance.md. Run:
/// <c>dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter *BatchEmbedding*</c>
/// </summary>
// CA1001 (owns a disposable field but is not IDisposable): BenchmarkDotNet owns
// this type's lifecycle and calls [GlobalCleanup] below, which disposes
// _embedder. IDisposable would advertise an ownership no caller ever takes.
#pragma warning disable CA1001
[MemoryDiagnoser]
[Config(typeof(NonOptimizedOnnxRuntime))]
public class BatchEmbeddingBenchmarks
{
    private static readonly string ModelPath =
        Path.Combine(AppContext.BaseDirectory, "oracles", "tiny_embedder.onnx");

    // Not uniform, and wide (2-65 tokens): same-length rows would hide what
    // padding and bucketing each save.
    private static readonly string[] Sentences = BuildCorpus();

    private static string[] BuildCorpus()
    {
        const string phrase = "the quick brown fox jumps and the dog runs while the cat plays ";
        var sentences = new string[16];
        for (int i = 0; i < sentences.Length; i++)
        {
            // 1, 2, 3, 5, 8, 13, 21… words: a spread wide enough that padding to
            // the longest row is visibly wasteful when the rows are unsorted.
            int words = 1 + ((i * i) % 13) * 5;
            sentences[i] = string.Concat(Enumerable.Repeat(phrase, (words / 13) + 1))
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(words)
                .Aggregate((a, b) => a + " " + b);
        }
        return sentences;
    }

    private OnnxTextEmbedder _embedder = null!;
    private BatchEncoder _encoder = null!;
    private string[] _corpus = [];

    /// <summary>Corpus size, i.e. how many texts one call embeds.</summary>
    [Params(1, 8, 32, 128)]
    public int CorpusSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var vocabulary = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string word in Sentences.SelectMany(s => s.Split(' ')).Distinct(StringComparer.Ordinal))
        {
            vocabulary[word] = vocabulary.Count + 1;
        }
        vocabulary["[UNK]"] = 0;
        vocabulary["[CLS]"] = vocabulary.Count;
        vocabulary["[SEP]"] = vocabulary.Count;
        vocabulary["[PAD]"] = vocabulary.Count;

        var tokenizer = new WordPieceTokenizer(vocabulary, "[UNK]");
        _embedder = new OnnxTextEmbedder(ModelPath, tokenizer);
        _encoder = new BatchEncoder(tokenizer);
        _corpus = Enumerable.Range(0, CorpusSize).Select(i => Sentences[i % Sentences.Length]).ToArray();
    }

    [GlobalCleanup]
    public void Cleanup() => _embedder.Dispose();

    /// <summary>
    /// One ONNX Runtime call per text — what the guide's three lines amounted to
    /// before <c>EmbedBatch</c> existed.
    /// </summary>
    [Benchmark(Baseline = true)]
    public float[][] UnitLoop()
    {
        var vectors = new float[_corpus.Length][];
        for (int i = 0; i < _corpus.Length; i++)
        {
            long[] ids = _encoder.Encode(_corpus[i]);
            var mask = new long[ids.Length];
            for (int t = 0; t < mask.Length; t++)
            {
                mask[t] = 1;
            }
            vectors[i] = _embedder.Embed(ids, mask);
        }
        return vectors;
    }

    /// <summary>Sub-batches padded to their own longest row, in input order.</summary>
    [Benchmark]
    public float[][] EmbedBatch() => _embedder.EmbedBatch(_corpus, Unsorted);

    /// <summary>
    /// The same, with length bucketing. Only engages past one sub-batch, so with
    /// <see cref="SubBatch"/> at 8 the rows at <c>CorpusSize</c> 1 and 8 run the
    /// identical path to <see cref="EmbedBatch"/> — those two are the control;
    /// a claim about 32 or 128 must clear their noise floor (docs/guides/performance.md).
    /// </summary>
    [Benchmark]
    public float[][] EmbedBatchBucketed() => _embedder.EmbedBatch(_corpus, Sorted);

    /// <summary>
    /// Small enough that bucketing has several sub-batches to work with at the
    /// larger corpus sizes; the library's own default is 32.
    /// </summary>
    private const int SubBatch = 8;

    private static readonly EncodingOptions Unsorted =
        new() { BatchSize = SubBatch, SortByLength = false };

    private static readonly EncodingOptions Sorted =
        new() { BatchSize = SubBatch, SortByLength = true };

    /// <summary>
    /// Lets the run proceed against ONNX Runtime's shipped assembly, which is not
    /// marked optimized. BenchmarkDotNet is right to refuse that in general — a
    /// Debug build measures nothing — but <c>Microsoft.ML.OnnxRuntime</c> is a
    /// managed shim over native code, consumed exactly as a user consumes it, and
    /// the work measured happens in the native library the flag can't see. Only
    /// this validator is disabled; the job, diagnosers and everything else stay default.
    /// </summary>
    // SonarLint S1144: the constructor is never called from this file. It is called
    // by BenchmarkDotNet, which instantiates the type named in [Config(typeof(…))]
    // by reflection — a call graph no analyzer can follow. Deleting the constructor
    // as dead code is exactly what the rule would have you do, and the benchmark
    // then refuses to run.
#pragma warning disable S1144
    private sealed class NonOptimizedOnnxRuntime : ManualConfig
    {
        public NonOptimizedOnnxRuntime() => Options |= ConfigOptions.DisableOptimizationsValidator;
    }
#pragma warning restore S1144
}
