# ZeroOneLoss

The fraction of **samples** predicted wrongly — one minus [`Accuracy`](accuracy.md) on single-label
input, and something else entirely on a label matrix.

There, a row counts as wrong if **any** of its labels differs. One wrong label out of three costs a
whole sample, where [`HammingLoss`](hammingloss.md) charges a third of one. Measured on two samples
over three labels with two labels wrong, one in each row: `1` here and `0.3333…` there.

`normalize` follows [`Accuracy.Score`](accuracy-score.md)'s: pass `false` and the answer is the
weight of the wrong samples rather than their share — a **count** when every weight is 1.

## Members

| Member | What it does |
| --- | --- |
| [`ZeroOneLoss.Score`](zerooneloss-score.md) | The share of samples that disagree, from labels or from a matrix. |
