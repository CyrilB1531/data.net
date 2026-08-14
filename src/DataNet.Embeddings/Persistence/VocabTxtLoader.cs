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
/// Matches <c>transformers.BertTokenizer</c>'s vocabulary loading — see
/// <c>docs/equivalence.md</c>'s loader row for the two Python-loop quirks
/// reproduced, and the guide's "Loading vocabularies" section for a worked
/// example and what stays a parameter (not in this file).
/// </remarks>
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
            // Text mode: "\n", "\r\n" or bare "\r" end a line, so a classic-Mac file
            // does not fold into one token. IndexOfAny is vectorized, unlike a hand scan.
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
        // A BOM survives in Python's "utf-8" codec (kept as part of the first
        // token); stripped here instead -- equivalence.md's loader row records it.
        ReadOnlySpan<byte> span = payload.Span;
        int offset = span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF ? 3 : 0;

        // TryGetArray, not ToArray: this memory always wraps an array (from
        // JsonArtifact.ReadAllBytes), and netstandard2.0's Encoding has no span overload.
        ReadOnlyMemory<byte> text = payload.Slice(offset);
        return MemoryMarshal.TryGetArray(text, out ArraySegment<byte> segment) && segment.Array is not null
            ? JsonArtifact.Utf8NoBom.GetString(segment.Array, segment.Offset, segment.Count)
            : JsonArtifact.Utf8NoBom.GetString(text.ToArray());
    }

}
