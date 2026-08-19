# HammingLoss

The fraction of **labels** predicted wrongly.

On single-label input that is one minus [`Accuracy`](accuracy.md), and it agrees with
[`ZeroOneLoss`](zerooneloss.md) exactly. On a label matrix the three part company, and the
difference is the reason both of these exist: this counts wrong **labels** where `ZeroOneLoss`
counts wrong **rows**, so one wrong label out of three costs a third of a sample here and a whole
sample there.

Measured on two samples over three labels with two labels wrong: `0.3333…` here and `1` there.

## Members

| Member | What it does |
| --- | --- |
| [`HammingLoss.Score`](hammingloss-score.md) | The share of labels that disagree, from labels or from a matrix. |
