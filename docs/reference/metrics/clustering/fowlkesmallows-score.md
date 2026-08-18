# FowlkesMallows.Score

The geometric mean of pair precision and pair recall for two labellings.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference labelling and `labelsPred` the one being scored;
they must be the same length. The label *values* carry no meaning — only which samples share one.

**Returns** — `double` in `[0, 1]`. `1` when both labellings put exactly the same pairs together,
`0` when they share no pair at all.

**Exceptions** — `ArgumentException` when the two labellings disagree in length.

**Example** — a renaming, a disagreement, and the degenerate case that surprises.

```csharp
using Lodestar.Metrics;

double renamed = FowlkesMallows.Score([0, 0, 1, 1], [2, 2, 0, 0]);  // => 1
double independent = FowlkesMallows.Score([0, 0, 1, 1], [0, 1, 0, 1]);  // => 0
double empty = FowlkesMallows.Score([], []);  // => 0
```

**Remarks** — **an empty input and a single sample score `0` here, where the other five metrics in
this namespace score `1`.** That is not an inconsistency to route around: those five ask whether the
two labellings disagree anywhere, and agreeing about nothing is agreeing. This one counts *agreeing
pairs*, and an input with no pairs has none. The value is scikit-learn's.

Nothing here is corrected for chance, which is the difference from
[`AdjustedMutualInformation.Score`](adjustedmutualinformation-score.md) and
[`AdjustedRand.Score`](adjustedrand-score.md). Splitting a labelling into more clusters can raise
this score without the clustering having improved.

The result is grouped as `sqrt(tk/pk) · sqrt(tk/qk)` rather than `tk / sqrt(pk·qk)`, following the
reference's own associativity — the two disagree in the last places, and the frozen corpus reads
them at `1e-9`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`FowlkesMallows`](fowlkesmallows.md), [`AdjustedRand.Score`](adjustedrand-score.md),
[the clustering index](../clustering.md).
