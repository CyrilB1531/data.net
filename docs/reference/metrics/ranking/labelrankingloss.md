# LabelRankingLoss

How often the ranking got a pair the wrong way round. Every (relevant, irrelevant) pair of labels in
a sample is one comparison the model either won or lost, and this is the fraction it lost, averaged
over the samples. `0` is perfect and `1` is every relevant label buried under every irrelevant one.

It is the pairwise counterpart of [`CoverageError`](coverageerror.md): coverage reads down to the
single worst-placed relevant label and reports a position, this counts every pair and reports a
fraction, so a row with one badly ranked label out of many hurts coverage far more than it hurts
this. Reported together, the two say whether a bad number comes from one outlier or from the whole
ordering.

**A tie counts as an error.** An irrelevant label sharing a relevant one's score is counted as
outranking it, so a row whose scores are all equal scores `1` rather than `0.5`. That is the
reference's arithmetic — the rank of a tied group is its worst — and the frozen corpus pins it.

A sample where every label or no label is relevant holds no pair to order and contributes `0`. A
single label column is refused with scikit-learn's sentence, `binary format is not supported`, as
[`CoverageError`](coverageerror.md) refuses it and
[`LabelRankingAveragePrecision`](labelrankingaverageprecision.md) does not.

## Members

| Member | What it does |
| --- | --- |
| [`LabelRankingLoss.Score`](labelrankingloss-score.md) | The mean fraction of wrongly ordered label pairs, in `[0, 1]`. |
