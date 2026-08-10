using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DataNet.Embeddings.Onnx;
using DataNet.Embeddings.Tokenization;

namespace DataNet.Text.Benchmarks;

/// <summary>
/// What batching an ONNX encoder buys over the loop of one-sequence calls the
/// library used to force on the caller.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Read the number before quoting it.</strong> The model here is
/// <c>tiny_embedder.onnx</c>: one Gather node over a 64 × 4 table, because
/// CONTRIBUTING.md forbids committing weights and a real encoder is a hundred
/// megabytes. Its arithmetic is free, so what this measures is almost entirely
/// the per-call cost batching removes — graph dispatch, thread-pool wake-up,
/// tensor wrapping — and none of the matrix multiplication a real encoder would
/// add to both sides of the comparison.
/// </para>
/// <para>
/// So this is an <em>upper bound</em> on the speed-up, not the speed-up. It
/// answers "how much overhead does the unit loop pay per sequence", which is the
/// question the issue asks and the one a synthetic model can answer honestly. On
/// a real encoder the same overhead is paid, and is a smaller share of a much
/// larger total.
/// </para>
/// <para>
/// Run: <c>dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter *BatchEmbedding*</c>
/// </para>
/// </remarks>
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

    // Deliberately not uniform, and deliberately wide: a corpus whose sequences
    // are all the same length hides both what dynamic padding saves and what
    // bucketing saves, which are two of the three things being measured. The
    // spread below runs from 2 tokens to 65, roughly the shape the issue
    // describes — a median near 30 against a tail that would otherwise dictate
    // the width of every row it shares a call with.
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
    /// The same, with length bucketing — the switch the issue says is worth
    /// measuring before committing to.
    /// </summary>
    /// <remarks>
    /// Bucketing only engages when the corpus spans more than one sub-batch, so
    /// with <see cref="SubBatch"/> at 8 the rows at <c>CorpusSize</c> 1 and 8 run
    /// the identical code path to <see cref="EmbedBatch"/>. That is deliberate:
    /// those two rows are the control, and whatever they differ by is this
    /// harness's noise floor. Any claim about the rows at 32 and 128 has to clear
    /// it.
    /// </remarks>
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
    /// Lets the run proceed against the ONNX Runtime package, whose shipped
    /// assembly is not marked as optimized.
    /// </summary>
    /// <remarks>
    /// BenchmarkDotNet refuses to measure code that references a non-optimized
    /// assembly, and it is right to: a Debug build measures nothing. The exception
    /// is narrow and worth stating. `Microsoft.ML.OnnxRuntime` is a managed shim
    /// over a native library, it is consumed from nuget.org exactly as a user
    /// consumes it, and the work being measured happens in native code the flag
    /// does not describe. Only this validator is disabled — the job, the
    /// diagnosers and every other check stay at their defaults.
    /// </remarks>
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
