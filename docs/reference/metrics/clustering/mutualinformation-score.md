# MutualInformation.Score

The information two labellings share, in nats.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference labelling and `labelsPred` the one being scored,
one label per sample and the same length. The label *values* carry no meaning: only which samples
share one does.

**Returns** — `double` in **nats**, never negative and **not bounded above**.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty input
is not an error: it returns `0`, unlike scikit-learn — see the divergence below.

**Example** — every sample in its own cluster still shares one bit of information with a
two-cluster truth.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
int[] alone = [0, 1, 2, 3];

double shared = MutualInformation.Score(truth, alone);   // => 0.6931471805599452
double scaled = NormalizedMutualInformation.Score(truth, alone);   // => 0.6666666666666666
```

**Remarks** — `0.693…` is `ln 2`, exactly one bit — the unit is **nats**, natural logarithms, not
bits, because that is what scikit-learn uses. This is the raw form of
[`NormalizedMutualInformation.Score`](normalizedmutualinformation-score.md), which divides the
same quantity by the mean entropy to land in `[0, 1]`; that page's own remark has why `0.667` there
is not the same statement as `0.693` here.

**Unbounded above is the practical difference from every other metric in this namespace.** Two
scores are comparable only between labellings of the same data at the same sizes — splitting a
labelling into more clusters can only raise this number, never lower it, so it cannot rank
clusterings of different sizes fairly. Reach for
[`NormalizedMutualInformation.Score`](normalizedmutualinformation-score.md) or
[`AdjustedMutualInformation.Score`](adjustedmutualinformation-score.md) across datasets instead.

**One divergence from scikit-learn, deliberate.** `mutual_info_score([], [])` raises
`ValueError` in scikit-learn 1.9.0 — a `log(0)` inside an unguarded logarithm, not a documented
refusal. This method returns `0` there instead, matching every sibling clustering metric, which
all treat an empty input as a case rather than an error.
[Decision 0039](../../../decisions/0039-mutual-information-returns-zero-on-an-empty-input.md) has
the measurement and the reasoning. A single sample also returns `0` — unlike the six agreement
metrics this family started with, which score `1` there, because a single sample carries no
information to share rather than no disagreement to find.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`NormalizedMutualInformation.Score`](normalizedmutualinformation-score.md),
[`AdjustedMutualInformation.Score`](adjustedmutualinformation-score.md),
[the Python equivalence table](../../../equivalence.md).
