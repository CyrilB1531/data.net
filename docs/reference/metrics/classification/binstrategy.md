# BinStrategy

Where [`CalibrationCurve`](calibrationcurve.md) gets its bin edges.

<!-- docs-declaration -->

```csharp
public enum BinStrategy { Uniform, Quantile }
```

**Members** — `Uniform` cuts `[0, 1]` into equal widths whatever the data does, which is
scikit-learn's default. `Quantile` reads the edges off the probabilities themselves, its
`strategy='quantile'`.

**Example** — the same four probabilities, binned both ways.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 1, 0];
double[] probabilities = [0.1, 0.9, 0.8, 0.3];

CalibrationCurve byQuantile = CalibrationCurve.Compute(
    truth, probabilities, nBins: 4, strategy: BinStrategy.Quantile);

int quantile = byQuantile.ProbTrue.Count;   // => 4
```

**Remarks** — `Quantile` equalises **rank**, not count. Repeated probabilities collapse two edges
onto each other, and a bin between two equal edges can hold nothing at all — so the strategy meant
to balance the bins is also the one that empties them on tied input. Measured: six probabilities
that take only two distinct values, over four quantile bins, return **two** points.

Its edges come from the linear interpolation `np.percentile` computes, which is not the weighted
percentile [decision 0024](../../../decisions/0024-weighted-median-averages-within-scikit-learns-epsilon.md)
pinned for the medians. The two disagree in the third decimal, and the reference reaches for the
unweighted one here.

`Uniform` is the safer default for a reliability plot precisely because its x-axis does not move
with the data: two models are comparable bin by bin only if the bins are the same.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CalibrationCurve.Compute`](calibrationcurve-compute.md), the
[Python equivalence table](../../../equivalence.md).
