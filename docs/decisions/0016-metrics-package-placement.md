# 0016 — Classification metrics ship as `DataNet.Metrics`, not inside `DataNet.Text`

**Status:** accepted · **Date:** 2026-08-06

## Context

[Issue #61](https://github.com/CyrilB1531/data.net/issues/61) asks for
scikit-learn-parity evaluation metrics: confusion matrix, accuracy, precision,
recall, F1, F-beta, `classification_report` and ROC-AUC. The migration
inventory had been naming the gap without filling it — its scikit-learn row read
"check the definitions (macro/micro averaging, handling of absent classes)",
which tells a reader there is a trap and leaves them in it.

Four choices had to be made before any of it could be written, and each one is
hard to reverse once the package is published.

## Decision

### A separate package, not a namespace in `DataNet.Text`

`DataNet.Text` is described, in its own README, as the library for **text**:
distances, vectorization, stemming. A confusion matrix is not textual. It takes
`int` labels and `double` weights and would work identically for a model that
never saw a string.

The alternative — `DataNet.Text.Metrics` — costs nothing today and everything
later. Moving a type out of a published package is a breaking change for every
consumer that referenced it, and the pressure to move it would only grow as
regression metrics, calibration and clustering scores arrive, none of which have
any business in a text library. The split is cheapest at the moment the package
has no users, which is now.

The cost is a fourth package to version, tag and publish. That cost was already
paid structurally: [`0012`](0012-per-package-versioning.md) made every package
version independently, and `DataNet.Metrics` creates no inter-package edge — no
DataNet package depends on it and it depends on no DataNet package, so it is
freer to release than `DataNet.Text` is.

### `ConfusionMatrix` is public, not an implementation detail

scikit-learn exposes `confusion_matrix` as a metric in its own right, so parity
alone settles most of it. The stronger reason is cost: precision, recall, F1 and
the report are all read off the same `O(samples)` pass. A caller who wants four
numbers and can only reach
[`Precision.Score(yTrue, yPred, …)`](../reference/metrics/classification.md#precisionscore)
pays that pass four times over.

Making the matrix public gives them the choice, and it is the choice the
benchmarks measure:
[`ClassificationReport.Compute(cm)`](../reference/metrics/classification.md#classificationreportcompute)
over a million samples costs what building the matrix costs, because everything
after it is `O(classes)`.

The type is therefore part of the supported surface: label order, the `Labels`
view, `TotalWeight`, the indexer and `ToArray()`. What stays internal is the
storage — a flat `double[]` with a stride — so the layout can change without
breaking anyone.

### `sampleWeight` from the start, and `double` counts as its price

Weighted counts force the matrix to hold `double`, not `int`. A confusion matrix
whose cells are `double` reads oddly — the number of samples in a cell is a
whole number — and every caller who prints one now formats a float.

It was taken anyway. `sample_weight` runs through the whole of scikit-learn's
metrics API, and a library that omits it is not at parity for any caller doing
class rebalancing or importance weighting. Retrofitting it later would change
the public type of every cell, every support figure and every `PerClass` array
— a breaking change across the entire package, to add a parameter that was
always going to be needed.

`ToArray()` returns `double[,]` for the same reason, and the sample prints the
cells with `F0` so the common unweighted case still reads as counts.

### `Averaging.None` became `PerClass`, a separate method

scikit-learn spells the unreduced case `average=None`, one more value of the
same parameter. Transcribing that into an enum gives `Averaging.None`, which is
wrong in C# on two counts: `None` is the conventional name for a *zero* flags
value, and — decisively — the return type is not the same. Every other averaging
mode yields one `double`; `None` yields one per class.

An enum member that silently changes the return type of its method cannot exist,
so it becomes a method:
[`Precision.PerClass(cm)`](../reference/metrics/classification.md#precisionperclass)
returns `double[]` in label order. The enum keeps only the four members that
genuinely reduce to a scalar — `Binary`, `Micro`, `Macro`, `Weighted` — and the
equivalence table records the rename against `average=None`.

`Averaging.Binary` keeps scikit-learn's default and its strictness: it is not an
average at all but a single class scored against the rest, and it throws on a
target with more than two classes rather than guess which class was meant.

## Consequences

- A fourth package in the pack loops, the release workflow, the nuspec
  dependency check and the sample. `samples/DataNet.Sample` gains
  `Lot5Metrics.cs`, which
  is not optional: `PackagingGate` fails the build when an exported type is
  unreachable from the sample, so the surface is checked rather than assumed.
- `UndefinedMetricException` is the one metrics type the gate cannot see. Its
  whole public surface is constructors and a consumer catches rather than
  constructs, which leaves a type reference and no member reference — the same
  shape as the enum carve-out the gate already documents. It is excluded with
  that reason rather than exercised artificially.
- Callers wanting several metrics are steered towards computing the matrix once.
  The overloads taking `yTrue`/`yPred` remain, because the one-metric case
  should not have to know what a confusion matrix is.
- The `double` cells are visible in the public API forever. The alternative was
  to break every consumer the first time someone passed a weight.
