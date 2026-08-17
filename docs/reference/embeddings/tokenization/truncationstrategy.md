# TruncationStrategy

Which end is cut when a sequence is longer than `MaxLength`.

<!-- docs-declaration -->

```csharp
public enum TruncationStrategy { None, LongestFirst, Right }
```

**Members** — `None` does not truncate, so a sequence over the cap stays over it and the model
decides what to do. `LongestFirst` cuts the longest sequence of a pair first, which is what a
sentence-pair model wants so that neither half disappears. `Right` cuts from the end, keeping the
beginning.

**Example** — the default cap with the end cut.

```csharp
using Lodestar.Embeddings.Tokenization;

var options = new EncodingOptions
{
    MaxLength = 8,
    Truncation = TruncationStrategy.Right,
};

TruncationStrategy strategy = options.Truncation;  // => Right
```

**Remarks** — `Right` is right for most things, because the start of a document usually carries
more of what identifies it. It is not universal: for a question-and-context pair, cutting the end
removes the context and keeps the question, which is why `LongestFirst` exists.

`None` is the honest default for a caller who would rather see a failure than a silently shortened
input — a truncated document embeds to a vector for a **different text**, and nothing downstream
can tell.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EncodingOptions`](encodingoptions.md), [`BatchEncoder`](batchencoder.md).
