using System.Runtime.InteropServices;
using System.Text.Json;
using Lodestar.Embeddings.Tokenization;
using Lodestar.Internal.Persistence;

namespace Lodestar.Embeddings.Persistence;

/// <summary>
/// Reads the <c>vocab.json</c> + <c>merges.txt</c> pair GPT-2 ships, the layout
/// that predates <c>tokenizer.json</c>.
/// </summary>
/// <remarks>
/// Matches <c>tokenizers.models.BPE.from_file(vocab, merges)</c>; see the guide's
/// "Loading vocabularies" section for a worked example and
/// <c>docs/equivalence.md</c>'s <c>models.BPE.from_file</c> row for what stays a
/// parameter.
/// </remarks>
public static class BpeFilesLoader
{
    private const string SourceName = "merges.txt";

    /// <summary>What Python's text mode treats as ending a line.</summary>
    private static readonly char[] LineTerminators = ['\n', '\r'];

    /// <summary>Reads a BPE model from two streams.</summary>
    /// <param name="vocabJson">A <c>vocab.json</c>: a JSON object of token to id. Never disposed by this method.</param>
    /// <param name="merges">A <c>merges.txt</c>: one space-separated pair per line, in rank order. Never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="byteLevel">Whether the model tokenizes through the byte alphabet; <see langword="true"/> describes GPT-2.</param>
    /// <exception cref="InvalidDataException">Either file is malformed, empty, or exceeds a limit.</exception>
    public static BpeVocabulary Load(
        Stream vocabJson,
        Stream merges,
        ArtifactLoadOptions? options = null,
        bool byteLevel = true)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return Parse(
            JsonArtifact.ReadAllBytes(vocabJson, limits),
            JsonArtifact.ReadAllBytes(merges, limits),
            limits,
            byteLevel);
    }

    /// <summary>Reads a BPE model from two files.</summary>
    /// <param name="vocabJsonPath">Path to a <c>vocab.json</c>.</param>
    /// <param name="mergesPath">Path to a <c>merges.txt</c>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="byteLevel">Whether the model tokenizes through the byte alphabet.</param>
    /// <exception cref="InvalidDataException">Either file is malformed, empty, or exceeds a limit.</exception>
    public static BpeVocabulary Load(
        string vocabJsonPath,
        string mergesPath,
        ArtifactLoadOptions? options = null,
        bool byteLevel = true)
    {
        using FileStream vocabFile = JsonArtifact.OpenRead(vocabJsonPath);
        using FileStream mergesFile = JsonArtifact.OpenRead(mergesPath);
        return Load(vocabFile, mergesFile, options, byteLevel);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, Stream, ArtifactLoadOptions?, bool)"/>.</summary>
    /// <param name="vocabJson">A <c>vocab.json</c>; never disposed by this method.</param>
    /// <param name="merges">A <c>merges.txt</c>; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="byteLevel">Whether the model tokenizes through the byte alphabet.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public static async Task<BpeVocabulary> LoadAsync(
        Stream vocabJson,
        Stream merges,
        ArtifactLoadOptions? options = null,
        bool byteLevel = true,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        ReadOnlyMemory<byte> vocabPayload = await JsonArtifact.ReadAllBytesAsync(vocabJson, limits, cancellationToken).ConfigureAwait(false);
        ReadOnlyMemory<byte> mergesPayload = await JsonArtifact.ReadAllBytesAsync(merges, limits, cancellationToken).ConfigureAwait(false);
        return Parse(vocabPayload, mergesPayload, limits, byteLevel);
    }

    private static BpeVocabulary Parse(
        ReadOnlyMemory<byte> vocabPayload,
        ReadOnlyMemory<byte> mergesPayload,
        in ArtifactLimits limits,
        bool byteLevel)
    {
        Dictionary<string, int> vocab = ParseVocab(vocabPayload, limits);
        List<MergePair> merges = ParseMerges(mergesPayload, limits);

        return new BpeVocabulary(vocab, merges)
        {
            ByteLevel = byteLevel,
            // No pipeline in these files (equivalence.md's models.BPE.from_file row), so
            // the split is the lineage's own -- named now that null no longer means it.
            PreTokenizerPattern = byteLevel ? BpePatterns.Gpt2 : BpePatterns.Whitespace,
            PreSplit = null,
        };
    }

    private static Dictionary<string, int> ParseVocab(ReadOnlyMemory<byte> vocabPayload, in ArtifactLimits limits)
    {
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        using (JsonDocument doc = ParseVocabDocument(vocabPayload, limits))
        {
            foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
            {
                // TryGetInt32, not GetInt32: a string value leaks InvalidOperationException,
                // an out-of-range/non-integer leaks FormatException, not InvalidDataException.
                if (entry.Value.ValueKind != JsonValueKind.Number || !entry.Value.TryGetInt32(out int id))
                {
                    throw new InvalidDataException($"The vocab.json maps token '{entry.Name}' to a value that is not an integer id.");
                }
                limits.CheckTokenLength(entry.Name.Length);
                limits.CheckVocabularySize(vocab.Count + 1);
                vocab[entry.Name] = id;
            }
        }
        if (vocab.Count == 0)
        {
            throw new InvalidDataException("The vocab.json is empty: a vocabulary needs at least one token.");
        }
        return vocab;
    }

    private static JsonDocument ParseVocabDocument(ReadOnlyMemory<byte> payload, in ArtifactLimits limits)
    {
        var documentOptions = new JsonDocumentOptions
        {
            MaxDepth = limits.MaxJsonDepth,
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
        };
        try
        {
            JsonDocument document = JsonDocument.Parse(payload, documentOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw new InvalidDataException("The vocab.json is not a JSON object of token to id.");
            }
            return document;
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"The vocab.json is not well-formed JSON: {e.Message}", e);
        }
    }

    private static List<MergePair> ParseMerges(ReadOnlyMemory<byte> mergesPayload, in ArtifactLimits limits)
    {
        var merges = new List<MergePair>();
        string text = DecodeMerges(mergesPayload);
        int start = 0;
        while (start < text.Length)
        {
            int terminator = text.IndexOfAny(LineTerminators, start);
            int stop = terminator < 0 ? text.Length : terminator;
            ParseMergeLine(text, start, stop, merges, limits, isFirstLine: start == 0);
            if (stop >= text.Length)
            {
                break;
            }
            start = stop + 1 < text.Length && text[stop] == '\r' && text[stop + 1] == '\n' ? stop + 2 : stop + 1;
        }
        return merges;
    }

    private static string DecodeMerges(ReadOnlyMemory<byte> payload)
    {
        // A leading BOM would misread the header line as a spurious rank-0 merge --
        // A_byte_order_mark_on_merges_txt_does_not_shift_ranks pins it. TryGetArray: see VocabTxtLoader.Decode.
        ReadOnlySpan<byte> span = payload.Span;
        int offset = span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF ? 3 : 0;

        ReadOnlyMemory<byte> text = payload.Slice(offset);
        return MemoryMarshal.TryGetArray(text, out ArraySegment<byte> segment) && segment.Array is not null
            ? JsonArtifact.Utf8NoBom.GetString(segment.Array, segment.Offset, segment.Count)
            : JsonArtifact.Utf8NoBom.GetString(text.ToArray());
    }

    private static void ParseMergeLine(string text, int start, int stop, List<MergePair> merges, in ArtifactLimits limits, bool isFirstLine)
    {
        int length = stop - start;
        // Blank / "#version" lines are skipped, as in Python; '#' is not a comment
        // marker -- GPT-2 leaves it in its alphabet, see A_merge_whose_left_symbol_starts_with_a_hash_is_kept.
        if (length == 0)
        {
            return;
        }
        if (isFirstLine && length >= 8 && string.CompareOrdinal(text, start, "#version", 0, 8) == 0)
        {
            return;
        }
        limits.CheckTokenLength(length);
        limits.CheckArrayLength(merges.Count + 1, SourceName);

        int space = text.IndexOf(' ', start, length);
        if (space < 0)
        {
            throw new InvalidDataException(
                $"The {SourceName} has a line with no separator: '{text.Substring(start, length)}'. Each line is two symbols separated by a space.");
        }
        // Exactly one space, or the line is refused (tokenizers 0.23.1 agrees) --
        // BpeFilesLoaderTests pins both edge cases: too many spaces, and a trailing one.
        if (text.IndexOf(' ', space + 1, stop - space - 1) >= 0)
        {
            throw new InvalidDataException(
                $"The {SourceName} has a line that is not two space-separated symbols: '{text.Substring(start, length)}'.");
        }
        merges.Add(new MergePair(
            text.Substring(start, space - start),
            text.Substring(space + 1, stop - space - 1)));
    }
}
