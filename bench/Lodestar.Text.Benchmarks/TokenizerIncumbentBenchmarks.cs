using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

namespace Lodestar.Text.Benchmarks;

// CA1822: see LevenshteinIncumbentBenchmarks.
#pragma warning disable CA1822

/// <summary>Which sub-word model a row of the incumbent table measures.</summary>
public enum TokenizerModel
{
    /// <summary>WordPiece, both sides reading <c>vocab_30k.txt</c>'s vocabulary.</summary>
    WordPiece,

    /// <summary>SentencePiece unigram, both sides reading <c>spiece_30k.model</c>.</summary>
    SentencePiece,
}

/// <summary>
/// Our two sub-word tokenizers against `Microsoft.ML.Tokenizers`, the first-party
/// incumbent issue #438 names for this package.
/// </summary>
/// <remarks>
/// Both sides encode the same documents from the same artefact and were checked to
/// return identical ids first; bench/README.md section 15 has that check, and why the
/// model is a parameter rather than four methods.
/// </remarks>
[MemoryDiagnoser]
public class TokenizerIncumbentBenchmarks
{
    private string[] _documents = [];
    private WordPieceTokenizer _wordPiece = null!;
    private SentencePieceTokenizer _sentencePiece = null!;
    private Microsoft.ML.Tokenizers.WordPieceTokenizer _theirWordPiece = null!;
    private Microsoft.ML.Tokenizers.SentencePieceTokenizer _theirSentencePiece = null!;

    /// <summary>The model this row measures on both libraries.</summary>
    [Params(TokenizerModel.WordPiece, TokenizerModel.SentencePiece)]
    public TokenizerModel Model { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var bounds = new ArtifactLoadOptions
        {
            MaxTotalBytes = 32L * 1024 * 1024,
            MaxVocabularySize = 300_000,
            MaxArrayLength = 300_000,
        };
        _documents = JsonSerializer.Deserialize<string[]>(
            File.ReadAllBytes(BenchCorpus.Path("documents.json")))!;

        _wordPiece = new WordPieceTokenizer(
            TokenizerJsonLoader.LoadWordPiece(BenchCorpus.Path("tokenizer_30k_wordpiece.json"), bounds));
        _sentencePiece = new SentencePieceTokenizer(
            SentencePieceModelLoader.Load(BenchCorpus.Path("spiece_30k.model"), bounds));

        using (var vocabulary = File.OpenRead(BenchCorpus.Path("vocab_30k.txt")))
        {
            _theirWordPiece = Microsoft.ML.Tokenizers.WordPieceTokenizer.Create(vocabulary);
        }

        using var model = File.OpenRead(BenchCorpus.Path("spiece_30k.model"));
        // Positionally: addBeginOfSentence, addEndOfSentence. Left on, every document
        // would carry a leading <s> ours does not emit, and the ids would not compare.
        _theirSentencePiece = Microsoft.ML.Tokenizers.SentencePieceTokenizer.Create(model, false, false);
    }

    [Benchmark(Baseline = true)]
    public int Lodestar()
    {
        int total = 0;
        foreach (string document in _documents)
        {
            total += Model == TokenizerModel.WordPiece
                ? _wordPiece.Encode(document).Ids.Count
                : _sentencePiece.Encode(document).Ids.Count;
        }
        return total;
    }

    [Benchmark]
    public int MlTokenizers()
    {
        int total = 0;
        foreach (string document in _documents)
        {
            total += Model == TokenizerModel.WordPiece
                ? _theirWordPiece.EncodeToIds(document).Count
                : _theirSentencePiece.EncodeToIds(document).Count;
        }
        return total;
    }
}
