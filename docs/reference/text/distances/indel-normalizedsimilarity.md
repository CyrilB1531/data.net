# Indel.NormalizedSimilarity

`1 - NormalizedDistance`, and — multiplied by 100 — exactly rapidfuzz's `fuzz.ratio`.

<!-- docs-declaration -->

```csharp
public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character. rapidfuzz works on code points, so `TextElement.CodePoint` is what reproduces its
numbers on text outside the Basic Multilingual Plane.

**Returns** — `double` in `[0, 1]`, larger meaning more alike. Two empty inputs give `1`.

**Example** — four of the five letters survive in order, so `fuzz.ratio` on this pair is 80.

```csharp
using Lodestar.Text.Distances;

double s = Indel.NormalizedSimilarity("state", "taste");   // => 0.8
```

**Remarks** — this is the member that ports `fuzz.ratio`: multiply by 100 and the numbers agree.
`Lodestar.Fuzzy`'s `Fuzz.Ratio` is literally this call times 100, so use that if you want the 0-100
scale and the rest of the `fuzz.*` family alongside it, and this if you want the `[0, 1]` score on
its own.

What separates this from `RatcliffObershelp`, the other measure this page recommends for longer
text, is the one thing worth reading twice: **this counts every character the two share in order,
however scattered, and `RatcliffObershelp` counts only characters sitting inside a shared unbroken
run.** On the pair above the shared material is `tate` — four characters, but never more than two
of
them adjacent — so this scores `0.8` where `RatcliffObershelp.Similarity` scores `0.6`. On text
whose overlap comes in a few solid passages the two agree exactly; the more interleaved the
overlap,
the further apart they drift, and `("conversation", "voicesranton")` splits them `0.5833…` to
`0.25`.

The trap is that neither of them preprocesses anything. rapidfuzz's `fuzz` functions are routinely
called with a `processor` that lowercases and strips punctuation, and fuzzywuzzy did that by
default; here the comparison is on exactly the characters given, so `"Kitten"` and `"kitten"`
score
below `1`. Normalize the strings yourself before the call.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Indel.NormalizedDistance`, `Levenshtein.NormalizedSimilarity`,
the [migrating-from-rapidfuzz guide](../../../guides/migrating-from-rapidfuzz.md),
the [Python equivalence table](../../../equivalence.md).
