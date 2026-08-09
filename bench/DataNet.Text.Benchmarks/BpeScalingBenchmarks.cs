using BenchmarkDotNet.Attributes;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;

namespace DataNet.Text.Benchmarks;

/// <summary>
/// How the pathological-token cost scales with length -- the measurement the
/// #59 plan reserved before deciding whether the linear scan in
/// <c>BpeTokenizer.Merge</c> needs a priority queue.
/// </summary>
/// <remarks>
/// <para>
/// Each length is a single run of the same repeated character, with no split
/// point for <c>Merge</c> to find, so the whole run is one piece to the
/// pre-tokenizer and one merge loop to <c>Merge</c>. A linear scan repeated
/// once per merge costs roughly the square of the symbol count; a doubling
/// of <see cref="Length"/> should then roughly quadruple the measured cost.
/// A cost that instead roughly doubles per doubling says the scan is not the
/// bottleneck it looks like on paper. Neither reading is assumed here --
/// <see cref="Length"/> is stepped so the ratio can be read from the table
/// rather than inferred from a single point.
/// </para>
/// <para>
/// Split from <see cref="BpeBenchmarks"/> so <see cref="Length"/>'s four
/// values do not also rerun <see cref="BpeBenchmarks.Unigram"/> and
/// <see cref="BpeBenchmarks.Bpe"/> four times each: neither depends on
/// <see cref="Length"/>, so multiplying them would only lengthen the run
/// without adding information.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class BpeScalingBenchmarks
{
    private BpeTokenizer _bpe = null!;

    /// <summary>The repeated-character run length: one piece, no split point for <c>Merge</c> to find.</summary>
    [Params(512, 1024, 2048, 4096)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var bounds = new ArtifactLoadOptions
        {
            MaxTotalBytes = 32L * 1024 * 1024,
            MaxVocabularySize = 300_000,
            MaxArrayLength = 300_000,
        };
        _bpe = new BpeTokenizer(TokenizerJsonLoader.LoadBpe(BenchCorpus.Path("tokenizer_30k_bpe.json"), bounds));
    }

    /// <summary>A single long token with no split point, which is where a linear merge scan would hurt.</summary>
    [Benchmark]
    public int BpeOnOnePathologicalToken() => _bpe.Encode(new string('a', Length)).Ids.Count;
}
