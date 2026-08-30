using System.Buffers;
using System.Runtime.InteropServices;

namespace Lodestar.Embeddings.Persistence;

/// <summary>Presents a byte payload's float block as a <see cref="Memory{T}"/> over the same bytes.</summary>
/// <remarks>
/// <see cref="MemoryMarshal.Cast{TFrom, TTo}(ReadOnlySpan{TFrom})"/> reinterprets a span and
/// there is no counterpart for <see cref="Memory{T}"/>, which is the whole reason this type
/// exists rather than a one-line cast. Nothing is copied and nothing is owned: the payload
/// belongs to the caller who passed it, for as long as the block is read.
/// </remarks>
internal sealed class NpyPayloadManager(ReadOnlyMemory<byte> payload) : MemoryManager<float>
{
    public override Span<float> GetSpan() =>
        MemoryMarshal.Cast<byte, float>(MemoryMarshal.AsMemory(payload).Span);

    /// <summary>Not supported: this manager borrows and has nothing to pin.</summary>
    public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

    public override void Unpin() => throw new NotSupportedException();

    // Nothing to release: the manager borrows the caller's bytes and never owns a resource.
    protected override void Dispose(bool disposing) { }
}
