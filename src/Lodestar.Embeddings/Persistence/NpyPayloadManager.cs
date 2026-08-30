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
    /// <summary>The payload's bytes as floats, reinterpreted rather than copied.</summary>
    /// <remarks>
    /// Aligned because the payload is: numpy pads the header so the block starts on a
    /// 64-byte boundary, and <c>NpyFile.Write</c> writes the same padding. A hand-written
    /// file need not, and neither need the caller's origin -- <c>blob.AsMemory(3)</c> is
    /// legal. x64 loads unaligned floats anyway; the netstandard2.0 assembly also ships to
    /// Mono, Unity and .NET Framework, where such a load can cost more than an aligned one.
    /// </remarks>
    public override Span<float> GetSpan() =>
        MemoryMarshal.Cast<byte, float>(MemoryMarshal.AsMemory(payload).Span);

    /// <summary>Pins the payload from <paramref name="elementIndex"/> on.</summary>
    /// <remarks>
    /// Delegated rather than refused, so a block read from memory reaches an interop or
    /// async API that pins its values just as one read from a stream does. The index
    /// counts floats and the payload holds bytes, which is what the scaling is for.
    /// </remarks>
    public override MemoryHandle Pin(int elementIndex = 0) =>
        payload.Slice(elementIndex * sizeof(float)).Pin();

    public override void Unpin()
    {
        // Nothing to release: the handle Pin returned holds the pin and frees it on dispose.
    }

    // Nothing to release: the manager borrows the caller's bytes and never owns a resource.
    protected override void Dispose(bool disposing) { }
}
