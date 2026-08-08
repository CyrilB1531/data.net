using System.Runtime.InteropServices;
using System.Text;
using DataNet.Embeddings.Tokenization;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Persistence;

/// <summary>
/// Reads a BERT-style <c>vocab.txt</c>: one token per line, the id being the
/// line number.
/// </summary>
/// <remarks>
/// <para>
/// Matches how <c>transformers.BertTokenizer</c> loads its vocabulary file —
/// text mode with universal newlines, then <c>token.rstrip("\n")</c>, then
/// <c>vocab[token] = index</c>. Two consequences of that loop are reproduced
/// rather than improved on: a blank line is a token whose string is empty, and a
/// token repeated on two lines keeps the <em>last</em> id, because the Python
/// dictionary assignment overwrites.
/// </para>
/// <para>
/// The file carries only the tokens. Whether the model was trained lowercased,
/// and what marks a continuation piece, live in the model's configuration — pass
/// them here, or use <see cref="TokenizerJsonLoader"/>, which reads them from the
/// file.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// WordPieceVocabulary vocab = VocabTxtLoader.Load("bert-base-uncased/vocab.txt", lowercase: true);
/// var tokenizer = new WordPieceTokenizer(vocab);
/// </code>
/// </example>
public static class VocabTxtLoader
{
    private const string SourceName = "vocab.txt";

    /// <summary>What Python's text mode treats as ending a line.</summary>
    private static readonly char[] LineTerminators = ['\n', '\r'];

    /// <summary>Reads a vocabulary from <paramref name="source"/>.</summary>
    /// <param name="source">UTF-8 text, one token per line; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="unkToken">The unknown token; must appear in the file.</param>
    /// <param name="continuationPrefix">Marks non-initial word pieces.</param>
    /// <param name="lowercase">Whether the model was trained on lowercased text.</param>
    /// <exception cref="InvalidDataException">The file is empty, exceeds a limit, or lacks <paramref name="unkToken"/>.</exception>
    public static WordPieceVocabulary Load(
        Stream source,
        ArtifactLoadOptions? options = null,
        string unkToken = "[UNK]",
        string continuationPrefix = "##",
        bool lowercase = false)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return Parse(JsonArtifact.ReadAllBytes(source, limits), limits, unkToken, continuationPrefix, lowercase);
    }

    /// <summary>Reads a vocabulary from the file at <paramref name="path"/>.</summary>
    /// <param name="path">Path to a <c>vocab.txt</c>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="unkToken">The unknown token; must appear in the file.</param>
    /// <param name="continuationPrefix">Marks non-initial word pieces.</param>
    /// <param name="lowercase">Whether the model was trained on lowercased text.</param>
    /// <exception cref="InvalidDataException">The file is empty, exceeds a limit, or lacks <paramref name="unkToken"/>.</exception>
    public static WordPieceVocabulary Load(
        string path,
        ArtifactLoadOptions? options = null,
        string unkToken = "[UNK]",
        string continuationPrefix = "##",
        bool lowercase = false)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Load(file, options, unkToken, continuationPrefix, lowercase);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, ArtifactLoadOptions?, string, string, bool)"/>.</summary>
    /// <param name="source">UTF-8 text, one token per line; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="unkToken">The unknown token; must appear in the file.</param>
    /// <param name="continuationPrefix">Marks non-initial word pieces.</param>
    /// <param name="lowercase">Whether the model was trained on lowercased text.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<WordPieceVocabulary> LoadAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        string unkToken = "[UNK]",
        string continuationPrefix = "##",
        bool lowercase = false,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        ReadOnlyMemory<byte> payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        return Parse(payload, limits, unkToken, continuationPrefix, lowercase);
    }

    private static WordPieceVocabulary Parse(
        ReadOnlyMemory<byte> payload,
        in ArtifactLimits limits,
        string unkToken,
        string continuationPrefix,
        bool lowercase)
    {
        Guard.NotNull(unkToken);
        Guard.NotNull(continuationPrefix);

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        string text = Decode(payload);
        int id = 0;
        int start = 0;
        while (start < text.Length)
        {
            // Python opens the file in text mode, where "\n", "\r\n" and a bare "\r"
            // all end a line. Splitting on "\n" alone would fold a classic-Mac file
            // into one enormous token.
            //
            // IndexOfAny rather than a hand-written scan: it is vectorised, and
            // measured at 0.27 ms against 0.50 ms for the scalar loop on a 30k-entry
            // file — the same cost as the "\n"-only IndexOf it replaces, for a
            // terminator set that is actually correct.
            int terminator = text.IndexOfAny(LineTerminators, start);
            int stop = terminator < 0 ? text.Length : terminator;
            int length = stop - start;
            int next = stop < text.Length && text[stop] == '\r' && stop + 1 < text.Length && text[stop + 1] == '\n'
                ? stop + 2
                : stop + 1;

            // Measured on the line, not on the string it would become: the limit
            // exists to stop a hostile file allocating, so it has to run first.
            limits.CheckTokenLength(length);
            limits.CheckVocabularySize(id + 1);
            vocab[text.Substring(start, length)] = id;
            id++;

            if (stop >= text.Length)
            {
                // A final line with no terminator is still a token.
                break;
            }
            start = next;
        }

        if (id == 0)
        {
            throw new InvalidDataException($"The {SourceName} is empty: a vocabulary needs at least one token.");
        }
        if (!vocab.ContainsKey(unkToken))
        {
            throw new InvalidDataException(
                $"The {SourceName} has no '{unkToken}' entry. Pass the unknown token this model actually uses.");
        }
        return new WordPieceVocabulary(vocab, unkToken, continuationPrefix, lowercase);
    }

    private static string Decode(ReadOnlyMemory<byte> payload)
    {
        // A vocab.txt written on Windows may carry a byte-order mark; Python's
        // "utf-8" codec would keep it as part of the first token, but every real
        // file that has one was meant to be read without it.
        ReadOnlySpan<byte> span = payload.Span;
        int offset = span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF ? 3 : 0;

        // TryGetArray rather than ToArray: this memory always wraps an array — it
        // comes from JsonArtifact.ReadAllBytes — and netstandard2.0's Encoding has
        // no span overload to decode through instead.
        ReadOnlyMemory<byte> text = payload.Slice(offset);
        return MemoryMarshal.TryGetArray(text, out ArraySegment<byte> segment) && segment.Array is not null
            ? JsonArtifact.Utf8NoBom.GetString(segment.Array, segment.Offset, segment.Count)
            : JsonArtifact.Utf8NoBom.GetString(text.ToArray());
    }

}
