# NpyBlock

A float block read from a `.npy` file, with the shape it was stored under.

<!-- docs-declaration -->

```csharp
public readonly record struct NpyBlock(ReadOnlyMemory<float> Values, IReadOnlyList<int> Shape)
{
    public float[]? OwnedArray { get; init; }
}
```

**Parameters** — `Values` are the elements in C order, so a matrix reads row by row.
`Shape` has one entry for a vector and two for a matrix. `OwnedArray` is the array the block
owns, or `null` when it borrows one — set by the reader, never by the caller.

**Example** — the shape is the file's, not a guess.

```csharp
using Lodestar.Embeddings.Persistence;

using var buffer = new MemoryStream();
NpyFile.Write(buffer, [1f, 2f, 3f, 4f, 5f, 6f], 2, 3);
buffer.Position = 0;

NpyBlock block = NpyFile.Read(buffer);
string shape = string.Join("x", block.Shape);   // => 2x3
bool adoptable = block.OwnedArray is not null;  // => True
```

**Remarks** — `Values` is `ReadOnlyMemory<float>` rather than an array because the block is the
file's content and not the caller's buffer to grow. `.Span` reads it without copying; `.ToArray()`
takes a copy when one is wanted.

`Shape` is what the file declared and what the read was bounded against — the element count is
checked before it sizes anything, which is the rule every loader here follows.

**`OwnedArray` is filled by the stream reader and by nobody else.** Reading from a `Stream` or a
path allocates an array nothing else holds, and that array is what
[`EmbeddingIndex.FromOwnedBlock`](../search/embeddingindex-fromownedblock.md) may adopt instead of
copying the block again — the transfer
[decision 0056](../../../decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md)
puts on the caller. Reading from a `ReadOnlyMemory<byte>` leaves it null, because those values
are borrowed and there is no array to give; so does a block you construct yourself, which is what
stops adoption being reached without the method that documents it.
[Decision 0057](../../../decisions/0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md)
has why the two reads differ.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`NpyFile`](npyfile.md), [`NpyFile.Read`](npyfile-read.md),
[the persistence index](../persistence.md).
