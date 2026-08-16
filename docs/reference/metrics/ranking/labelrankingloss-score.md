# LabelRankingLoss.Score

The mean fraction of wrongly ordered label pairs, in `[0, 1]` — `sklearn.metrics.label_ranking_loss`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` says whether each label is relevant and `yScore` holds the scores the
ranking was made from, both row-major: one row per sample, `labelCount` values each, and the same
length. `labelCount` is how many labels a row holds, and it must be at least `2` here.
`sampleWeight` is one weight per sample, or empty — the default — for an unweighted mean.

**Returns** — `double` in `[0, 1]`, the mean over the samples of the fraction of (relevant,
irrelevant) label pairs the ranking ordered wrongly. `0` is a sample whose every relevant label
outscores every irrelevant one. A sample where all labels or no labels are relevant has no such pair
and contributes `0` — a perfect score for a row that said nothing, which is what the reference does
and the reason this metric is read beside a count of relevant labels.

**Exceptions** — `ArgumentException` in six shapes, all of them scikit-learn's or numpy's refusals:
`labelCount` below `1`; `labelCount` exactly `1`, with "binary format is not supported" — where
[`LabelRankingAveragePrecision.Score`](labelrankingaverageprecision-score.md) accepts a single label
column and returns `1`; `yTrue` and `yScore` disagreeing in length; `yTrue` empty, or not a whole
number of rows of `labelCount`; a non-empty `sampleWeight` whose length is not the row count; and a
`sampleWeight` summing to zero, with numpy's "Weights sum to zero, can't be normalized."

**Example** — two samples over three labels. The first has one wrongly ordered pair out of two, the
second has both wrong.

```csharp
using Lodestar.Metrics;

bool[] truth = [true, false, false, false, false, true];
double[] scores = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

double loss = LabelRankingLoss.Score(truth, scores, labelCount: 3);  // => 0.75
```

A tie is not a half-win. One sample, two relevant labels and one irrelevant, every score equal:

```csharp
using Lodestar.Metrics;

double tied = LabelRankingLoss.Score([true, true, false], [0.5, 0.5, 0.5], labelCount: 3);  // => 1
```

Both pairs count as errors, so the row scores `1`. Drop the irrelevant label's score below the
others and the same row scores `0`.

**Remarks** — the comparison is `score[relevant] <= score[irrelevant]`, which is where the tie rule
comes from: the reference ranks a tied group at its worst position, so an irrelevant label that
merely matches a relevant one is counted as beating it. The order *within* a tie is not observed at
all — pairs are counted, never sorted — so no permutation of equally scored labels can change the
answer.

The denominator is per sample: `relevant × irrelevant` pairs, so rows with different numbers of
relevant labels are each normalized before they are averaged. A negative weight is accepted, as
`numpy.average` accepts one, and takes the result outside `[0, 1]` — measured, weights `[-1, 2]` on
the worked example give `2.0`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CoverageError.Score`](coverageerror-score.md),
[`LabelRankingAveragePrecision.Score`](labelrankingaverageprecision-score.md), the
[Python equivalence table](../../../equivalence.md).
