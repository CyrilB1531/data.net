# NpyFile.Read

Reads a `.npy` file into a block and the shape it was stored under.

<!-- docs-declaration -->

```csharp
public static NpyBlock Read(Stream source, ArtifactLoadOptions options = null)
public static NpyBlock Read(ReadOnlyMemory<byte> npy, ArtifactLoadOptions options = null)
public static NpyBlock Read(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is the file's bytes, never disposed here; `npy` is the whole file
already in memory, which must outlive the block and must not change while it is read; `path` is
the file, whose stream this method owns. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.

**Returns** — [`NpyBlock`](npyblock.md): the elements in C order, and the shape. Read from a
stream or a path, the block also carries the array it filled; read from memory it carries none,
because it borrowed instead of allocating.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException` when
the file does not open with numpy's magic, declares a version this does not read, declares a
header longer than this reader accepts (65 536 bytes), holds a dtype or layout this does not read,
is truncated against its own shape, declares more elements than one block can hold, or exceeds a
bound in `options`.

**Example** — reading what numpy wrote.

<!-- docs-run: skip - reads a file this repository does not commit -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Search;

NpyBlock block = NpyFile.Read("vectors.npy");
var index = new EmbeddingIndex(block.Shape[1], normalize: true);
```

**Example** — the same file already in memory, read without copying it.

```csharp
using Lodestar.Embeddings.Persistence;

using var written = new MemoryStream();
NpyFile.Write(written, [1f, 0f, 0f, 1f], 2, 2);

NpyBlock borrowedBlock = NpyFile.Read(written.GetBuffer().AsMemory(0, (int)written.Length));

int rank = borrowedBlock.Shape.Count;  // => 2
bool borrowed = borrowedBlock.OwnedArray is null;  // => True
```

**Remarks** — **the three overloads differ in what they cost and in what they ask of you.**
`Read(Stream)` and `Read(string)` read the payload straight into the array the block carries —
one copy on net10.0, and two on netstandard2.0, where `Stream.Read(Span<byte>)` does not exist and
the payload stages through a chunk on its way in. Nothing is asked of the caller in return, and
`OwnedArray` names that array so
[`EmbeddingIndex.FromOwnedBlock`](../search/embeddingindex-fromownedblock.md) can adopt it rather
than copy it a second time.

`Read(ReadOnlyMemory<byte>)` copies nothing on either target, and **that is the overload with a
contract**: the block's values alias the bytes you passed, so those bytes must outlive the block
and must not change while it is read — what
[`EmbeddingIndex.Load`](../search/embeddingindex-load.md) already asks of a caller who hands it an
artifact it holds. `OwnedArray` is null on a block read this way, because a borrowed block has no
array to hand over, which is what keeps adoption out of reach from here.
[Decision 0057](../../../decisions/0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md)
has why there are two contracts rather than one.

**The header is never evaluated.** numpy's header is a Python dict literal, and this
accepts a fixed grammar out of it rather than parsing Python: three known keys, each with one of a
closed set of values.

`descr: '|O'` is refused by name and first. That is numpy's object dtype and its payload is a
pickle — arbitrary code, the thing
[decision 0011](../../../decisions/0011-persistence-format.md) rules out for artifacts. The refusal
happens on the header, before the payload is touched.

Refused with what they held, rather than read approximately: `>f4` (big-endian), `<f8` (float64),
`fortran_order: True` (column-major), a scalar shape `()`, and anything past two dimensions.
`(0, 4)` is legal and reads as empty.

**Non-finite values are carried, not refused.** [`EmbeddingIndex.Save`](../search/embeddingindex-save.md)
refuses a `NaN` because a non-finite component poisons every later score of an index this library
owns. A `.npy` is somebody else's data; changing it on the way in would be the worse failure.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`NpyFile`](npyfile.md), [`NpyFile.Write`](npyfile-write.md),
[`NpyBlock`](npyblock.md), [the persistence index](../persistence.md).
