# HingeLoss.MultiClass

The multiclass hinge loss — `sklearn.metrics.hinge_loss` over one decision per class.

<!-- docs-declaration -->

```csharp
public static double MultiClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> predDecision, int classCount, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the true class index of each sample, in `[0, classCount)`.
`predDecision` is one decision per class, row-major: sample 0's classes, then sample 1's.
`classCount` is how many classes each row scores. `sampleWeight` is one weight per sample, or empty.

**Returns** — `double`, `0` or above. `0` when every sample's own class wins by at least `1`.

**Exceptions** — `ArgumentException` when `predDecision` is not `yTrue.Length × classCount`, or a
label is not a class index below `classCount`. `ArgumentOutOfRangeException` when `classCount` is
below two.

**Example** — four samples over three classes.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 2, 1];
double[] decisions =
[
    1.2, 0.3, -0.5,
    0.1, 0.9, 0.2,
    0.4, 0.2, 0.7,
    0.3, 0.1, 0.6,
];

double loss = HingeLoss.MultiClass(truth, decisions, classCount: 3);  // => 0.65
```

The last sample's own class scores `0.1` against a best rival of `0.6`, a margin of `-0.5`, so it
alone costs `1.5` of the `2.6` the four sum to.

**Remarks** — the margin is the true class's decision less the **best of the others**, not less all
of them: Crammer and Singer's multiclass hinge, which is what the reference computes. Only that one
rival matters, so improving a class the sample was never going to be confused with changes nothing.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HingeLoss.Score`](hingeloss-score.md),
[`LogLoss.MultiClass`](logloss-multiclass.md), the [Python equivalence table](../../../equivalence.md).
