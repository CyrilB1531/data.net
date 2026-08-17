# Fuzz.TokenSortRatio

The words sorted before comparing, so their order stops counting.

<!-- docs-declaration -->

```csharp
public static double TokenSortRatio(string a, string b)
```

**Parameters** — `a` and `b` are the strings to compare.

**Returns** — `double` in `[0, 100]`, the [`Ratio`](fuzz-ratio.md) of the two strings after each
is split into words, sorted and rejoined.

**Example** — the same words in a different order.

```csharp
using Lodestar.Fuzzy;

double reordered = Fuzz.TokenSortRatio("new york mets", "mets new york");  // => 100
```

**Remarks** — `100`, where [`Ratio`](fuzz-ratio.md) on the same pair is much lower. Word order is
the difference, and for names, addresses and titles it usually carries no meaning — "Smith, John"
and "John Smith" are one person.

It still counts **extra** words against you: a side with a word the other lacks scores lower, which
is the difference from [`TokenSetRatio`](fuzz-tokensetratio.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Fuzz.TokenSetRatio`](fuzz-tokensetratio.md), [`Fuzz.Ratio`](fuzz-ratio.md).
