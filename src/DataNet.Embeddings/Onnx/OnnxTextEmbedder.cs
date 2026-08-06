using System.Buffers;
using DataNet.Embeddings.Pooling;
using DataNet.Embeddings.Tokenization;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace DataNet.Embeddings.Onnx;

/// <summary>
/// Runs a transformer encoder exported to ONNX and turns its token outputs into a
/// single sentence embedding (mean pooling + L2 normalization).
/// </summary>
/// <remarks>
/// <para>
/// Loading and running the model is delegated to ONNX Runtime (the official
/// package) — there is nothing to port. The model weights are <em>not</em> shipped
/// with DataNet; supply a path to a model you downloaded (e.g. a sentence-transformers
/// encoder exported to ONNX). Only the inputs the model declares are fed, so this
/// works whether or not the model uses <c>token_type_ids</c>.
/// </para>
/// <para>
/// Given a tokenizer, <see cref="EmbedBatch(IEnumerable{string}, EncodingOptions, CancellationToken)"/>
/// is the whole pipeline: it is the equivalent of
/// <c>sentence_transformers.SentenceTransformer.encode(texts, batch_size=…)</c>,
/// inserting the model's special tokens, truncating, padding each sub-batch to its
/// own longest row and pooling behind the mask it built itself.
/// </para>
/// <para>Dispose the instance to release the native session. Thread-safety follows ONNX Runtime's session.</para>
/// </remarks>
public sealed class OnnxTextEmbedder : IDisposable
{
    // Consulted in order when no output name is given and the model has several.
    // "Whichever key the dictionary yields first" is not a contract; these are.
    private static readonly string[] PreferredOutputNames =
        ["last_hidden_state", "token_embeddings", "sentence_embedding", "output"];

    private readonly InferenceSession _session;
    private readonly string _inputIdsName;
    private readonly string _attentionMaskName;
    private readonly string? _tokenTypeIdsName;
    private readonly string _outputName;
    private readonly string[] _outputNames;
    private readonly ISubwordTokenizer? _tokenizer;

    /// <summary>Opens an ONNX encoder model from <paramref name="modelPath"/>.</summary>
    /// <param name="modelPath">Path to the <c>.onnx</c> model file.</param>
    /// <param name="options">Optional ONNX Runtime session options.</param>
    /// <param name="inputIdsName">Name of the token-ids input (default <c>input_ids</c>).</param>
    /// <param name="attentionMaskName">Name of the attention-mask input (default <c>attention_mask</c>).</param>
    /// <param name="tokenTypeIdsName">Name of the token-type-ids input (default <c>token_type_ids</c>), used only if the model declares it.</param>
    /// <param name="outputName">Name of the token-embeddings output; see the remarks for how one is chosen when this is null.</param>
    /// <remarks>
    /// When <paramref name="outputName"/> is null the output is chosen
    /// deterministically: the model's only output if it has one, else the first of
    /// <c>last_hidden_state</c>, <c>token_embeddings</c>, <c>sentence_embedding</c>
    /// and <c>output</c> that it declares, else the ordinally first name. Dictionary
    /// key order is not part of ONNX Runtime's contract, so "the model's first
    /// output" was a coin toss on a multi-output model.
    /// </remarks>
    /// <exception cref="ArgumentException">The model declares no input under <paramref name="inputIdsName"/> or <paramref name="attentionMaskName"/>, or no output under <paramref name="outputName"/>.</exception>
    public OnnxTextEmbedder(
        string modelPath,
        SessionOptions? options = null,
        string inputIdsName = "input_ids",
        string attentionMaskName = "attention_mask",
        string tokenTypeIdsName = "token_type_ids",
        string? outputName = null)
    {
        Guard.NotNull(modelPath);
        _session = options is null ? new InferenceSession(modelPath) : new InferenceSession(modelPath, options);
        _inputIdsName = RequireInput(_session, inputIdsName, nameof(inputIdsName));
        _attentionMaskName = RequireInput(_session, attentionMaskName, nameof(attentionMaskName));
        _tokenTypeIdsName = _session.InputMetadata.ContainsKey(tokenTypeIdsName) ? tokenTypeIdsName : null;
        _outputName = ChooseOutput(_session, outputName);
        _outputNames = [_outputName];
    }

    /// <summary>
    /// Opens an ONNX encoder model and attaches the tokenizer the text-level
    /// overloads encode with.
    /// </summary>
    /// <remarks>
    /// The equivalent of building a <c>sentence_transformers.SentenceTransformer</c>
    /// from a model directory: the tokenizer and the graph travel together, because
    /// a model run against a tokenizer it was not trained with produces embeddings
    /// that are wrong without being invalid.
    /// </remarks>
    /// <param name="modelPath">Path to the <c>.onnx</c> model file.</param>
    /// <param name="tokenizer">The tokenizer matching the model — a <see cref="WordPieceTokenizer"/> or a <see cref="SentencePieceTokenizer"/>.</param>
    /// <param name="options">Optional ONNX Runtime session options.</param>
    /// <param name="inputIdsName">Name of the token-ids input (default <c>input_ids</c>).</param>
    /// <param name="attentionMaskName">Name of the attention-mask input (default <c>attention_mask</c>).</param>
    /// <param name="tokenTypeIdsName">Name of the token-type-ids input (default <c>token_type_ids</c>), used only if the model declares it.</param>
    /// <param name="outputName">Name of the token-embeddings output; defaults as described on the other constructor.</param>
    public OnnxTextEmbedder(
        string modelPath,
        ISubwordTokenizer tokenizer,
        SessionOptions? options = null,
        string inputIdsName = "input_ids",
        string attentionMaskName = "attention_mask",
        string tokenTypeIdsName = "token_type_ids",
        string? outputName = null)
        : this(modelPath, options, inputIdsName, attentionMaskName, tokenTypeIdsName, outputName)
    {
        Guard.NotNull(tokenizer);
        _tokenizer = tokenizer;
    }

    /// <summary>The embedding dimension reported by the model output, if known (else -1).</summary>
    public int Dimension
    {
        get
        {
            int[] shape = _session.OutputMetadata[_outputName].Dimensions;
            return shape.Length > 0 ? shape[^1] : -1;
        }
    }

    /// <summary>
    /// The sequence length the model declares, when it declares a fixed one; else
    /// <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// This is what <see cref="EncodingOptions.MaxLength"/> falls back to. An export
    /// with a dynamic axis — what <c>torch.onnx.export</c> produces with
    /// <c>dynamic_axes</c>, and what most published encoders ship — declares a
    /// symbolic dimension, reported here as a negative number and treated as "no
    /// declared maximum". The model's real positional limit lives in its
    /// <c>config.json</c>, not in the graph, so it cannot be read from here and must
    /// be passed as <see cref="EncodingOptions.MaxLength"/>.
    /// </remarks>
    public int? MaxSequenceLength
    {
        get
        {
            int[] shape = _session.InputMetadata[_inputIdsName].Dimensions;
            return shape.Length >= 2 && shape[^1] > 0 ? shape[^1] : null;
        }
    }

    /// <summary>Embeds a single tokenized sequence into a normalized sentence vector.</summary>
    /// <remarks>
    /// Matches <c>session.run(None, {"input_ids": …, "attention_mask": …})</c>
    /// followed by the sentence-transformers mean-pool and normalize. The caller
    /// owns the special tokens and the mask here; use
    /// <see cref="EmbedBatch(IEnumerable{string}, EncodingOptions, CancellationToken)"/>
    /// to have the library own them instead.
    /// </remarks>
    /// <param name="inputIds">Token ids.</param>
    /// <param name="attentionMask">Attention mask (same length as <paramref name="inputIds"/>).</param>
    public float[] Embed(ReadOnlySpan<long> inputIds, ReadOnlySpan<long> attentionMask)
    {
        if (inputIds.Length != attentionMask.Length)
        {
            throw new ArgumentException("inputIds and attentionMask must have equal length.");
        }

        int seqLen = inputIds.Length;
        // ONNX Runtime wraps a Memory<T>, which a span cannot supply, so the copy
        // stays; renting it is what removes the two per-call array allocations.
        long[] ids = ArrayPool<long>.Shared.Rent(seqLen);
        long[] mask = ArrayPool<long>.Shared.Rent(seqLen);
        try
        {
            inputIds.CopyTo(ids);
            attentionMask.CopyTo(mask);
            return Run(ids, mask, batchSize: 1, seqLen)[0];
        }
        finally
        {
            ArrayPool<long>.Shared.Return(ids);
            ArrayPool<long>.Shared.Return(mask);
        }
    }

    /// <summary>
    /// Embeds a corpus: tokenizes, inserts the model's special tokens, truncates,
    /// pads each sub-batch to its own longest sequence, and returns one normalized
    /// vector per input text, in the input order.
    /// </summary>
    /// <remarks>
    /// The equivalent of
    /// <c>SentenceTransformer.encode(texts, batch_size=…, normalize_embeddings=True)</c>.
    /// Requires the constructor overload that takes a tokenizer.
    /// </remarks>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="options">Template, truncation and batching settings; <see langword="null"/> uses the defaults, with <c>MaxLength</c> taken from <see cref="MaxSequenceLength"/>.</param>
    /// <param name="cancellationToken">Observed while tokenizing and between sub-batches.</param>
    /// <exception cref="InvalidOperationException">The embedder was built without a tokenizer.</exception>
    public float[][] EmbedBatch(
        IEnumerable<string> texts,
        EncodingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_tokenizer is null)
        {
            throw new InvalidOperationException(
                "This embedder has no tokenizer. Use the constructor overload that takes one, " +
                "or call EmbedBatch(texts, encoder, cancellationToken) with a BatchEncoder you built.");
        }
        return EmbedBatch(texts, new BatchEncoder(_tokenizer, ResolveOptions(options)), cancellationToken);
    }

    /// <summary>Embeds a corpus using an explicit <see cref="BatchEncoder"/>.</summary>
    /// <remarks>
    /// Same contract as
    /// <see cref="EmbedBatch(IEnumerable{string}, EncodingOptions, CancellationToken)"/>,
    /// for the case where the tokenizer is not the one the embedder was built with.
    /// </remarks>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="encoder">The encoder that owns the tokenizer, template and truncation.</param>
    /// <param name="cancellationToken">Observed while tokenizing and between sub-batches.</param>
    public float[][] EmbedBatch(IEnumerable<string> texts, BatchEncoder encoder, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(encoder);
        List<long[]> sequences = encoder.EncodeAll(texts, cancellationToken);

        int total = sequences.Count;
        var embeddings = new float[total][];
        if (total == 0)
        {
            return embeddings;
        }

        int batchSize = encoder.Options.BatchSize;
        int[]? order = encoder.Options.SortByLength && total > batchSize ? SortedByLength(sequences) : null;

        for (int start = 0; start < total; start += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count = Math.Min(batchSize, total - start);
            EncodedBatch batch = encoder.Pad(sequences, start, count, order);
            float[][] vectors = Run(batch.Ids, batch.Mask, count, batch.SequenceLength);
            for (int i = 0; i < count; i++)
            {
                embeddings[order is null ? start + i : order[start + i]] = vectors[i];
            }
        }
        return embeddings;
    }

    /// <summary>Embeds an already-encoded batch, in batch order.</summary>
    /// <remarks>
    /// The lowest-level batch entry point: one ONNX Runtime call over
    /// <see cref="EncodedBatch.InputIds"/>, pooled behind
    /// <see cref="EncodedBatch.AttentionMask"/>. No sub-batching and no reordering.
    /// </remarks>
    /// <param name="batch">A batch from <see cref="BatchEncoder.EncodeBatch"/>.</param>
    /// <param name="cancellationToken">Observed before the call is made.</param>
    public float[][] EmbedBatch(EncodedBatch batch, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(batch);
        cancellationToken.ThrowIfCancellationRequested();
        return batch.Count == 0 ? [] : Run(batch.Ids, batch.Mask, batch.Count, batch.SequenceLength);
    }

    /// <summary>Releases the underlying ONNX Runtime session.</summary>
    public void Dispose() => _session.Dispose();

    /// <summary>Fills in the model-derived defaults the caller left open.</summary>
    private EncodingOptions ResolveOptions(EncodingOptions? options)
    {
        EncodingOptions resolved = options ?? new EncodingOptions();
        return resolved.MaxLength is null && MaxSequenceLength is int declared
            ? resolved with { MaxLength = declared }
            : resolved;
    }

    /// <summary>Indices of <paramref name="sequences"/> ordered by length, ties by original position.</summary>
    private static int[] SortedByLength(List<long[]> sequences)
    {
        var order = new int[sequences.Count];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }
        // The index tie-break keeps the permutation a function of the input alone,
        // so two runs over the same corpus pad identically.
        Array.Sort(order, (a, b) =>
        {
            int byLength = sequences[a].Length.CompareTo(sequences[b].Length);
            return byLength != 0 ? byLength : a.CompareTo(b);
        });
        return order;
    }

    /// <summary>Runs one padded batch and pools it.</summary>
    private float[][] Run(long[] ids, long[] mask, int batchSize, int seqLen)
    {
        int elements = batchSize * seqLen;
        var inputs = new List<NamedOnnxValue>(3)
        {
            NamedOnnxValue.CreateFromTensor(_inputIdsName, new DenseTensor<long>(ids.AsMemory(0, elements), [batchSize, seqLen])),
            NamedOnnxValue.CreateFromTensor(_attentionMaskName, new DenseTensor<long>(mask.AsMemory(0, elements), [batchSize, seqLen])),
        };
        if (_tokenTypeIdsName is not null)
        {
            inputs.Add(NamedOnnxValue.CreateFromTensor(
                _tokenTypeIdsName,
                new DenseTensor<long>(ZeroTokenTypeIds.OfLength(elements).AsMemory(0, elements), [batchSize, seqLen])));
        }

        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs, _outputNames);
        Tensor<float> output = results[0].AsTensor<float>();

        int rank = output.Dimensions.Length;
        if (rank is not (2 or 3))
        {
            throw new InvalidOperationException(
                $"The model output '{_outputName}' has rank {rank}. A token-embedding output must be " +
                "[batch, sequence, dim] (rank 3), or [batch, dim] (rank 2) if the model pools internally. " +
                "Pass outputName to select a different output.");
        }

        int dim = output.Dimensions[^1];
        // Reading the tensor's own buffer, rather than ToArray(), keeps the whole
        // [batch, sequence, dim] block from being copied on the way to the pooler.
        ReadOnlySpan<float> flat = output is DenseTensor<float> dense ? dense.Buffer.Span : output.ToArray();

        if (rank == 2)
        {
            // Already pooled by the graph: normalize each row and stop.
            var pooled = new float[batchSize][];
            for (int b = 0; b < batchSize; b++)
            {
                float[] vector = flat.Slice(b * dim, dim).ToArray();
                Pooler.L2Normalize(vector);
                pooled[b] = vector;
            }
            return pooled;
        }

        return Pooler.MeanPoolAndNormalizeBatch(flat, batchSize, seqLen, dim, mask.AsSpan(0, elements));
    }

    private static string RequireInput(InferenceSession session, string name, string parameterName)
    {
        if (!session.InputMetadata.ContainsKey(name))
        {
            throw new ArgumentException(
                $"The model declares no input named '{name}'. It declares: {string.Join(", ", session.InputMetadata.Keys)}.",
                parameterName);
        }
        return name;
    }

    private static string ChooseOutput(InferenceSession session, string? requested)
    {
        IReadOnlyDictionary<string, NodeMetadata> outputs = session.OutputMetadata;
        if (requested is not null)
        {
            if (!outputs.ContainsKey(requested))
            {
                throw new ArgumentException(
                    $"The model declares no output named '{requested}'. It declares: {string.Join(", ", outputs.Keys)}.",
                    nameof(requested));
            }
            return requested;
        }
        if (outputs.Count == 1)
        {
            return outputs.Keys.First();
        }
        foreach (string preferred in PreferredOutputNames)
        {
            if (outputs.ContainsKey(preferred))
            {
                return preferred;
            }
        }
        return outputs.Keys.OrderBy(name => name, StringComparer.Ordinal).First();
    }

    /// <summary>A per-thread buffer of zeros, fed as <c>token_type_ids</c>.</summary>
    /// <remarks>
    /// Nothing ever writes to it, so it needs no clearing on reuse — which is the
    /// difference between this and renting from <see cref="ArrayPool{T}"/>. It is
    /// thread-static rather than shared because the session may be run from several
    /// threads at once, and a single-segment model has no other use for it.
    /// </remarks>
    private static class ZeroTokenTypeIds
    {
        [ThreadStatic]
        private static long[]? _buffer;

        public static long[] OfLength(int length)
        {
            long[]? buffer = _buffer;
            if (buffer is null || buffer.Length < length)
            {
                buffer = new long[Math.Max(length, 512)];
                _buffer = buffer;
            }
            return buffer;
        }
    }
}
