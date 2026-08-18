# AveragePrecision.PerLabel

One average precision per label of a matrix, uncombined — `average_precision_score(…, average=None)`.

<!-- docs-declaration -->

```csharp
public static double[] PerLabel(ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` says whether each label is relevant and `yScore` holds the scores, both
row-major: one row per sample, `labelCount` values each, and the same length. `labelCount` is how
many labels a row holds. `sampleWeight` is one weight per **sample** — per row, not per label — or
empty, the default.

**Returns** — `double[]`, one score per label in column order, each of them what
[`AveragePrecision.Score`](averageprecision-score.md)'s binary overload would return for that column
on its own. A label no sample carries scores `0` rather than being dropped, which is why the
`Macro` mean over these is not the mean over the labels that actually occur.

**Exceptions** — `ArgumentException` when `labelCount` is below `1`, when `yTrue` and `yScore`
disagree in length, when `yTrue` is empty or not a whole number of rows of `labelCount`, or when a
non-empty `sampleWeight` is not one per row.

**Example** — two samples over three labels, where the middle label is carried by neither.

```csharp
using Lodestar.Metrics;

bool[] relevant = [true, false, false, false, false, true];
double[] labelScores = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

double[] perLabel = AveragePrecision.PerLabel(relevant, labelScores, labelCount: 3);
double middle = perLabel[1];  // => 0
```

The outer two score `0.5` each and the middle `0`, which is the `0.3333…` that
`Averaging.Macro` reports and the `0.5` that `Averaging.Weighted` reports — the two differ only in
whether the empty column is counted.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`AveragePrecision.Score`](averageprecision-score.md), the
[Python equivalence table](../../../equivalence.md).
