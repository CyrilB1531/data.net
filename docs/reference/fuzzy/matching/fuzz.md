# Fuzz

The seven scorers, all returning a similarity in `[0, 100]`.

<!-- docs-declaration -->

```csharp
public static class Fuzz
```

**Example** — the base scorer on a typo.

```csharp
using Lodestar.Fuzzy;

double score = Fuzz.Ratio("apple pie", "appel pie");  // => 88.88888888888889
```

**Remarks** — `100` is identical and `0` shares nothing; the scale is rapidfuzz's, and the values
match it. They are `double` rather than `int` because rapidfuzz's are, and rounding them here would
make a ported comparison disagree at the boundary of a cutoff.

The seven are one scorer applied to different things: `Ratio` compares the strings, `PartialRatio`
the best window, the `Token*` family the words after sorting or setting, and the `PartialToken*`
pair both transformations at once. `WRatio` chooses among them.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Process`](process.md), [the matching index](../matching.md), the
[rapidfuzz guide](../../../guides/migrating-from-rapidfuzz.md).

## Members

| Member | What it does |
| --- | --- |
| [`Fuzz.PartialRatio`](fuzz-partialratio.md) | The best-matching window of the longer string. |
| [`Fuzz.PartialTokenSetRatio`](fuzz-partialtokensetratio.md) | Set comparison over the best window. |
| [`Fuzz.PartialTokenSortRatio`](fuzz-partialtokensortratio.md) | Sorted comparison over the best window. |
| [`Fuzz.Ratio`](fuzz-ratio.md) | Edit-distance similarity over the whole of both. |
| [`Fuzz.TokenSetRatio`](fuzz-tokensetratio.md) | The words as sets, so extras stop counting. |
| [`Fuzz.TokenSortRatio`](fuzz-tokensortratio.md) | The words sorted, so order stops counting. |
| [`Fuzz.WRatio`](fuzz-wratio.md) | Picks among the others by inspecting the input. |
