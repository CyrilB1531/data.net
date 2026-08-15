# JaroWinkler.Distance

`1 - Similarity`, for code that wants a distance rather than a score.

<!-- docs-declaration -->

```csharp
public static double Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, double prefixWeight = 0.1, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare, `prefixWeight` is what each shared
leading character is worth (`0.1` by default) and `element` says what counts as one character —
all
three exactly as for `Similarity`, which this subtracts from `1`.

**Returns** — `double`, normally in `[0, 1]` and larger meaning less alike.

**Example** — a swapped pair of letters, forgiven almost entirely because the prefix agrees.

```csharp
using DataNet.Text.Distances;

double d = JaroWinkler.Distance("MARTHA", "MARHTA");   // => 0.0388…
```

**Remarks** — the same measure as `Similarity`, turned round for code that sorts ascending or
thresholds with "at most". Nothing else differs.

It inherits `Similarity`'s unvalidated `prefixWeight`, and inverts its consequence: a weight above
`0.25` can push the similarity past `1` and therefore this **below zero**. A negative distance
breaks the assumption that most clustering and nearest-neighbour code makes without stating it, so
leave `prefixWeight` alone unless you have a reason and a test.

**Applies to** — net10.0, netstandard2.0.

**See also** — `JaroWinkler.Similarity`, `Jaro.Distance`,
the [Python equivalence table](../../../equivalence.md).
