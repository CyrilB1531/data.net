namespace Lodestar.Embeddings.Tokenization;

/// <summary>
/// Turns text into the padded <c>input_ids</c> / <c>attention_mask</c> pair a
/// transformer encoder is fed: special tokens inserted, over-long sequences
/// truncated, the batch padded to its own longest row.
/// </summary>
/// <remarks>
/// The C# equivalent of <c>tokenizer(texts, padding=True, truncation=True,
/// max_length=…)</c> — see <c>docs/guides/embeddings.md</c>'s "Embed a batch".
/// Thread-safe after construction, as the tokenizers it wraps are.
/// </remarks>
public sealed class BatchEncoder
{
    private readonly ISubwordTokenizer _tokenizer;
    private readonly long[] _prefixIds;
    private readonly long[] _suffixIds;
    private readonly long _padId;

    /// <summary>Creates an encoder over a tokenizer and a set of encoding options.</summary>
    /// <param name="tokenizer">The tokenizer whose vocabulary resolves the template's special tokens.</param>
    /// <param name="options">Template, truncation and batching settings; defaults to <see cref="EncodingOptions"/>'s own defaults.</param>
    /// <exception cref="ArgumentException">
    /// The tokenizer's vocabulary does not contain one of the template's tokens, or
    /// <see cref="EncodingOptions.MaxLength"/> leaves no room for them.
    /// </exception>
    public BatchEncoder(ISubwordTokenizer tokenizer, EncodingOptions? options = null)
    {
        Guard.NotNull(tokenizer);
        _tokenizer = tokenizer;
        Options = options ?? new EncodingOptions();

        if (Options.BatchSize < 1)
        {
            throw new ArgumentException($"BatchSize must be at least 1, was {Options.BatchSize}.", nameof(options));
        }

        SpecialTokenTemplate template = Options.Template;
        _prefixIds = ResolveAll(tokenizer, template.PrefixTokens);
        _suffixIds = ResolveAll(tokenizer, template.SuffixTokens);
        _padId = Resolve(tokenizer, template.PadToken);

        if (Options.MaxLength is int max)
        {
            if (max < 1)
            {
                throw new ArgumentException($"MaxLength must be at least 1, was {max}.", nameof(options));
            }
            if (max < template.SpecialTokenCount)
            {
                throw new ArgumentException(
                    $"MaxLength {max} is smaller than the {template.SpecialTokenCount} special tokens the template inserts.",
                    nameof(options));
            }
        }
    }

    /// <summary>The options this encoder was built with, with <c>MaxLength</c> as resolved.</summary>
    public EncodingOptions Options { get; }

    /// <summary>
    /// Encodes one text into its final token ids: template tokens inserted,
    /// truncated to <see cref="EncodingOptions.MaxLength"/>, not padded.
    /// </summary>
    /// <remarks>Matches <c>tokenizer.encode(text).ids</c> with the post-processor and truncation enabled.</remarks>
    /// <param name="text">The text to encode. The empty string yields the template's tokens alone.</param>
    /// <exception cref="ArgumentException">
    /// The sequence exceeds <see cref="EncodingOptions.MaxLength"/> and
    /// <see cref="EncodingOptions.Truncation"/> is <see cref="TruncationStrategy.None"/>.
    /// </exception>
    public long[] Encode(string text)
    {
        Guard.NotNull(text);
        IReadOnlyList<int> ids = _tokenizer.Encode(text).Ids;
        int kept = Truncate(ids.Count, text);

        var encoded = new long[_prefixIds.Length + kept + _suffixIds.Length];
        _prefixIds.CopyTo(encoded, 0);
        for (int i = 0; i < kept; i++)
        {
            encoded[_prefixIds.Length + i] = ids[i];
        }
        _suffixIds.CopyTo(encoded, _prefixIds.Length + kept);
        return encoded;
    }

    /// <summary>
    /// Encodes a sequence of texts into one padded batch, in the order given.
    /// </summary>
    /// <remarks>
    /// Matches <c>tokenizer(texts, padding=True, truncation=True)</c>: every row is
    /// padded to the longest sequence in this call, and the attention mask marks
    /// the padding with 0.
    /// </remarks>
    /// <param name="texts">The texts to encode.</param>
    /// <param name="cancellationToken">Observed between texts; tokenizing a large corpus is not instant.</param>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public EncodedBatch EncodeBatch(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<long[]> sequences = EncodeAll(texts, cancellationToken);
        return Pad(sequences, 0, sequences.Count);
    }

    /// <summary>
    /// Encodes every text into its own row of ids, without padding and without
    /// laying them out as a batch.
    /// </summary>
    /// <remarks>
    /// The front half of <see cref="EncodeBatch"/>, public because a caller that
    /// sub-batches — an inference package running a corpus through a session a
    /// few rows at a time, say — needs the lengths before it can decide how to
    /// group them. Pair it with <see cref="Pad"/>, which is the back half.
    /// </remarks>
    /// <param name="texts">The texts to encode.</param>
    /// <param name="cancellationToken">Observed between texts; tokenizing a large corpus is not instant.</param>
    /// <exception cref="ArgumentNullException"><paramref name="texts"/> is null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public IReadOnlyList<long[]> EncodeAll(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(texts);
        var sequences = new List<long[]>();
        foreach (string text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sequences.Add(Encode(text));
        }
        return sequences;
    }

    /// <summary>Lays a window of <paramref name="count"/> sequences out as one rectangle, padded to its own longest row.</summary>
    /// <param name="sequences">The unpadded encodings.</param>
    /// <param name="start">Index of the first sequence to take.</param>
    /// <param name="count">How many to take.</param>
    /// <param name="order">Optional indirection: row <c>i</c> is <c>sequences[order[start + i]]</c>, which is how length bucketing is expressed.</param>
    /// <remarks>
    /// The back half of <see cref="EncodeBatch"/>: public so that a caller grouping
    /// rows itself does not write a second padding that can drift from this one.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="sequences"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The window is negative, or runs past <paramref name="sequences"/> (or <paramref name="order"/>).</exception>
    public EncodedBatch Pad(IReadOnlyList<long[]> sequences, int start, int count, int[]? order = null)
    {
        Guard.NotNull(sequences);
        Guard.NotLessThan(start, 0);
        Guard.NotLessThan(count, 0);

        // The window was unreachable while both callers lived in this file; a public
        // entry point is where that stops being true.
        int available = order?.Length ?? sequences.Count;
        if (start > available - count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                $"start {start} plus count {count} exceeds the {available} entries available.");
        }

        int width = 0;
        var lengths = new int[count];
        for (int i = 0; i < count; i++)
        {
            int length = sequences[order is null ? start + i : order[start + i]].Length;
            lengths[i] = length;
            width = Math.Max(width, length);
        }

        // A zero-width batch is a tensor ONNX Runtime cannot shape. One padded
        // column, masked off, means "no tokens" without being an empty dimension.
        if (count > 0)
        {
            width = Math.Max(width, 1);
        }

        var ids = new long[count * width];
        var mask = new long[count * width];
        for (int i = 0; i < count; i++)
        {
            long[] sequence = sequences[order is null ? start + i : order[start + i]];
            int offset = i * width;
            sequence.CopyTo(ids, offset);
            for (int t = 0; t < sequence.Length; t++)
            {
                mask[offset + t] = 1;
            }
            for (int t = sequence.Length; t < width; t++)
            {
                ids[offset + t] = _padId;
            }
        }
        return new EncodedBatch(ids, mask, count, width, lengths);
    }

    private static long[] ResolveAll(ISubwordTokenizer tokenizer, IReadOnlyList<string> tokens)
    {
        var ids = new long[tokens.Count];
        for (int i = 0; i < tokens.Count; i++)
        {
            ids[i] = Resolve(tokenizer, tokens[i]);
        }
        return ids;
    }

    private static long Resolve(ISubwordTokenizer tokenizer, string token)
    {
        if (!tokenizer.TryGetId(token, out int id))
        {
            throw new ArgumentException(
                $"The tokenizer's vocabulary has no token '{token}', which the special-token template requires. " +
                "Pass a template matching the model, or a vocabulary that declares it.",
                nameof(tokenizer));
        }
        return id;
    }

    private int Truncate(int tokenCount, string text)
    {
        if (Options.MaxLength is not int max)
        {
            return tokenCount;
        }

        int budget = max - Options.Template.SpecialTokenCount;
        if (tokenCount <= budget)
        {
            return tokenCount;
        }
        if (Options.Truncation == TruncationStrategy.None)
        {
            throw new ArgumentException(
                $"The text encodes to {tokenCount + Options.Template.SpecialTokenCount} tokens, over the MaxLength of {max}, " +
                $"and TruncationStrategy.None refuses to shorten it: \"{Preview(text)}\".",
                nameof(text));
        }
        return budget;
    }

    // CA1845 (use span-based string.Concat): that overload does not exist on
    // netstandard2.0. The Substring form is what makes this file compile there.
#pragma warning disable CA1845
    private static string Preview(string text) =>
        text.Length <= 48 ? text : text.Substring(0, 45) + "...";
#pragma warning restore CA1845
}
