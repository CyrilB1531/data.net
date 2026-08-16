# CoverageError

How far down the ranked labels you have to read before you have seen every relevant one, averaged
over the samples. A sample whose two relevant labels are the two highest-scoring covers `2`; the
same sample with one of them scored last covers the whole row.

The floor is not `1` but the number of relevant labels the row holds — scikit-learn's own "The best
value is equal to the average number of labels in `y_true` per sample." A coverage of `2.5` is
perfect on samples carrying two or three relevant labels and poor on samples carrying one, so the
number means nothing without the label counts beside it.

**A sample with no relevant label contributes `0`, not the label count.** There is nothing to
cover, and the row is not dropped from the average either, so the mean can sit **below `1`** —
measured, two samples one of which is empty give `0.5`. Read a coverage under `1` as "some rows had
nothing to find", never as an impossibly good ranking.

Like [`LabelRankingLoss`](labelrankingloss.md) and unlike
[`LabelRankingAveragePrecision`](labelrankingaverageprecision.md), a single label column is refused
with scikit-learn's sentence, `binary format is not supported`. That the three do not agree about it
is a divergence inside the reference, reproduced rather than smoothed.

## Members

| Member | What it does |
| --- | --- |
| [`CoverageError.Score`](coverageerror-score.md) | The mean rank of the worst-ranked relevant label. |
