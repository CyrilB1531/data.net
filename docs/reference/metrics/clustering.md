# Clustering metrics — `Lodestar.Metrics`

You clustered some samples and you have a reference partition to compare against — labels from a
human, from an earlier model, or from a dataset that came with them. Every type on this page scores
how well the two partitions agree, and none of them cares what the clusters are *called*: swapping
the names of two clusters changes nothing, which is exactly what separates these from the
classification metrics.

They disagree on what "agree" means, and the disagreement is the reason there are five.

- **Corrected for chance or not.** Split every sample into a cluster of its own and
  [`Homogeneity`](clustering/homogeneity.md) scores a perfect `1`, because every cluster does hold
  one class. [`AdjustedRand`](clustering/adjustedrand.md) scores `0` on the same input, because that
  is what random labelling achieves. When a clustering looks suspiciously good, this is the pair to
  read together.
- **Symmetric or not.** [`Homogeneity`](clustering/homogeneity.md) and
  [`Completeness`](clustering/completeness.md) are the same measurement with the two labellings
  exchanged, and they pull in opposite directions: splitting raises one and merging raises the
  other. [`VMeasure`](clustering/vmeasure.md) is their harmonic mean, for when you want one number.

**The degenerate cases answer surprisingly, and it is deliberate.** An empty input scores `1` on
every metric here — agreeing about nothing is agreeing — and so does a single sample. Two
independent partitions of four samples score `-0.5` on `AdjustedRand`, not `0`: the correction for
chance is a subtraction, and it can go below zero. Every one of those numbers is scikit-learn's,
measured against 1.9.0 and frozen in the oracle corpus rather than reasoned about.

| Type | What it measures |
| --- | --- |
| [`AdjustedRand`](clustering/adjustedrand.md) | How many pairs of samples the two partitions agree about, minus what chance would give. |
| [`AdjustedMutualInformation`](clustering/adjustedmutualinformation.md) | Shared information between the two labellings, minus what chance would give — the one to use across different cluster counts. |
| [`FowlkesMallows`](clustering/fowlkesmallows.md) | The geometric mean of pair precision and pair recall, uncorrected for chance. |
| [`Completeness`](clustering/completeness.md) | Whether every sample of one class landed in the same cluster. |
| [`Homogeneity`](clustering/homogeneity.md) | Whether each cluster holds samples of a single class. |
| [`Silhouette`](clustering/silhouette.md) | How well each sample sits in its own cluster rather than the nearest other one — no reference partition needed. |
| [`NormalizedMutualInformation`](clustering/normalizedmutualinformation.md) | How much knowing one labelling tells you about the other, scaled into `[0, 1]`. |
| [`VMeasure`](clustering/vmeasure.md) | Homogeneity and completeness as one number, their harmonic mean. |
