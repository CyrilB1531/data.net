# Auc

The area under a curve you already have — the companion to the three curve types, and the one
member here that takes points rather than labels.

[`RocAuc.Score`](rocauc-score.md) computes the ROC area without ever exposing the curve, and is what
to reach for when the area is all you want. This is for a caller who is holding a curve: it
integrates whatever points it is given.

**The two agree, and a test says so.** The trapezoid over [`RocCurve`](roccurve.md)'s own output
equals `RocAuc.Score` on the same input, which is an invariant no oracle can state — it relates two
members of this package rather than one of them to Python — so it is asserted over every fixture of
the frozen corpus.

## It is the wrong reading of a precision-recall curve

Deliberately so. Over [`PrecisionRecallCurve`](precisionrecallcurve.md)'s points the trapezoid
interpolates between two thresholds as though the curve were straight there, and comes out
optimistic: `0.7916…` where [`AveragePrecision.Score`](../ranking/averageprecision-score.md) sums
the steps to `0.8333…`. Both are kept, both are documented, and a test asserts they disagree.

## Members

| Member | What it does |
| --- | --- |
| [`Auc.Trapezoid`](auc-trapezoid.md) | The trapezoidal area under the points given. |
