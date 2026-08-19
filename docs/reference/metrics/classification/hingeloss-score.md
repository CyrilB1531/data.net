# HingeLoss.Score

The binary hinge loss — `sklearn.metrics.hinge_loss`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> predDecision, int posLabel = 1, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the true labels, one per sample. `predDecision` is the decision value per
sample: positive on `posLabel`'s side of the boundary, and further from zero the more confident — **not**
a probability. `posLabel` is the label on the positive side, `1` by default. `sampleWeight` is one
weight per sample, or empty.

**Returns** — `double`, `0` or above and unbounded. `0` when every sample sits on the right side by a
margin of at least `1`.

**Exceptions** — `ArgumentException` when the inputs disagree in length, are empty, or the weights do
not match.

**Example** — four samples, two of them inside the margin.

```csharp
using Lodestar.Metrics;

int[] truth = [-1, 1, 1, -1];
double[] decisions = [-0.5, 1.2, 0.3, 0.8];

double loss = HingeLoss.Score(truth, decisions);  // => 0.75
```

The second sample is right by more than `1` and costs nothing; the fourth is on the wrong side by
`0.8` and costs `1.8`.

**Remarks** — only the sign of the decision is compared against the label, so relabelling cannot move
the number. On a truth carrying **one class only**, scikit-learn returns a value computed against the
wrong side — `1.65` where the margins say `0.35` — because its `LabelBinarizer` has nothing to
contrast; [the type page](hingeloss.md) has that divergence in full.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HingeLoss.MultiClass`](hingeloss-multiclass.md),
[`ZeroOneLoss.Score`](zerooneloss-score.md), [`LogLoss.Score`](logloss-score.md), the
[Python equivalence table](../../../equivalence.md).
