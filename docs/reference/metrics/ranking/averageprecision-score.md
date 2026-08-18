# AveragePrecision.Score

The step sum over the precision-recall curve — `sklearn.metrics.average_precision_score`, binary or
over a label matrix.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel = 1, ReadOnlySpan<double> sampleWeight = default)
public static double Score(ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, Averaging averaging = Averaging.Macro, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — the binary overload takes `yTrue`, one label per sample, and `yScore`, one score
per sample: the higher, the more the model believes `posLabel`. `posLabel` is the label counted as
positive, `1` by default — scikit-learn infers it from the labels present, and this asks instead.
The matrix overload takes `yTrue` as one boolean per label per sample and `yScore` of the same
shape, both row-major with `labelCount` values per row, and `averaging` decides how the per-label
scores are combined. `sampleWeight` is one weight per **sample** — per row, not per label — or
empty, the default, to weight every sample by `1`.

**Returns** — `double` in `[0, 1]` for non-negative weights. `1` when every positive sample outranks
every negative one.

**`0` when no sample carries `posLabel`.** scikit-learn warns "No positive class found in y_true,
recall is set to one for all thresholds" and returns a value rather than refusing, and that value is
reproduced here — where [`RocAuc.Score`](../classification/rocauc-score.md) on the same walk throws,
because a ROC area genuinely has no value without both classes and this has one.

**Exceptions** — `ArgumentException` when `yTrue` and `yScore` disagree in length, when they are
empty, when a score is `NaN`, or — on the matrix overload — when `labelCount` is below `1`, when
`yTrue` is not a whole number of rows of `labelCount`, or when a non-empty `sampleWeight` is not one
per row. `ArgumentOutOfRangeException` when `averaging` is `Averaging.Binary`, which scores one
positive label of two and has no meaning over a matrix, or is not a declared member at all.

**Example** — the worked binary case, where the sum and the trapezoid part company.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
double[] scores = [0.1, 0.4, 0.35, 0.8];

double ap = AveragePrecision.Score(truth, scores);  // => 0.8333…
```

The trapezoid over the same curve is `0.7916…`. And the same two samples over three labels, as a
matrix:

```csharp
using Lodestar.Metrics;

bool[] relevant = [true, false, false, false, false, true];
double[] labelScores = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

double macro = AveragePrecision.Score(relevant, labelScores, labelCount: 3);  // => 0.3333…
```

Two of the three labels score `0.5` and the third is carried by no sample and scores `0`, so the
plain mean is `0.3333…`. `Averaging.Weighted` gives `0.5` on the same input, because the empty
column carries no positive weight and drops out of the average.

**Remarks** — three weight vectors take the answer outside what either side defines, and all six
numbers below are measured rather than reasoned about.

| input | scikit-learn 1.9.0 | here |
| --- | --- | --- |
| a weight vector summing to zero, `[1, 1, 1, -3]` | `0.5`, through a numpy "divide by zero encountered" warning | `-0` |
| every weight `0` | `ValueError`, "Sample weights must contain at least one non-zero number." | `0` |
| a `posLabel` no sample carries | `ValueError`, "pos_label=7 is not a valid label." | `0` |

The first two are the same defect seen twice: the running total of weight reaches zero, and the
precision at that threshold is a division the reference performs under a warning and this one
guards. The third is the package's standing position rather than an accident — `posLabel` is a
parameter here where scikit-learn infers it, so a label no sample carries is the no-positive case
and answers `0`, exactly as [`TopKAccuracy.Score`](topkaccuracy-score.md) accepts a class no sample
carries. A negative weight that leaves the total positive is accepted by both and agrees:
`[-1, 2, 1, 1]` scores `0.75` on either side.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`AveragePrecision.PerLabel`](averageprecision-perlabel.md),
[`RocAuc.Score`](../classification/rocauc-score.md),
[`LabelRankingAveragePrecision.Score`](labelrankingaverageprecision-score.md), the
[Python equivalence table](../../../equivalence.md).
