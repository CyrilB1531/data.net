# 0020 — `normalize=` is a projection, and `ZeroDivision` keeps a default per metric

**Status:** accepted · **Date:** 2026-08-10

## Context

[Issue #93](https://github.com/CyrilB1531/data.net/issues/93) closes the
classification gap left by [`0016`](0016-metrics-package-placement.md): balanced
accuracy, Matthews correlation, Cohen's kappa, and `confusion_matrix`'s
`normalize=`. Three of the four are one formula each read off a matrix this
package already builds, and none of them needed a decision.

The fourth did, and it dragged three more with it. scikit-learn's metrics take
labels; this package's take a `ConfusionMatrix` as well, because
[`0016`](0016-metrics-package-placement.md) made the matrix public so several
metrics could share one `O(samples)` pass. That single difference means a
parameter which is harmless in scikit-learn is not harmless here — and it decides
what a matrix built over a subset of the labels can be asked for.

The other two questions are naming ones that a reader will otherwise read as
carelessness: the same enum now defaults differently in different places, and one
scikit-learn keyword had to be renamed.

## Decision

### `normalize=` is a projection on the matrix, not a parameter on `Compute`

[`ConfusionMatrix.ToArray(Normalization)`](../reference/metrics/classification/confusionmatrix-toarray.md)
returns scaled cells. A `ConfusionMatrix` is never normalized and never
remembers having been.

The rejected alternative is scikit-learn's own signature —
[`ConfusionMatrix.Compute(…, normalize: Normalization.All)`](../reference/metrics/classification/confusionmatrix-compute.md)
— which is what a reader coming from `confusion_matrix(…, normalize="all")`
will look for first.
It cannot be offered.
[`Accuracy.Score(ConfusionMatrix)`](../reference/metrics/classification/accuracy-score.md)
divides the diagonal by the total weight; on a matrix whose cells are row
fractions that quotient is still a number between 0 and 1, still prints, and is
neither accuracy nor anything else. So do the balanced accuracy, Matthews and
kappa overloads added here, all of which read the same cells. There is no type
error, no exception, and nothing in the value to notice.

Making it safe would have meant carrying a `Normalization` on the matrix and
having every matrix-consuming metric refuse a normalized one at run time — a
flag on the matrix, a new exception on every matrix-consuming method, and a failure
mode that only appears once a caller has combined two features that each work.
A projection cannot be handed to
[`Accuracy.Score`](../reference/metrics/classification/accuracy-score.md) at all:
`double[,]` is not a `ConfusionMatrix`, so the compiler rejects the mistake
instead of the library reporting it. The cost is one departure from a
scikit-learn signature, recorded in [`equivalence.md`](../equivalence.md).

`ToArray(Normalization.None)` is kept as the identity, so the enum has a member
for each of scikit-learn's four values rather than three plus "call the other
overload".

### `Score(cm)` computes over the classes the matrix holds

A matrix built with `labels:` holds a subset of the classes, and the three
metrics added here — `BalancedAccuracy`, `MatthewsCorrelation` and `CohenKappa` —
read exactly the classes in it, never the samples it dropped. Every quantity they
use (cells, row sums, column sums, trace, total, and balanced accuracy's
per-class recall denominator) comes from `Internal.MatrixSums`, which sums the
`Size × Size` view only.

This is not true of the metrics that came before, and the difference is
deliberate rather than an inconsistency to tidy up. `Precision`, `Recall`, `F1`,
`FBeta` and `ClassificationReport` go through `Internal.Prf`, whose `Support` is
`ConfusionMatrix.TrueSum` and whose `PredictedSum` runs over all `Stride` rows —
both counted against every *observed* label, not only the requested ones. They
have to be: those are scikit-learn's `true_sum` and `pred_sum`, which
`precision_recall_fscore_support(…, labels=…)` computes from the samples
themselves, so a sample whose predicted label fell outside the request still
lands in a requested class's row denominator there. Matching that is the whole
point of `Prf.Support`'s own remarks. The three metrics here must not match it,
because scikit-learn computes *them* from `confusion_matrix(y_true, y_pred,
labels=…)`, which is `k × k` and has already dropped those samples. Two rules,
each faithful to the reference call it mirrors; on a matrix that dropped nothing
— which is every matrix built without explicit `labels`, since `Stride == Size`
there — the two coincide exactly.

This is a property of the matrix-consuming overload, not a claim about the
reference, and the three metrics differ in whether a reference value even exists.
`balanced_accuracy_score` and `matthews_corrcoef` take no `labels` argument at
all, so for them a restricted matrix has no scikit-learn counterpart to agree or
disagree with; the label-taking overloads added here pass `labels` through to
[`ConfusionMatrix.Compute`](../reference/metrics/classification/confusionmatrix-compute.md), which is the
only place it means anything. `cohen_kappa_score` **does** take `labels`, so
there a counterpart exists — and on the corpus fixture the tests pin,
restricting to `[1, 2]` gives `1.0` from both. That is stated as agreement on a
measured case, not as a capability gap: the frozen corpus passes no `labels=` to
any of the three generators, so every value in it covers the full label domain,
and the restricted case is pinned by
`CohenKappaTests.A_restricted_label_set_reads_over_the_matrix_it_holds` rather
than by an oracle row.

The rejected alternative was to have the matrix overloads reason about the
samples that fell outside the label set — averaging a dropped class in as zero
recall, say, so that `Score(cm)` matched what some scikit-learn call over the
full labels would return. That would have cost the matrix its meaning: the whole
point of the public matrix is that what it holds is what gets scored, and a
metric that silently reads samples the matrix does not contain cannot be
explained to a caller who printed it.

### The same `ZeroDivision` enum defaults differently in each metric

Four defaults, two distinct values, none of them chosen for consistency:

- `Zero` for precision, recall, F1, F-beta and the report — scikit-learn's
  `zero_division=0`.
- `Zero` for Matthews correlation, matching a `0.0` that scikit-learn hard-codes
  rather than exposes.
- `NaN` for Cohen's kappa, matching `replace_undefined_by=nan`.
- `NaN` for R², when [issue #92](https://github.com/CyrilB1531/data.net/issues/92)
  lands — already committed in its design, for the same reason.

Each is faithful to its own metric and none was picked to agree with its
neighbours. A reader who compares two signatures will see an inconsistency, which
is why it is written down here.

The rejected alternative — one default across the package, whichever it was —
would have made the library return a different number from scikit-learn for a
legal input, with no diagnostic, for whichever metrics lost the vote. That is the
one cost this package will not pay: the corpora exist so that every value can be
checked against the reference, and a default that diverges makes the *unchecked*
call the wrong one. Uniformity of signature is worth less than agreement of
value.

### `weights` became `weighting`

`cohen_kappa_score(…, weights="linear")` is
[`CohenKappa.Score(…, KappaWeighting.Linear)`](../reference/metrics/classification/cohenkappa-score.md).

The rejected alternative is the literal transcription, `weights`, which would
have sat in the same signature as `sampleWeight` while meaning something
unrelated: `weights` is a distance between two *classes*, `sampleWeight` a
multiplier on a *sample*. Both are optional and both would be reachable by name
in the same call. `weighting` costs a row in the equivalence table; `weights`
would have cost a caller a plausible, compiling, wrong argument.

### Matthews correlation gains a knob scikit-learn does not have

`matthews_corrcoef` returns `0.0` when the denominator collapses — which happens
whenever either side of the matrix holds a single label — and emits an
`UndefinedMetricWarning`. The value is hard-coded; there is no keyword.
[`MatthewsCorrelation.Score`](../reference/metrics/classification/matthewscorrelation-score.md)
takes `ZeroDivision`, defaulting to `Zero`, so the default call returns
scikit-learn's number.

This is an extension of a kind the package already documents rather than a new
idea: `ZeroDivision.Throw` exists at all because a Python warning has no useful
.NET equivalent and is easy to miss in a log. Refusing the parameter here — the
rejected alternative — would have left the one metric whose undefined case is
*most* likely to be a real modelling bug, a target with one class in it, as the
only one that cannot be made to say so.

The cost is that the signature no longer transcribes: a reader porting
`matthews_corrcoef(y_true, y_pred)` finds a second parameter with no counterpart
to map it to, and has to be told that the default reproduces the reference. It
also puts a fourth default on an enum that already defaults inconsistently across
the package (above), so the *set* of defaults is one entry harder to remember for
the sake of one metric's diagnostics. Both are paid in
[`equivalence.md`](../equivalence.md), which states the row as an extension
beyond parity rather than a divergence in value.

## Consequences

- Cohen's kappa's `nan` is the first non-finite value in any oracle in this
  repository, and JSON has no literal for it. It travels as the string `"NaN"`
  (with `"Infinity"` and `"-Infinity"` for the cases regression metrics will
  bring), and `OracleLoader.Number(JsonElement)` reads a string of those three
  shapes and throws on any other. The generator writes with `allow_nan=False`,
  so a non-finite value nobody encoded on purpose fails generation instead of
  producing a file the loader will reject later. Issue #92 needs the identical
  plumbing for R², and consumes this rather than rebuilding it.
- The matrix-consuming overloads are now the package's main surface for these
  three metrics, so `ConfusionMatrix`'s label order is load-bearing in a new way:
  `KappaWeighting.Linear` and `Quadratic` measure distance between label
  *positions*, so the weighted kappa depends on the order of `Labels`. A full
  reversal preserves every distance and returns the same value; any other
  permutation does not. Both invariants are pinned by tests.
- `Normalization` and `KappaWeighting` are enums whose members carry no member
  reference in IL, so `PackagingGate` can only see them as type references. They
  are exercised in `samples/DataNet.Sample/Lot5Metrics.cs` through the calls that
  take them, which is the carve-out the gate already documents for enums.
- Four rows in [`equivalence.md`](../equivalence.md), each naming its divergence
  rather than only its mapping. `normalize=` is the only one where a caller has
  to write something structurally different from the Python they are porting.
