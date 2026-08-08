using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;

namespace DataNet.Text.Benchmarks;

/// <summary>
/// The #59 acceptance bar: byte-level BPE at a cost comparable to the unigram
/// tokenizer already shipped, on the same documents and the same vocabulary size.
/// </summary>
/// <remarks>
/// Both tokenizers are built once in <see cref="Setup"/>, so the numbers are
/// encoding cost rather than model loading. The corpus is the shared 30 000-entry
/// one, so a difference is the algorithm rather than the vocabulary.
/// See <see cref="BpeScalingBenchmarks"/> for the pathological-token case: it
/// was split out so its four lengths do not also rerun <see cref="Unigram"/>
/// and <see cref="Bpe"/>, which do not depend on that parameter.
/// </remarks>
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
