# NpyFile

Reads and writes a block of `float` in numpy's `.npy` format.

<!-- docs-declaration -->

```csharp
public static class NpyFile
```

**Example** — a block out and the same block back, without touching a file.

```csharp
using Lodestar.Embeddings.Persistence;

float[] vectors = [1f, -2f, 3.5f, 0f, 0.25f, -0.5f];

using var buffer = new MemoryStream();
NpyFile.Write(buffer, vectors, 3, 2);
buffer.Position = 0;

NpyBlock block = NpyFile.Read(buffer);
Console.WriteLine($"{block.Shape[0]}x{block.Shape[1]} {block.Values.Length}"); // => 3x2 6
```

**Remarks** — this is an **interop format for a float matrix, not a second artifact format**.
[`EmbeddingIndex.Save`](../search/embeddingindex-save.md) is unchanged and still writes the
versioned JSON [decision 0011](../../../decisions/0011-persistence-format.md) chose; a `.npy`
carries no ids, no normalization flag and no schema header, so it cannot stand in for one.

What it is for is the other direction: vectors that came from numpy, or vectors going to it.

**The header is a Python dict literal and is never evaluated.** numpy writes
`{'descr': '<f4', 'fortran_order': False, 'shape': (3, 4), }` — executable source, which is the
hazard decision 0011 refused `pickle` over. Only a fixed grammar is accepted here: three known
keys, each with one of a closed set of values, and anything else refused rather than interpreted.

`descr: '|O'` — numpy's object dtype, whose payload *is* a pickle — is refused by name, before a
byte of that payload is read.

Only `float32`, C order, little-endian, one or two dimensions. A file outside that is refused with
a message naming what it held, rather than read approximately.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`NpyBlock`](npyblock.md), [`EmbeddingIndex`](../search/embeddingindex.md),
[the persistence index](../persistence.md).

## Members

| Member | What it does |
| --- | --- |
| [`NpyFile.Read`](npyfile-read.md) | Reads a `.npy` into a block and its shape. |
| [`NpyFile.Write`](npyfile-write.md) | Writes a block as a `.npy`. |
