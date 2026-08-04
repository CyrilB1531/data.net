using DataNet.Embeddings.Pooling;
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
/// <para>Dispose the instance to release the native session. Thread-safety follows ONNX Runtime's session.</para>
/// </remarks>
public sealed class OnnxTextEmbedder : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputIdsName;
    private readonly string _attentionMaskName;
    private readonly string? _tokenTypeIdsName;
    private readonly string _outputName;

    /// <summary>Opens an ONNX encoder model from <paramref name="modelPath"/>.</summary>
    /// <param name="modelPath">Path to the <c>.onnx</c> model file.</param>
    /// <param name="options">Optional ONNX Runtime session options.</param>
    /// <param name="inputIdsName">Name of the token-ids input (default <c>input_ids</c>).</param>
    /// <param name="attentionMaskName">Name of the attention-mask input (default <c>attention_mask</c>).</param>
    /// <param name="tokenTypeIdsName">Name of the token-type-ids input (default <c>token_type_ids</c>), used only if the model declares it.</param>
    /// <param name="outputName">Name of the token-embeddings output; defaults to the model's first output.</param>
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
        _inputIdsName = inputIdsName;
        _attentionMaskName = attentionMaskName;
        _tokenTypeIdsName = _session.InputMetadata.ContainsKey(tokenTypeIdsName) ? tokenTypeIdsName : null;
        _outputName = outputName ?? _session.OutputMetadata.Keys.First();
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

    /// <summary>Embeds a single tokenized sequence into a normalized sentence vector.</summary>
    /// <param name="inputIds">Token ids.</param>
    /// <param name="attentionMask">Attention mask (same length as <paramref name="inputIds"/>).</param>
    public float[] Embed(IReadOnlyList<long> inputIds, IReadOnlyList<long> attentionMask)
    {
        Guard.NotNull(inputIds);
        Guard.NotNull(attentionMask);
        if (inputIds.Count != attentionMask.Count)
        {
            throw new ArgumentException("inputIds and attentionMask must have equal length.");
        }

        int seqLen = inputIds.Count;
        long[] ids = [.. inputIds];
        long[] mask = [.. attentionMask];

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputIdsName, new DenseTensor<long>(ids, [1, seqLen])),
            NamedOnnxValue.CreateFromTensor(_attentionMaskName, new DenseTensor<long>(mask, [1, seqLen])),
        };
        if (_tokenTypeIdsName is not null)
        {
            inputs.Add(NamedOnnxValue.CreateFromTensor(_tokenTypeIdsName, new DenseTensor<long>(new long[seqLen], [1, seqLen])));
        }

        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs, [_outputName]);
        Tensor<float> output = results.First().AsTensor<float>();

        // Expected shape [1, seqLen, dim]; if the model already pools to [1, dim],
        // treat it as a single token with a full mask.
        int rank = output.Dimensions.Length;
        int dim = output.Dimensions[^1];
        float[] flat = output.ToArray();

        if (rank == 2)
        {
            float[] pooledDirect = flat.AsSpan(0, dim).ToArray();
            Pooler.L2Normalize(pooledDirect);
            return pooledDirect;
        }

        return Pooler.MeanPoolAndNormalize(flat, seqLen, dim, mask);
    }

    /// <summary>Releases the underlying ONNX Runtime session.</summary>
    public void Dispose() => _session.Dispose();
}
