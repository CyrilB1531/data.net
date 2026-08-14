# 0031 — `NoSampleCorrect` mirrors NumPy's float64 upcast, not requested-label accuracy

**Status:** accepted · **Date:** 2026-08-14

## Context

`sklearn.metrics.classification_report` decides whether to print its support
column as plain integers or as floats by asking `multilabel_confusion_matrix`
whether *any* prediction anywhere in the dataset was correct —
`y_true[i] == y_pred[i]` for some `i` — checked over every observed label, not
only the labels a `labels=` argument requested. `multilabel_confusion_matrix`
is pure Python — `sklearn/metrics/_classification.py` — and normally fills
`tp_sum`/`pred_sum`/`true_sum` from `_bincount`, whose result is int64. When
nothing at all matched, `tp_bins` is empty and the branch its own comment
labels `# Pathological case`
(`sklearn/metrics/_classification.py:786`, scikit-learn 1.9.0) seeds all
three with `xp.zeros(...)` instead — a NumPy float64 array. NumPy then
upcasts the whole
assembled matrix to float64, so every support value downstream prints with a
decimal point — `15.0`, not `15` — even though nothing was weighted. The
frozen oracle's `all_wrong` fixture pins exactly this: its `reports` field
carries `"15.0"`/`"17.0"`/`"18.0"` supports rather than integers, for a
50-sample, unweighted, three-class target where nothing was ever right.

This is a different condition from `Accuracy` over the requested labels being
zero. A sample whose true or predicted label falls outside the requested
label set can still be the one correct prediction that makes the *dataset-wide*
check true while `Prf`'s per-class accuracy, computed only over the requested
labels, is nonetheless zero — the two questions ("was anything, anywhere,
right" vs. "was anything *requested* right") diverge exactly on that sample.

## Decision

`ConfusionMatrix.NoSampleCorrect` is computed once, during `Compute`, over
every observed label — not the requested subset — mirroring scikit-learn's
own pre-restriction check rather than being derived later from the public,
label-subset view. `ReportText` reads it to decide integer-vs-decimal support
formatting, the same branch scikit-learn's dtype upcast produces.

## Consequences

- `ConfusionMatrix.NoSampleCorrect`'s `<summary>` carries a pointer here
  instead of restating the NumPy upcast mechanism.
- Verified by `ReportTextTests.Support_stays_integral_when_a_correct_prediction_falls_outside_the_requested_labels`
  for the "dataset-wide correct, requested-label accuracy zero" divergence
  this record's Context section describes, and by
  `ReportTextTests.Renders_the_sklearn_table_character_for_character`
  (`tests/oracles/classification_metrics.json`, fixture `all_wrong`) for the
  decimal-support formatting itself.
