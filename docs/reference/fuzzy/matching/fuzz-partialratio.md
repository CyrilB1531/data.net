# Fuzz.PartialRatio

The best-matching window of the longer string.

<!-- docs-declaration -->

```csharp
public static double PartialRatio(string a, string b)
```

**Parameters** — `a` and `b` are the strings to compare; which is longer does not matter.

**Returns** — `double` in `[0, 100]`, the best [`Ratio`](fuzz-ratio.md) over any window of the
longer string as long as the shorter.

**Example** — a short string contained in a longer one.

```csharp
using Lodestar.Fuzzy;

double contained = Fuzz.PartialRatio("apple", "an apple a day");  // => 100
```

**Remarks** — `100`, because `apple` appears verbatim inside the longer string.
[`Ratio`](fuzz-ratio.md) on the same pair is far lower, and both are correct: one asks "are these
the same string", the other "does the short one occur in the long one".

Reach for it when one side is a **fragment** — a search box against titles, a product name against
a description. Do not reach for it when the two are the same kind of thing, because it will happily
score `100` for a short string that matches a small part of a long one and means something else.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Fuzz.Ratio`](fuzz-ratio.md),
[`Fuzz.PartialTokenSortRatio`](fuzz-partialtokensortratio.md).
