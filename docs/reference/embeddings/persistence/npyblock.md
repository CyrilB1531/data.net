# NpyBlock

A float block read from a `.npy` file, with the shape it was stored under.

<!-- docs-declaration -->

```csharp
public readonly record struct NpyBlock(ReadOnlyMemory<float> Values, IReadOnlyList<int> Shape)
```

**Parameters** — `Values` are the elements in C order, so a matrix reads row by row.
`Shape` has one entry for a vector and two for a matrix.

**Example** — the shape is the file's, not a guess.

```csharp
using Lodestar.Embeddings.Persistence;

using var buffer = new MemoryStream();
NpyFile.Write(buffer, [1f, 2f, 3f, 4f, 5f, 6f], 2, 3);
buffer.Position = 0;

NpyBlock block = NpyFile.Read(buffer);
string shape = string.Join("x", block.Shape);   // => 2x3
```

**Remarks** — `Values` is `ReadOnlyMemory<float>` rather than an array because the block is the
file's content and not the caller's buffer to grow. `.Span` reads it without copying; `.ToArray()`
takes a copy when one is wanted.

`Shape` is what the file declared and what the read was bounded against — the element count is
checked before it sizes anything, which is the rule every loader here follows.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`NpyFile`](npyfile.md), [`NpyFile.Read`](npyfile-read.md),
[the persistence index](../persistence.md).
