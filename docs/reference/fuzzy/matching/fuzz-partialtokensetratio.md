# Fuzz.PartialTokenSetRatio

Word sets, compared over the best-matching window.

<!-- docs-declaration -->

```csharp
public static double PartialTokenSetRatio(string a, string b)
```

**Parameters** — `a` and `b` are the strings to compare.

**Returns** — `double` in `[0, 100]`: the word sets are formed, then
[`PartialRatio`](fuzz-partialratio.md) is applied.

**Example** — a subset of the words, scored as a fragment.

```csharp
using Lodestar.Fuzzy;

string query = "mariners vs angels";
string candidate = "los angeles angels vs seattle mariners";

double score = Fuzz.PartialTokenSetRatio(query, candidate);  // => 100
```

**Remarks** — **the most forgiving of the seven**, and the one to justify before using. It ignores
word order, ignores extra words, and scores the best window rather than the whole — three
allowances at once, so a `100` here says much less than a `100` from [`Ratio`](fuzz-ratio.md).

It earns its place on messy, human-entered text where every one of those three differences is
noise. On clean data it will merge records that should stay apart.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Fuzz.TokenSetRatio`](fuzz-tokensetratio.md), [`Fuzz.WRatio`](fuzz-wratio.md).
