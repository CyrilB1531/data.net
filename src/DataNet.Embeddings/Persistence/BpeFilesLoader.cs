using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Persistence;

/// <summary>
/// Reads the <c>vocab.json</c> + <c>merges.txt</c> pair GPT-2 ships, the layout
/// that predates <c>tokenizer.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// Matches <c>tokenizers.models.BPE.from_file(vocab, merges)</c>. The two files
/// carry the model and nothing else: what pattern the text was split on, and
/// whether the model is byte-level, live in the tokenizer configuration beside
/// them. Pass them here, or use <see cref="TokenizerJsonLoader.LoadBpe(string, ArtifactLoadOptions?)"/>,
/// which reads them from the file.
/// </para>
/// <para>
/// The defaults describe GPT-2, because that is the model this layout is almost
/// always found in.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// BpeVocabulary vocab = BpeFilesLoader.Load("gpt2/vocab.json", "gpt2/merges.txt");
/// var tokenizer = new BpeTokenizer(vocab);
/// </code>
/// </example>
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
        byte[] vocabPayload = await JsonArtifact.ReadAllBytesAsync(vocabJson, limits, cancellationToken).ConfigureAwait(false);
        byte[] mergesPayload = await JsonArtifact.ReadAllBytesAsync(merges, limits, cancellationToken).ConfigureAwait(false);
        return Parse(vocabPayload, mergesPayload, limits, byteLevel);
    }

    private static BpeVocabulary Parse(
        byte[] vocabPayload,
        byte[] mergesPayload,
        in ArtifactLimits limits,
        bool byteLevel)
    {
        Dictionary<string, int> vocab = ParseVocab(vocabPayload, limits);
        List<MergePair> merges = ParseMerges(mergesPayload, limits);
        int skipped = merges.Count(pair => !vocab.ContainsKey(pair.Left) || !vocab.ContainsKey(pair.Right));

        return new BpeVocabulary(vocab, merges)
        {
            ByteLevel = byteLevel,
            SkippedMerges = skipped,
            PreTokenizerPattern = byteLevel ? BpePatterns.Gpt2 : null,
        };
    }

    private static Dictionary<string, int> ParseVocab(byte[] vocabPayload, in ArtifactLimits limits)
    {
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        using (JsonDocument doc = ParseVocabDocument(vocabPayload, limits))
        {
            foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
            {
                // TryGetInt32, not GetInt32: a string value throws
                // InvalidOperationException and an out-of-range or non-integer
                // number throws FormatException, neither of which this loader
                // documents. TokenizerJsonLoader.ReadWordPiece hits the same
                // token-to-id shape and takes the same guard.
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

    private static JsonDocument ParseVocabDocument(byte[] payload, in ArtifactLimits limits)
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

    private static List<MergePair> ParseMerges(byte[] mergesPayload, in ArtifactLimits limits)
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

    private static string DecodeMerges(byte[] payload)
    {
        // A merges.txt written on Windows may carry a byte-order mark. Left in,
        // it lands on the first line as U+FEFF, "#version" no longer matches at
        // offset 0, and the header row is misread as a merge instead of being
        // skipped -- silently shifting every real merge's rank by one rather
        // than throwing. Mirrors VocabTxtLoader.Decode.
        int offset = payload.Length >= 3 && payload[0] == 0xEF && payload[1] == 0xBB && payload[2] == 0xBF ? 3 : 0;
        return JsonArtifact.Utf8NoBom.GetString(payload, offset, payload.Length - offset);
    }

    private static void ParseMergeLine(string text, int start, int stop, List<MergePair> merges, in ArtifactLimits limits, bool isFirstLine)
    {
        int length = stop - start;
        // A blank line separates nothing, and a leading "#version: 0.2" states
        // the file's format rather than a pair. Both are skipped, as in Python.
        //
        // The header test is anchored to the FIRST line and to "#version"
        // specifically. '#' is not a comment marker in this format: GPT-2's
        // byte-level alphabet leaves '#' as itself, so eight of its 50 000
        // merge lines start with one ("# #", "## ##", "#### ####", ...). A
        // `text[start] == '#'` predicate drops them silently.
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
        merges.Add(new MergePair(
            text.Substring(start, space - start),
            text.Substring(space + 1, stop - space - 1)));
    }
}
