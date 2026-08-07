using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Search;
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
    private EmbeddingIndex _index = null!;
    private byte[] _indexArtifact = [];

    /// <summary>
    /// The 10 000 x 384 vector block is 3 840 000 floats, past the loader's default
    /// <c>MaxArrayLength</c> of 1 000 000 (a hostile-input guard, not a size a real
    /// vocabulary or idf vector reaches). A benchmark built to the shape a real
    /// sentence-transformer corpus has must raise the same limit a caller with that
    /// corpus would — this is not a benchmark-only workaround.
    /// </summary>
    private static readonly ArtifactLoadOptions IndexLoadOptions = new() { MaxArrayLength = 4_000_000 };

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

        _index = BuildIndex();
        using var indexStream = new MemoryStream();
        _index.Save(indexStream);
        _indexArtifact = indexStream.ToArray();
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

    [Benchmark]
    public long EmbeddingIndexSave()
    {
        using var stream = new MemoryStream(_indexArtifact.Length);
        _index.Save(stream);
        return stream.Length;
    }

    [Benchmark]
    public EmbeddingIndex EmbeddingIndexLoad()
    {
        using var stream = new MemoryStream(_indexArtifact);
        return EmbeddingIndex.Load(stream, IndexLoadOptions);
    }

    /// <summary>
    /// Ten thousand vectors of 384 dimensions — the shape a sentence-transformer
    /// corpus actually has, and 15 MB of floats.
    /// </summary>
    /// <remarks>
    /// Generated from a fixed seed rather than read from the corpus directory: the
    /// cost of writing a float block depends on how many floats there are, not on
    /// what they are, and both language sides reproduce the same array from the same
    /// arithmetic without a committed fixture.
    /// </remarks>
    internal static EmbeddingIndex BuildIndex()
    {
        const int count = 10_000;
        const int dimension = 384;
        var index = new EmbeddingIndex(dimension, normalize: true);
        var vector = new float[dimension];
        uint state = 12_345;
        for (int item = 0; item < count; item++)
        {
            for (int i = 0; i < dimension; i++)
            {
                // xorshift32: the same three shifts are trivial to reproduce in Python.
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                vector[i] = (state & 0xFFFFFF) / (float)0xFFFFFF - 0.5f;
            }
            index.Add(vector, $"doc-{item}");
        }
        return index;
    }
}
