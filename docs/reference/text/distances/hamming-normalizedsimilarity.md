# Hamming.NormalizedSimilarity

Turns the distance into a score in `[0, 1]`: `1 - distance / max(len(a), len(b))`.

<!-- docs-declaration -->

```csharp
public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
position, and here it changes the answer, because it changes both how many positions there are and
how long the inputs are.

**Returns** — `double` in `[0, 1]`, larger meaning more alike. Two empty inputs give `1`.

**Example** — the emoji is one code point but two UTF-16 units, so the unit chosen moves the
score.

```csharp
using Lodestar.Text;
using Lodestar.Text.Distances;

double s = Hamming.NormalizedSimilarity("a\U0001F600", "a", TextElement.CodePoint);   // => 0.5
```

**Remarks** — jellyfish exposes only the integer distance, so this member has no Python
counterpart
to be compared against; it exists because a raw Hamming distance is as incomparable across pairs
as
any other raw distance. Use it to threshold, and the integer form to report.

Two traps sit next to each other here. The empty case returns `1`, treating two blank fields as a
perfect match. And because the divisor is the **longer** length, a short input compared against a
long one is punished twice — once for every position that differs and once for the length gap —
which is the intended reading for fixed-width data and a misleading one for anything else.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Hamming.Distance`, `Levenshtein.NormalizedSimilarity`,
[decision 0005](../../../decisions/0005-hamming-jellyfish-divergence.md),
the [Python equivalence table](../../../equivalence.md).
