# NpyFile.Write

Writes a block of `float` as a `.npy`, the counterpart of `numpy.save`.

<!-- docs-declaration -->

```csharp
public static void Write(Stream destination, ReadOnlySpan<float> values, int[] shape)
public static void Write(string path, ReadOnlySpan<float> values, int[] shape)
```

**Parameters** — `destination` is flushed but never disposed, the caller owning it; `path` names a
file this method opens and owns, replacing any existing one. `values` are the elements in C order.
`shape` is one dimension for a vector or two for a matrix, and its product must equal `values`'s
length.

**Exceptions** — `ArgumentNullException` for a null destination, path or shape.
`ArgumentException` when `shape` is empty, has more than two dimensions, holds a negative
dimension, or describes a different number of elements than `values` holds.

**Example** — a matrix numpy will read back unchanged.

```csharp
using Lodestar.Embeddings.Persistence;

using var buffer = new MemoryStream();
NpyFile.Write(buffer, [1f, 2f, 3f, 4f, 5f, 6f], 2, 3);
long written = buffer.Length;   // => 152
```

**Remarks** — the output is **byte-for-byte what numpy writes** for the same array: the same magic,
the same 1.0 version, the same header text, and the same padding to a 64-byte payload boundary.
That is pinned by tests comparing against files `numpy.save` produced, rather than against this
project's own reading of the format.

The block is always `<f4` — little-endian `float32`, C order. On a big-endian machine the bytes are
swapped on the way out, so the file is defined by the format rather than by the architecture that
happened to write it.

`shape` is `params`, so a matrix is `Write(stream, values, rows, columns)` and a vector is
`Write(stream, values, count)`. It is checked against `values` before anything is written: a shape
that does not describe the block is a caller error, not a file to be produced and puzzled over
later.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`NpyFile`](npyfile.md), [`NpyFile.Read`](npyfile-read.md),
[the persistence index](../persistence.md).
