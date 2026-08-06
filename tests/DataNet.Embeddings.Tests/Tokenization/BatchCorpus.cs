using System.Text.Json;
using DataNet.Embeddings.Tokenization;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Reads <c>batch_encoding.json</c> — the vocabulary, the six batches and the
/// embedding table <c>tiny_embedder.onnx</c> gathers from.
/// </summary>
/// <remarks>
/// The corpus is loaded once and shared: it carries a 64 × 4 table and six
/// batches, and re-parsing it per fact is the sort of cost that turns a fast
/// suite into a slow one for no gain, since nothing here is mutable.
/// </remarks>
internal static class BatchCorpus
{
    private static readonly Lazy<BatchOracle> Corpus = new(Load);

    public static BatchOracle Oracle => Corpus.Value;

    /// <summary>A tokenizer over the corpus vocabulary, which carries the three special tokens.</summary>
    public static WordPieceTokenizer Tokenizer() => new(Oracle.Vocabulary, Oracle.UnknownToken);

    private static BatchOracle Load()
    {
        using JsonDocument doc = OracleLoader.Load("batch_encoding.json");
        JsonElement metadata = doc.RootElement.GetProperty("metadata");

        var vocabulary = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in metadata.GetProperty("vocab").EnumerateObject())
        {
            vocabulary[entry.Name] = entry.Value.GetInt32();
        }

        var cases = new List<BatchCase>();
        foreach (JsonElement element in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            cases.Add(new BatchCase(
                element.GetProperty("id").GetInt32(),
                element.GetProperty("name").GetString()!,
                Strings(element.GetProperty("texts")),
                element.GetProperty("max_length") is { ValueKind: JsonValueKind.Number } max ? max.GetInt32() : null,
                element.GetProperty("sequence_length").GetInt32(),
                Rows(element.GetProperty("input_ids"), e => (long)e.GetInt32()),
                Rows(element.GetProperty("attention_mask"), e => (long)e.GetInt32()),
                Rows(element.GetProperty("pooled"), e => e.GetDouble()),
                Rows(element.GetProperty("pooled_normalized"), e => e.GetDouble())));
        }

        return new BatchOracle(
            vocabulary,
            metadata.GetProperty("unk_token").GetString()!,
            Rows(metadata.GetProperty("embedding_table"), e => e.GetDouble()),
            cases);
    }

    private static string[] Strings(JsonElement array) =>
        array.EnumerateArray().Select(e => e.GetString()!).ToArray();

    private static T[][] Rows<T>(JsonElement array, Func<JsonElement, T> read) =>
        array.EnumerateArray().Select(row => row.EnumerateArray().Select(read).ToArray()).ToArray();
}

/// <summary>The whole of <c>batch_encoding.json</c>, parsed.</summary>
/// <param name="Vocabulary">The WordPiece vocabulary, special tokens included.</param>
/// <param name="UnknownToken">The token an out-of-vocabulary word becomes.</param>
/// <param name="EmbeddingTable">The 64 × 4 table <c>tiny_embedder.onnx</c> gathers from.</param>
/// <param name="Cases">The six frozen batches.</param>
internal sealed record BatchOracle(
    Dictionary<string, int> Vocabulary,
    string UnknownToken,
    double[][] EmbeddingTable,
    IReadOnlyList<BatchCase> Cases)
{
    /// <summary>The case with the given name, so a fact names the fixture it depends on.</summary>
    /// <param name="name">The <c>name</c> field of the case, e.g. <c>edges</c>.</param>
    public BatchCase Named(string name) =>
        Cases.FirstOrDefault(c => c.Name == name)
        ?? throw new InvalidOperationException(
            $"batch_encoding.json has no case named '{name}'. It has: {string.Join(", ", Cases.Select(c => c.Name))}.");
}

/// <summary>One frozen batch: the texts, and what HuggingFace and the reference make of them.</summary>
/// <param name="Id">Position in the corpus.</param>
/// <param name="Name">What the fixture exercises, e.g. <c>edges</c>.</param>
/// <param name="Texts">The inputs.</param>
/// <param name="MaxLength">The truncation limit, or null for none.</param>
/// <param name="SequenceLength">The padded width the batch came to.</param>
/// <param name="InputIds">Padded ids, row per text.</param>
/// <param name="AttentionMask">Padded mask, row per text.</param>
/// <param name="Pooled">Mean-pooled reference vectors, before normalization.</param>
/// <param name="PooledNormalized">The reference sentence embeddings.</param>
internal sealed record BatchCase(
    int Id,
    string Name,
    string[] Texts,
    int? MaxLength,
    int SequenceLength,
    long[][] InputIds,
    long[][] AttentionMask,
    double[][] Pooled,
    double[][] PooledNormalized)
{
    /// <summary>The encoding options this fixture was frozen under.</summary>
    /// <remarks>
    /// <c>BatchSize</c> is left large enough to hold the whole fixture: the corpus
    /// froze one padded batch, so a replay that split it would be padding to a
    /// different width and comparing against the wrong rows.
    /// </remarks>
    public EncodingOptions Options => new()
    {
        Template = SpecialTokenTemplate.Bert,
        MaxLength = MaxLength,
        BatchSize = Math.Max(Texts.Length, 1),
        SortByLength = false,
    };

    public override string ToString() => $"case #{Id} '{Name}'";
}
