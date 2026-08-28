# NpyFile.Read

Reads a `.npy` file into a block and the shape it was stored under.

<!-- docs-declaration -->

```csharp
public static NpyBlock Read(Stream source, ArtifactLoadOptions options = null)
public static NpyBlock Read(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is the file's bytes, never disposed here; `path` is the file, whose
stream this method owns. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.

**Returns** — [`NpyBlock`](npyblock.md): the elements in C order, and the shape.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException` when
the file does not open with numpy's magic, declares a version this does not read, holds a dtype or
layout this does not read, is truncated against its own shape, or exceeds a bound in `options`.

**Example** — reading what numpy wrote.

<!-- docs-run: skip - reads a file this repository does not commit -->

```csharp
using Lodestar.Embeddings.Persistence;

NpyBlock block = NpyFile.Read("vectors.npy");
var index = new EmbeddingIndex(block.Shape[1], normalize: true);
```

**Remarks** — **the header is never evaluated.** numpy's header is a Python dict literal, and this
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
