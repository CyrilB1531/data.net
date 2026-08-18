# AdjustedMutualInformation.Score

Mutual information between two labellings, corrected for chance.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference labelling and `labelsPred` the one being scored;
they must be the same length. The label *values* carry no meaning — only which samples share one.

**Returns** — `double`, `1` when each labelling determines the other and about `0` for two
independent ones. **It can be negative**: agreement worse than chance is a real outcome. The independent case
below lands a rounding step short of exactly `-0.5`, which is scikit-learn's value too.

**Exceptions** — `ArgumentException` when the two labellings disagree in length.

**Example** — a renaming, and two independent partitions scoring below zero.

```csharp
using Lodestar.Metrics;

double renamed = AdjustedMutualInformation.Score([0, 0, 1, 1], [2, 2, 0, 0]);  // => 1
double independent = AdjustedMutualInformation.Score([0, 0, 1, 1], [0, 1, 0, 1]);  // => -0.4999…
double alone = AdjustedMutualInformation.Score([0, 0, 1, 1], [0, 1, 2, 3]);
bool chanceLevel = Math.Abs(alone) < 1e-12;  // => True
```

**Remarks** — the last pair of lines is the one worth the page. `alone` is chance level rather
than exactly zero — it lands a few rounding steps away, which is why the example asks the question
with a tolerance instead of promising a literal `0`.
[`NormalizedMutualInformation.Score`](normalizedmutualinformation-score.md) gives `0.667` for that
same pair, because putting every sample in its own cluster genuinely does determine the truth. What
it does not do is beat chance at it, and this metric says so. **That is the reason to reach for
this one when the clusterings being compared have different numbers of clusters.**

An empty input and a single sample both score `1`, as they do for
[`AdjustedRand.Score`](adjustedrand-score.md) — and unlike
[`FowlkesMallows.Score`](fowlkesmallows-score.md), which scores `0` on both. The degenerate cases
are a fact per metric rather than a rule for the family.

The correction subtracts the mutual information the two cluster-size profiles would share by
chance, summed over the hypergeometric distribution of every cell the marginals allow. Every
factorial that sum needs is of an integer, so it is taken from an exact cumulative `log(k!)` table
rather than from a `gammaln` approximation — no series, and no error budget to argue.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`AdjustedMutualInformation`](adjustedmutualinformation.md),
[`NormalizedMutualInformation.Score`](normalizedmutualinformation-score.md),
[the clustering index](../clustering.md).
