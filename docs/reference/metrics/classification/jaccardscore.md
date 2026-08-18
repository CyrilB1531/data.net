# JaccardScore

Intersection over union: of every sample that is in a class **or** was predicted into it, what share
is in both.

It is [`Precision`](precision.md)'s numerator over a larger denominator — precision divides the true
positives by what was predicted, [`Recall`](recall.md) divides them by what was true, and this
divides them by the two together. So it can never read above either, which is what makes it the
strictest of the three and a test asserts.

It takes the same four [`Averaging`](averaging.md) modes and the same
[`ZeroDivision`](zerodivision.md) as precision and recall, because it is the same shape with a
different ratio — the reason issue #211 called it the cheapest of its six.

**Labels only**, where [`Precision`](precision.md) and its two siblings also read a
[`ConfusionMatrix`](confusionmatrix.md) directly. Those overloads exist because
[`ClassificationReport`](classificationreport.md) reads them; nothing reads a Jaccard coefficient
from a report, and `jaccard_score` has no matrix form of its own.

## A class neither side carries

Nothing is in the union, so the ratio has no value. `ZeroDivision.Zero` — the default — answers `0`,
and `One` answers `1`; both are what `zero_division=0` and `zero_division=1` give.

**`NaN` and `Throw` have no counterpart here.** `jaccard_score` admits only `0`, `1` and `'warn'`,
and refuses `nan` outright with an `InvalidParameterError`. The two extra members are this package's,
and the equivalence table says so.

Reaching that case needs an explicit `labels` set: a class that occurs in neither input is not in the
sorted union the label set otherwise defaults to.

## Members

| Member | What it does |
| --- | --- |
| [`JaccardScore.Score`](jaccardscore-score.md) | The coefficient, reduced by an `Averaging` mode. |
| [`JaccardScore.PerClass`](jaccardscore-perclass.md) | One coefficient per class, in label order. |
