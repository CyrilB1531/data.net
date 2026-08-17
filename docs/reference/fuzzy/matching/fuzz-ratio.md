# Fuzz.Ratio

Edit-distance similarity over the whole of both strings.

<!-- docs-declaration -->

```csharp
public static double Ratio(string a, string b)
```

**Parameters** — `a` and `b` are the strings to compare. Order does not change the answer.

**Returns** — `double` in `[0, 100]`. `100` when identical, `0` when they share nothing.

**Example** — a transposition, and the two extremes.

```csharp
using Lodestar.Fuzzy;

double typo = Fuzz.Ratio("apple pie", "appel pie");  // => 88.88888888888889
double same = Fuzz.Ratio("abc", "abc");  // => 100
double nothing = Fuzz.Ratio("abc", "xyz");  // => 0
```

**Remarks** — this is the scorer to reach for when the two strings are **the same kind of thing**:
two spellings of a name, two renderings of an address. It compares everything, which is its
strength and its limit.

Length is what breaks it. `Fuzz.Ratio("apple", "an apple a day")` is low, not because the strings
disagree but because most of the second is absent from the first — and
[`PartialRatio`](fuzz-partialratio.md) is the answer to that shape.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Fuzz.PartialRatio`](fuzz-partialratio.md),
[`Fuzz.TokenSortRatio`](fuzz-tokensortratio.md).
