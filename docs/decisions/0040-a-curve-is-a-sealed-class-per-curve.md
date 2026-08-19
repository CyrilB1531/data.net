# 0040 — A curve is a sealed class per curve, not a record and not out-parameters

**Status:** accepted · **Date:** 2026-08-18

## Context

Every one of the 48 members `Lodestar.Metrics` shipped before this returns a scalar or a vector of
per-class scalars. [`RocCurve`](../reference/metrics/classification/roccurve.md),
[`PrecisionRecallCurve`](../reference/metrics/classification/precisionrecallcurve.md) and
[`DetCurve`](../reference/metrics/classification/detcurve.md) return a **curve**: three parallel
arrays of caller-unknown length, one of which is deliberately a different length from the other two.

That shape has no precedent here, and #212 was explicit that the return type is the decision the
lot turns on rather than the arithmetic — `Internal/BinaryRoc.cs` already walked the sorted
thresholds these three need.

### What was measured

On `y_true = [0, 0, 1, 1]`, `y_score = [0.1, 0.4, 0.35, 0.8]`, scikit-learn 1.9.0 returns:

| function | lengths |
| --- | --- |
| `roc_curve` | 5, 5, 5 |
| `precision_recall_curve` | 5, 5, **4** |
| `det_curve` | 3, 3, 3 |

The precision-recall curve's thresholds array is one shorter because the curve carries an endpoint
at recall `0` and precision `1` that no threshold produces.

## Options

**Three `out` parameters.** Closest to what the reference returns, and allocation-free at the call
site. Rejected: `out` on a public metric is unlike anything else in this package, the caller has to
name three locals before it can read one, and the length asymmetry becomes invisible — nothing in
`out double[] precision, out double[] recall, out double[] thresholds` says the third is shorter.

**One `Curve` type with three named constructors.** Fewer types. Rejected: the three axes are not
the same three quantities. `X` and `Y` would be false-positive and true-positive rate on one curve,
recall and precision on another, and false-positive and false-*negative* rate on the third. A
property name that means something different per factory is worse than three types.

**A `record`.** Rejected on evidence rather than taste. A positional record over arrays gives value
equality that compares references and a `with` copy that shares them — the defect issue #91 had to
fix in `TokenizationResult`. [`ClassificationReport`](../reference/metrics/classification/classificationreport.md)
is a sealed class for the same reason, and it is the closest thing this package already had to a
structured return.

**A sealed class per curve, with a static `Compute`.** Chosen.

## Decision

Each curve is a **sealed class** exposing `IReadOnlyList<double>` properties named for the quantity
they hold, built by a static `Compute` that takes the same `yTrue`, `yScore`, `posLabel` and
`sampleWeight` the rest of the binary metrics take.

Two consequences follow, and both are deliberate.

**`drop_intermediate`'s asymmetric defaults are reproduced, not normalised.** The reference defaults
it to `true` for `roc_curve` and `false` for the other two. A caller porting from Python gets the
same array lengths without reading a signature, and each page names the asymmetry rather than
leaving a reader to discover it. Measured on a ten-sample fixture, the flag changes the ROC curve
from 11 points to 5, the precision-recall curve from 11 to 8, and the DET curve from 11 to 8 —
by two different rules, which the pages also state.

**The thresholds array stays one shorter on the precision-recall curve.** Padding it to match would
invent a threshold for a point that has none, and a caller plotting thresholds against precision
would silently plot one pair too many.

## Consequences

[`Auc.Trapezoid`](../reference/metrics/classification/auc-trapezoid.md) ships with them, which #212
asked for: a caller holding a curve can integrate it, and an invariant test asserts that the
trapezoid over `RocCurve`'s own output equals [`RocAuc.Score`](../reference/metrics/classification/rocauc-score.md)
on the same input — something no oracle states, because it relates two of this package's members
rather than one of them to Python.

It also makes the contrast with [`AveragePrecision`](../reference/metrics/ranking/averageprecision.md)
checkable rather than merely stated: the trapezoid over the precision-recall curve reads
`0.7916666666666666` on the worked case where the step sum reads `0.8333333333333333`, and a test
holds the two apart.

`calibration_curve` (#286) inherits this shape rather than deciding it again, which is why #174 left
it out of the calibration lot.
