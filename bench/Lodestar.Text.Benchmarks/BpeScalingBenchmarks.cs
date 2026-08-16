using BenchmarkDotNet.Attributes;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

namespace Lodestar.Text.Benchmarks;

/// <summary>
/// How the pathological-token cost scales with length — the #59 measurement
/// deciding whether <c>BpeTokenizer.Merge</c>'s linear scan needs a priority
/// queue. Each length is one repeated-character run, one piece to <c>Merge</c>:
/// cost should roughly quadruple per doubling of <see cref="Length"/> if the
/// scan is the bottleneck, roughly double if not. Split from
/// <see cref="BpeBenchmarks"/> so these four lengths don't also rerun its two
/// benchmarks, which don't depend on <see cref="Length"/>.
/// </summary>
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
