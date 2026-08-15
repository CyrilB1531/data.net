# RatcliffObershelp.Distance

`1 - Similarity`, for code that wants a distance rather than a score.

<!-- docs-declaration -->

```csharp
public static double Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, exactly as it does for `Similarity`, which this subtracts from `1`.

**Returns** — `double` in `[0, 1]`, larger meaning less alike. `0` for equal inputs, and `0` when
both are empty.

**Example** — the share of the two strings their matched blocks fail to cover.

```csharp
using DataNet.Text.Distances;

double d = RatcliffObershelp.Distance("state", "taste");   // => 0.4
```

**Remarks** — the same measure turned round, for code that sorts ascending or thresholds with
"at most". It carries no information `Similarity` does not.

The trap is the empty case landing opposite to `Jaro.Distance`: two empty strings are `1` similar
here and therefore `0` apart, where `Jaro.Distance("", "")` is `1`. Two blank fields are the
closest
possible pair under this measure and the furthest under that one, which is worth pinning down
before
either is used to rank records.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RatcliffObershelp.Similarity`, `Jaro.Distance`,
[decision 0006](../../../decisions/0006-ratcliff-autojunk.md),
the [Python equivalence table](../../../equivalence.md).
