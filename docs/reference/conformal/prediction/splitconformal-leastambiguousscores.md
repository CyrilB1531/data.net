# SplitConformal.LeastAmbiguousScores

A classifier's calibration scores under LAC: how much probability the model withheld from the
class that turned out to be right.

<!-- docs-declaration -->

```csharp
public static double[] LeastAmbiguousScores(ReadOnlySpan<double> probabilities, ReadOnlySpan<int> labels, int classCount)
```

**Parameters** — `probabilities` is the predicted probability block, row-major: one row per
calibration sample, `classCount` values each. `labels` holds each sample's true class as an index
into that row. `classCount` is how many classes a row holds.

**Returns** — a fresh `double[]`, one `1 − p̂(true class)` per sample, in the input's order.

**Exceptions** — `ArgumentOutOfRangeException` when `classCount` is not positive.
`ArgumentException` when the block's length is not `labels.Length × classCount`, or a label falls
outside `[0, classCount)`.

**Example** — three calibration samples over three classes, each labelled with a different class.

```csharp
using Lodestar.Conformal;

double[] probabilities =
[
    0.75, 0.15, 0.10,
    0.10, 0.50, 0.40,
    0.25, 0.25, 0.50,
];
int[] labels = [0, 1, 2];

double[] scores = SplitConformal.LeastAmbiguousScores(probabilities, labels, classCount: 3);
double confident = scores[0];   // => 0.25
double unsure = scores[1];      // => 0.5
```

**Remarks** — LAC is *least ambiguous set-valued classifier*, MAPIE's `conformity_score="lac"` and
its default. The score is small when the model put its probability on the right class and large
when it did not, so the calibrated quantile ends up being "how wrong the model is allowed to be,
80 % of the time" — which is exactly what
[`PredictionSet`](splitconformal-predictionset.md) then thresholds against.

The class order matters and is not checked. Whatever order the columns are in here is the order
`PredictionSet` will interpret, and the order the `bool[]` it returns is indexed by. A model whose
`classes_` are sorted differently from your label encoding will produce prediction sets that are
wrong without being invalid.

Nothing here reads the probabilities as a distribution: rows are not required to sum to one, and a
model whose outputs are scores rather than probabilities still calibrates, it simply thresholds
against a number that no longer reads as a probability. What *is* required is that the calibration
rows come from data the model did not train on.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SplitConformal.Quantile`](splitconformal-quantile.md),
[`SplitConformal.PredictionSet`](splitconformal-predictionset.md), the
[Python equivalence table](../../../equivalence.md).
