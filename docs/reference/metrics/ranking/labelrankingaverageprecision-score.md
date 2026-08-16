# LabelRankingAveragePrecision.Score

The mean, over relevant labels, of how much of the ranking above them is relevant —
`sklearn.metrics.label_ranking_average_precision_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` says whether each label is relevant and `yScore` holds the scores the
ranking was made from, both row-major: one row per sample, `labelCount` values each, and the same
length. `labelCount` is how many labels a row holds; unlike the other two metrics of this family, a
`labelCount` of `1` is accepted. `sampleWeight` is one weight per sample, or empty — the default —
for an unweighted mean.

**Returns** — `double` in `[0, 1]` for non-negative weights. `1` when every relevant label outranks
every irrelevant one in every sample, and `1` as well for a sample where all labels or no labels are
relevant — such a ranking carries no information, and the reference scores it perfect rather than
dropping it from the average.

The answer is `NaN` when `sampleWeight` sums to zero, where
[`CoverageError.Score`](coverageerror-score.md) and
[`LabelRankingLoss.Score`](labelrankingloss-score.md) throw on the same input. The reference divides
by the weight sum directly on this path instead of going through `numpy.average`, which is the only
one of the three that refuses a zero sum.

**Exceptions** — `ArgumentException` in four shapes, each of them a refusal the reference also
makes: `labelCount` below `1`; `yTrue` and `yScore` disagreeing in length; `yTrue` empty, or not a
whole number of rows of `labelCount`; and a non-empty `sampleWeight` whose length is not the row
count. A `labelCount` of exactly `1` is **not** refused here, and is refused by the other two.

**Example** — two samples over three labels. The first sample's relevant label ranks second of
three, the second sample's ranks last.

```csharp
using Lodestar.Metrics;

bool[] truth = [true, false, false, false, false, true];
double[] scores = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

double lrap = LabelRankingAveragePrecision.Score(truth, scores, labelCount: 3);  // => 0.4166…
```

Half of the first row's ranking above its relevant label is relevant and a third of the second row's
is, so the mean is `0.4166…`. And the single label column the other two metrics refuse:

```csharp
using Lodestar.Metrics;

double single = LabelRankingAveragePrecision.Score([true], [0.7], labelCount: 1);  // => 1
```

**Remarks** — the rank of a label is `rankdata(-y_score, "max")`: `1` is the best score, and every
member of a tied group takes the group's worst rank. The order within a tie is never observed —
ranks are computed by counting how many scores are at least as high — so no permutation of equally
scored labels can change the answer, at any width.

A negative weight is accepted, as `numpy.average` accepts one, and takes the result outside `[0, 1]`
— measured, weights `[-1, 2]` on the worked example give `-0.3333…`. Report this beside
[`CoverageError.Score`](coverageerror-score.md): a good average precision with a large coverage
means most rankings are clean and a few rows hide a relevant label at the bottom.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CoverageError.Score`](coverageerror-score.md),
[`LabelRankingLoss.Score`](labelrankingloss-score.md), the
[Python equivalence table](../../../equivalence.md).
