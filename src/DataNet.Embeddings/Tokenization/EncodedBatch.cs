namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// A batch of sequences padded to a common length, laid out as the two rectangular
/// int64 tensors a transformer encoder expects.
/// </summary>
/// <remarks>
/// The C# form of <c>tokenizer(texts, padding=True)</c>: <c>input_ids</c> and
/// <c>attention_mask</c>, row-major <c>[Count × SequenceLength]</c> — this
/// batch's own longest sequence, not the model maximum. Built here, not by the
/// caller, so padding cannot silently reach the pooled vector.
/// </remarks>
public sealed class EncodedBatch
{
    // The tensors are handed straight to ONNX Runtime, which needs an array to
    // wrap; the spans below are the public, non-copying view of them.
    internal readonly long[] Ids;
    internal readonly long[] Mask;

    internal EncodedBatch(long[] ids, long[] mask, int count, int sequenceLength, int[] lengths)
    {
        Ids = ids;
        Mask = mask;
        Count = count;
        SequenceLength = sequenceLength;
        Lengths = lengths;
    }

    /// <summary>Number of sequences in the batch.</summary>
    public int Count { get; }

    /// <summary>The padded length every sequence was brought to — the longest in the batch.</summary>
    public int SequenceLength { get; }

    /// <summary>Token ids, row-major <c>[Count × SequenceLength]</c>, padded with the template's pad token.</summary>
    public ReadOnlySpan<long> InputIds => Ids;

    /// <summary>Attention mask, row-major <c>[Count × SequenceLength]</c>: 1 for a real token, 0 for padding.</summary>
    public ReadOnlySpan<long> AttentionMask => Mask;

    /// <summary>The unpadded length of each sequence, in batch order.</summary>
    public IReadOnlyList<int> Lengths { get; }

    /// <summary>The ids of one sequence, without its padding.</summary>
    /// <param name="index">Position in the batch.</param>
    public ReadOnlySpan<long> Sequence(int index)
    {
        if ((uint)index >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"The batch holds {Count} sequences.");
        }
        return Ids.AsSpan(index * SequenceLength, Lengths[index]);
    }
}
