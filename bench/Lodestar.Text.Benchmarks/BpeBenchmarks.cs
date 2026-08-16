using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;

namespace Lodestar.Text.Benchmarks;

/// <summary>
/// The #59 acceptance bar: byte-level BPE at a cost comparable to the unigram
/// tokenizer already shipped, same documents, same vocabulary size. Both
/// tokenizers build once in <see cref="Setup"/>, so the numbers are encoding
/// cost, not model loading. See <see cref="BpeScalingBenchmarks"/> for the
/// pathological-token case, split out so it does not also rerun this class's
/// two benchmarks at each of its four lengths.
/// </summary>
[MemoryDiagnoser]
public class BpeBenchmarks
{
    private BpeTokenizer _bpe = null!;
    private SentencePieceTokenizer _unigram = null!;
    private string[] _documents = [];

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
        _unigram = new SentencePieceTokenizer(TokenizerJsonLoader.LoadUnigram(BenchCorpus.Path("tokenizer_30k_unigram.json"), bounds));
        _documents = JsonSerializer.Deserialize<string[]>(File.ReadAllBytes(BenchCorpus.Path("documents.json")))!;
    }

    [Benchmark(Baseline = true)]
    public int Unigram()
    {
        int total = 0;
        foreach (string document in _documents)
        {
            total += _unigram.Encode(document).Ids.Count;
        }
        return total;
    }

    [Benchmark]
    public int Bpe()
    {
        int total = 0;
        foreach (string document in _documents)
        {
            total += _bpe.Encode(document).Ids.Count;
        }
        return total;
    }
}
