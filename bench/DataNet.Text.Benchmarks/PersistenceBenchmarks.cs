using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using DataNet.Text.Vectorization;

namespace DataNet.Text.Benchmarks;

/// <summary>
/// The #58 persistence work: three vocabulary loaders and the TF-IDF round trip.
/// </summary>
/// <remarks>
/// Sources are read once into memory in <see cref="Setup"/> and parsed from a
/// <see cref="MemoryStream"/>, so the numbers are parsing cost rather than disk
/// latency. The cross-language harness deliberately does the opposite and uses
/// the path-based API, because that is what a Python user calls.
/// </remarks>
[MemoryDiagnoser]
public class PersistenceBenchmarks
{
    private byte[] _vocabTxt = [];
    private byte[] _wordPieceJson = [];
    private byte[] _unigramJson = [];
    private byte[] _spieceModel = [];
    private byte[] _tfidfArtifact = [];
    private TfidfVectorizer _fitted = null!;

    [GlobalSetup]
    public void Setup()
    {
        _vocabTxt = BenchCorpus.Read("vocab_30k.txt");
        _wordPieceJson = BenchCorpus.Read("tokenizer_30k_wordpiece.json");
        _unigramJson = BenchCorpus.Read("tokenizer_30k_unigram.json");
        _spieceModel = BenchCorpus.Read("spiece_30k.model");

        string[] documents = JsonSerializer.Deserialize<string[]>(
            File.ReadAllBytes(BenchCorpus.Path("documents.json")))!;
        _fitted = new TfidfVectorizer().Fit(documents);

        using var stream = new MemoryStream();
        _fitted.Save(stream);
        _tfidfArtifact = stream.ToArray();
    }

    [Benchmark]
    public WordPieceVocabulary VocabTxt()
    {
        using var stream = new MemoryStream(_vocabTxt);
        return VocabTxtLoader.Load(stream);
    }

    [Benchmark]
    public WordPieceVocabulary TokenizerJsonWordPiece()
    {
        using var stream = new MemoryStream(_wordPieceJson);
        return TokenizerJsonLoader.LoadWordPiece(stream);
    }

    [Benchmark]
    public SentencePieceVocabulary TokenizerJsonUnigram()
    {
        using var stream = new MemoryStream(_unigramJson);
        return TokenizerJsonLoader.LoadUnigram(stream);
    }

    [Benchmark]
    public SentencePieceVocabulary SpieceModel()
    {
        using var stream = new MemoryStream(_spieceModel);
        return SentencePieceModelLoader.Load(stream);
    }

    [Benchmark]
    public int TfidfSave()
    {
        using var stream = new MemoryStream(_tfidfArtifact.Length);
        _fitted.Save(stream);
        return (int)stream.Length;
    }

    [Benchmark]
    public TfidfVectorizer TfidfLoad()
    {
        using var stream = new MemoryStream(_tfidfArtifact);
        return TfidfVectorizer.Load(stream);
    }
}
