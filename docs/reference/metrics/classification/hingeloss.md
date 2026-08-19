# HingeLoss

The only metric here that reads a **decision function** — not a label, and not a probability, but the
signed distance from a boundary that a support vector machine or a linear model produces.

A sample costs nothing once it is on the right side **by a margin of 1**, and its cost rises linearly
from there. That margin is the whole point: a prediction that is right but barely is still charged,
where [`ZeroOneLoss`](zerooneloss.md) counts it as free. This is the loss an SVM actually minimises,
which is why it is the one to report when tuning one.

## Only the sign of the decision matters

The label is compared against the decision's sign, so relabelling the two classes cannot move the
number — `-1`/`1`, `0`/`1` and `7`/`3` all give the same loss on the same decisions, and a test
asserts it. `posLabel` says which label is on the positive side; scikit-learn infers the two classes
instead.

**On a truth carrying only one class the two disagree.** scikit-learn maps every label to `-1`
through a `LabelBinarizer` that has nothing to contrast, and returns a number computed against the
wrong side — measured `1.65` where the margins say `0.35`. Here `posLabel` is a parameter, so that
input is ordinary and answers `0.35`. The divergence is the reference's degenerate case, not a
choice made here.

## The multiclass form is a different margin

[`MultiClass`](hingeloss-multiclass.md) takes one decision per class and charges on the true class's
decision less the **best of the others** — Crammer and Singer's multiclass hinge, which is what the
reference computes. A sample costs nothing once its own class wins by 1.

## Members

| Member | What it does |
| --- | --- |
| [`HingeLoss.Score`](hingeloss-score.md) | The binary case, from one decision per sample. |
| [`HingeLoss.MultiClass`](hingeloss-multiclass.md) | The same over one decision per class. |
