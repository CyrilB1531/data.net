# Jaro.Distance

`1 - Similarity`, for code that wants a distance rather than a score.

<!-- docs-declaration -->

```csharp
public static double Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, exactly as it does for `Similarity`, which this subtracts from `1`.

**Returns** — `double` in `[0, 1]`, larger meaning less alike. `0` for equal non-empty inputs —
see
the trap below for what two empty ones give.

**Example** — two names that a human would call a near-match.

```csharp
using Lodestar.Text.Distances;

double d = Jaro.Distance("DWAYNE", "DUANE");   // => 0.1777…
```

**Remarks** — this exists so that Jaro can be plugged into code written against a distance rather
than a similarity: a clustering routine, a sort where smaller is better, a threshold expressed as
"at most". It carries no information `Similarity` does not.

The trap it inherits is the empty case running the wrong way: two empty strings are `0` similar
and
therefore `1` apart, the maximum distance this can return. If empty fields are common in your
data,
that is a pair of blanks landing at the far end of every ranking.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Jaro.Similarity`, `JaroWinkler.Distance`,
the [Python equivalence table](../../../equivalence.md).
