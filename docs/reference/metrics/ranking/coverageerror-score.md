# CoverageError.Score

The mean rank of the worst-ranked relevant label — `sklearn.metrics.coverage_error`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` says whether each label is relevant and `yScore` holds the scores the
ranking was made from, both row-major: one row per sample, `labelCount` values each, and the same
length. `labelCount` is how many labels a row holds, and it must be at least `2` here.
`sampleWeight` is one weight per sample, or empty — the default — for an unweighted mean.

**Returns** — `double`, the mean over the samples of the rank of the worst-ranked relevant label,
weighted when weights are given. A row's own floor is the number of relevant labels it carries, so
the best attainable value is the mean of those counts rather than `1`. A row with no relevant label
contributes `0`, which can take the mean **below** `1` — that is the reference's answer, not a
degenerate one worth guarding against.

**Exceptions** — `ArgumentException` in six shapes, all of them scikit-learn's or numpy's refusals:
`labelCount` below `1`; `labelCount` exactly `1`, with "binary format is not supported" — where
[`LabelRankingAveragePrecision.Score`](labelrankingaverageprecision-score.md) accepts a single label
column and returns `1`; `yTrue` and `yScore` disagreeing in length; `yTrue` empty, or not a whole
number of rows of `labelCount`; a non-empty `sampleWeight` whose length is not the row count; and a
`sampleWeight` summing to zero, with numpy's "Weights sum to zero, can't be normalized."

**Example** — two samples over three labels, the first sample's relevant label ranked second and the
second sample's ranked last.

```csharp
using Lodestar.Metrics;

bool[] truth = [true, false, false, false, false, true];
double[] scores = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

double coverage = CoverageError.Score(truth, scores, labelCount: 3);  // => 2.5
```

Now the same call on two samples the first of which has nothing relevant in it at all:

```csharp
using Lodestar.Metrics;

bool[] sparse = [false, false, false, true, false, false];
double[] ranked = [0.7, 0.2, 0.1, 0.7, 0.2, 0.1];

double belowOne = CoverageError.Score(sparse, ranked, labelCount: 3);  // => 0.5
```

The empty row covers `0` labels and the other covers `1`, so the mean is `0.5` — below the `1` a
reader expects to be the floor. Treating the empty row as fully covered would give `2.0` and look
far more reasonable than it is.

**Remarks** — the rank is `rankdata(-y_score, "max")`: `1` is the best score, and every member of a
tied group takes the group's *worst* rank. A relevant label tied with two irrelevant ones therefore
reads as if it had lost to both, which is deliberate — it is also what
[`LabelRankingLoss.Score`](labelrankingloss-score.md) counts as an error. Nothing here observes the
order within a tie: the rank is computed by counting how many scores are at least as high, so no
ordering of equal scores exists to be wrong about.

A negative weight is accepted, as `numpy.average` accepts one, and takes the result outside the
range the paragraphs above describe — measured, weights `[-1, 2]` on the worked example give `5.0`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`LabelRankingLoss.Score`](labelrankingloss-score.md),
[`LabelRankingAveragePrecision.Score`](labelrankingaverageprecision-score.md), the
[Python equivalence table](../../../equivalence.md).
