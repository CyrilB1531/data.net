# Jaro.Similarity

Scores two strings on how many characters they share within a sliding window, and how many of
those
shared characters arrive in a different order.

<!-- docs-declaration -->

```csharp
public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare. `element` says what counts as one
character; jellyfish works on code points, so pass `TextElement.CodePoint` to reproduce its
numbers
on supplementary-plane text.

**Returns** — `double` in `[0, 1]`, larger meaning more alike. `1` for equal non-empty inputs —
see
the trap below for what two empty ones give.

**Example** — one transposition in a six-letter name barely dents the score.

```csharp
using Lodestar.Text.Distances;

double s = Jaro.Similarity("MARTHA", "MARHTA");   // => 0.9444…
```

**Remarks** — Jaro was designed for matching people's names in record linkage, and that is still
what it is best at: short strings, a handful of characters, where a typo or a swap should barely
move the score. It is far more forgiving than `Levenshtein` on exactly those inputs, and far less
meaningful on long text, where the matching window grows with the length and almost everything
ends
up "near" something.

The trap is the empty case, and it is the opposite of what the rest of this page does. Two empty
strings score `0`, not `1` — `Jaro.Similarity("", "")` reports no similarity at all. That is
jellyfish's convention and this implementation follows it deliberately, but it means an empty
field
never matches another empty field here, while `RatcliffObershelp.Similarity("", "")` returns `1`.
A single input being empty also gives `0`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Jaro.Distance`, `JaroWinkler.Similarity`,
[decision 0005](../../../decisions/0005-hamming-jellyfish-divergence.md),
the [Python equivalence table](../../../equivalence.md).
