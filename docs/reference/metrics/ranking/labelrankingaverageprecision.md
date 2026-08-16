# LabelRankingAveragePrecision

The average-precision idea applied to one sample's labels. For each relevant label, ask what
fraction of the labels ranked at or above it are themselves relevant; average that over the relevant
labels, then over the samples. `1` means every relevant label sits above every irrelevant one, and
the number degrades gracefully as irrelevant labels work their way up.

Where [`CoverageError`](coverageerror.md) reports a position and
[`LabelRankingLoss`](labelrankingloss.md) reports a fraction of lost comparisons, this reports how
clean the top of each ranking is, and it is the one of the three that is scale-free and bounded
without knowing how many labels are relevant.

**It accepts a single label column and returns `1`, where the other two refuse it** with
`binary format is not supported`. Nothing here decides that: `label_ranking_average_precision_score`
validates its input differently from `coverage_error` and `label_ranking_loss`, and the divergence
is reproduced rather than smoothed — making the three agree would invent a difference from the
reference instead of copying one.

**A weight vector summing to zero returns `NaN` here and raises in the other two**, for the same
kind of reason: the reference divides by the weight sum directly on this path and calls
`numpy.average` on the other two, and only `numpy.average` refuses a zero sum.

A sample where every label or no label is relevant scores `1` — its ranking carries no information,
and the reference says as much in a comment of its own.

## Members

| Member | What it does |
| --- | --- |
| [`LabelRankingAveragePrecision.Score`](labelrankingaverageprecision-score.md) | The mean, over relevant labels, of how much of the ranking above them is relevant. |
