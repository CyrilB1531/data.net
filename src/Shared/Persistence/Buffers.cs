namespace Lodestar.Internal.Persistence;

/// <summary>Allocation for the two blocks an artifact load fills whole.</summary>
/// <remarks>
/// The runtime zeroes an array before handing it over, and both of a load's large
/// buffers are overwritten immediately after — the payload by the stream, the vector
/// block by the decoder. At an embedding index's size that is tens of megabytes of
/// large-object-heap writes doing nothing, which #324 measured as the larger half of
/// the load. Only <see langword="struct"/> element types, which is what
/// <c>GC.AllocateUninitializedArray</c> can skip the zeroing for at all.
/// </remarks>
internal static class Buffers
{
    /// <summary>An array whose contents are undefined until the caller writes them.</summary>
    /// <typeparam name="T">The element type, which must hold no references.</typeparam>
    /// <param name="length">How many elements.</param>
    /// <remarks>
    /// <b>Every element the caller then exposes must be written first.</b> A partly
    /// filled buffer has to be sliced to what was written, the way
    /// <see cref="JsonArtifact.ReadAllBytes"/> returns only the bytes the stream gave:
    /// past that, this hands back whatever the heap held, where <c>new T[]</c> handed
    /// back zeroes.
    /// </remarks>
    public static T[] AllocateUninitialized<T>(int length)
        where T : struct =>
#if NET5_0_OR_GREATER
        GC.AllocateUninitializedArray<T>(length);
#else
        new T[length];
#endif
}
