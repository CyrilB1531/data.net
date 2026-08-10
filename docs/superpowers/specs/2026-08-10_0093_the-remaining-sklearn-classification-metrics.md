# The remaining classification metrics — design

**Issue:** [#93](https://github.com/CyrilB1531/data.net/issues/93) ·
**Date:** 2026-08-10 · **Package:** `DataNet.Metrics`

## Problem

Issue #61 shipped eight classification metrics and left four things out as
out of scope: `balanced_accuracy_score`, `matthews_corrcoef`,
`cohen_kappa_score`, and `normalize=` on `confusion_matrix`. This closes that lot.

## Why this lot before the regression one

Issue #92 (regression metrics) was specced first and then deferred behind this,
for a reason that is about release timing rather than preference.

`normalize=` changes `ConfusionMatrix`, a type that is **already public**. ADR 0016
took `sampleWeight` from day one on exactly that argument: "retrofitting it later
would change the public type of every cell … a breaking change across the entire
package, to add a parameter that was always going to be needed." `normalize` is
the same shape — it changes what the cells *mean* — and it is free before
`DataNet.Metrics` publishes `0.1.0` and awkward after.

The other three are also the smallest remaining work in the package: each is a
pure read off the confusion matrix, which is the use ADR 0016 made that type
public for and which nothing has exercised yet.

## Public API

```csharp
BalancedAccuracy.Score(yTrue, yPred, adjusted = false, sampleWeight = default)
BalancedAccuracy.Score(cm, adjusted = false)

MatthewsCorrelation.Score(yTrue, yPred, sampleWeight = default, zeroDivision = ZeroDivision.Zero)
MatthewsCorrelation.Score(cm, zeroDivision = ZeroDivision.Zero)

CohenKappa.Score(yTrue, yPred, weighting = KappaWeighting.None,
                 labels = default, sampleWeight = default, zeroDivision = ZeroDivision.NaN)
CohenKappa.Score(cm, weighting = KappaWeighting.None, zeroDivision = ZeroDivision.NaN)

double[,] ConfusionMatrix.ToArray(Normalization normalization)
```

Two entry points per metric — one taking labels, one re-reading a computed matrix
— matching `Accuracy`, `Precision`, `Recall` and `F1`.

`MatthewsCorrelation` rather than `Mcc`: the package writes words out except where
the abbreviation *is* the name (`F1`, `FBeta`, `RocAuc`), and
`MatthewsCorrelation.Score(cm)` reads without knowing the initialism.

Five new public types: three metrics plus
`Normalization { None, True, Pred, All }` and
`KappaWeighting { None, Linear, Quadratic }`.

### `normalize=` is a projection, never a state

`ToArray()` gains an overload; the no-argument form stays equivalent to
`Normalization.None`. **A `ConfusionMatrix` is never normalized**, and never
remembers having been.

This is the design decision of the issue. scikit-learn puts `normalize=` on
`confusion_matrix` because its other metrics take `y_true`/`y_pred` and cannot be
handed a matrix. This package's metrics *can*: `Accuracy.Score(cm)` computes
`diagonal / TotalWeight`, and `Prf` reads the internal `Cells` and `Stride`
directly. Give any of them a `'true'`-normalized matrix and they return a number
that is neither accuracy nor anything else — **silently**, since the cells are
still valid `double`s.

So a `normalize` parameter on `Compute` would make every existing
`ConfusionMatrix`-reading method wrong for a legal input. A projection makes the
misuse impossible to express instead of merely documented, which is the same move
ADR 0016 made when `average=None` became `PerClass`. It would also push the
private constructor from nine parameters to ten, past the S107 threshold that
already carries a documented suppression there.

Verified sums for the four modes on a 7-sample, 3-class fixture: `None` → 7,
`'true'` → 3, `'pred'` → 3, `'all'` → 1.

## The degenerate cases

Measured against scikit-learn 1.9.0 in this repository's `.venv-oracles`.

| Case | scikit-learn | This design |
| --- | --- | --- |
| MCC, a single label throughout | `0.0`, with a warning | `zeroDivision`, default `Zero` |
| Cohen's kappa, a single label throughout | `nan`, with a warning | `zeroDivision`, default `NaN` |
| balanced accuracy, a predicted class absent from the truth | `0.75`, with a warning | reproduce; **no knob** |
| `KappaWeighting` outside the three values | `InvalidParameterError` | not expressible — it is an enum |

The third row earns its own justification, because it looks like the other two and
is not. Nothing is undefined there: no denominator is zero, scikit-learn returns a
perfectly defined number and merely notes that the labels are unusual. Attaching a
`ZeroDivision` to it would invent a choice where none exists. The case is
documented and tested, not parameterised.

scikit-learn gives kappa its own knob for this — the full signature is
`cohen_kappa_score(y1, y2, *, labels=None, weights=None, sample_weight=None,
replace_undefined_by=nan)`. `ZeroDivision` covers the same ground and is what the
package already teaches, so `replace_undefined_by` maps onto it rather than
arriving as a second mechanism. MCC has no such parameter in scikit-learn — its
`0.0` is hard-coded — so offering `ZeroDivision` there is an extension beyond
parity, of exactly the kind the package already documents: `ZeroDivision.Throw`
exists because an `UndefinedMetricWarning` is easy to miss in a log.

### The same enum will have four different defaults

`Zero` for precision, recall and F1; `Zero` for MCC; `NaN` for kappa; and `NaN`
for R² when issue #92 lands. Each matches scikit-learn for its own metric, and
none matches its neighbours.

This is recorded as a decision in the ADR rather than left for a reader to trip
over, and every affected `<param>` states its own default.

### Naming: `weighting`, not `weights`

scikit-learn's `weights` parameter would sit beside `sampleWeight` in the same
signature, two unrelated senses of the same word. It becomes `weighting`, carried
by `KappaWeighting`. Kappa's symmetry between two raters — scikit-learn names its
inputs `y1`/`y2`, not `y_true`/`y_pred` — is stated in the XML documentation rather
than in the parameter names: it is a property of the metric, not a constraint on
the API, and the package's consistency wins here. The `Score(cm)` overload reads a
matrix built from a `yTrue` and a `yPred` anyway, so the orientation returns
through the back door regardless.

## Weighted kappa depends on the label order

Measured on truth `[0,0,1,1,2,2,2]` against prediction `[0,1,1,1,2,0,2]`:

| Label order | unweighted | `Linear` | `Quadratic` |
| --- | ---: | ---: | ---: |
| `[0,1,2]` | 0.5757575758 | 0.5116279070 | 0.4444444444 |
| `[2,1,0]` | 0.5757575758 | 0.5116279070 | 0.4444444444 |
| `[1,0,2]` | 0.5757575758 | **0.6956521739** | **0.8055555556** |

The weighting uses the distance between class *indices*, so a full reversal
preserves it and any other permutation does not. Consequences:

- The `Score(cm)` overload is load-bearing on `cm.Labels` order. `ConfusionMatrix`
  keeps the caller's order when labels were given explicitly and sorts otherwise —
  scikit-learn's own rule — so the number matches, **provided the XML
  documentation says the weighted result depends on that order.** Without it, a
  caller who passes an unsorted explicit label list gets a different answer than
  scikit-learn with no clue why.
- Two invariants the oracle does not provide: unweighted kappa is invariant under
  **any** permutation of the labels; weighted kappa is invariant under **reversal
  only**. Both are tested.

## Oracle validation

The three metrics extend `tests/oracles/classification_metrics.json` rather than
opening a new corpus — they are classification metrics, and `MetricsCorpus` and
its `1e-9` tolerance are reused unchanged. `tools/generate_oracles.py` extends
`generate_classification_metrics`.

Cases cover: the unweighted and weighted (`sample_weight`) fixtures the corpus
already carries; `adjusted` both ways; all three `KappaWeighting` values; an
explicit unsorted `labels` list, which is the only way to exercise the label-order
dependence through the oracle; the four `Normalization` modes as normalized
matrices; and one case per degenerate row above.

### Non-finite values in a corpus, for the first time

Kappa's degenerate value is `nan`, and **no committed oracle carries a non-finite
number today** — the existing `NaN` assertions are made directly in C#. Python's
`json.dump` defaults to `allow_nan=True` and would emit a bare `NaN`, which is
invalid JSON that `System.Text.Json` refuses.

So non-finite values are encoded as the string `"NaN"` and decoded by the loader,
and `allow_nan=False` is passed to `json.dump` so that a non-finite which was not
deliberately encoded fails generation loudly rather than producing a file CI
discovers later.

Issue #92 needs the identical plumbing for R²'s `nan` and `-inf`. **Whichever lot
lands first pays for it; the other inherits it.** This one lands first, so it pays,
and #92's spec should be updated to consume rather than rebuild it.

## Tests beyond the oracle

- The two kappa permutation invariants above.
- `BalancedAccuracy.Score(cm)` equals the mean of `Recall.PerClass(cm)` **over the
  classes with non-zero true weight** — not over all of them. This distinction is
  the whole of balanced accuracy's degenerate case and it is easy to get wrong:
  for truth `[0,0,1]` against prediction `[0,2,1]`, scikit-learn returns `0.75`,
  the mean of `0.5` and `1.0`, having **excluded** class 2, which has no true
  samples. `Recall.PerClass` with its own default of `ZeroDivision.Zero` would
  report `0.0` for that class and the mean of all three would be `0.5`.

  So the implementation is `Recall.PerClass(cm, ZeroDivision.NaN)` reduced by a
  mean that skips the `NaN`s — scikit-learn's `nanmean` — and the test asserts the
  identity on that form, with the `[0,0,1]`/`[0,2,1]` fixture as the case that
  distinguishes it from the naive one. A test using only fixtures where every class
  has a true sample would pass against both implementations and prove nothing.
- `ToArray(Normalization.All)` sums to 1; `'true'` and `'pred'` each sum to the
  class count; `ToArray()` equals `ToArray(Normalization.None)`.
- **A normalized matrix is not a `ConfusionMatrix`**: there is no API through which
  `Accuracy.Score` can receive one. This is asserted by the absence of a
  `Normalization` parameter on `Compute`, so the test is a compile-time property
  rather than a run-time one — stated in the ADR instead of faked as a test.
- Refusals: a `Normalization` or `KappaWeighting` cast from an out-of-range integer
  throws `ArgumentOutOfRangeException` naming the parameter and the value — C# does
  not stop `(Normalization)99`, so the switch must have a default arm that throws
  rather than falling through to a mode; plus the input checks `Inputs.Validate`
  already makes.
- The netstandard2.0 mirror replays all of it.

## Benchmarks

`bench/python/bench_metrics.py` and
`bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs` each gain the three
metrics over the existing corpus shapes. Issue #61 established processor time
against scikit-learn as this package's merge gate — 29 operations, every one at or
above 1× — and a lot that skips it ships three metrics with no measured claim.

## Plumbing that would fail the build if forgotten

- `samples/DataNet.Sample/PackagingGate.cs` requires a **member reference** to the
  three metric types and only a **type reference** to the two enums. That is not an
  assumption: the gate branches on it at `PackagingGate.cs:108` —
  `type.IsEnum ? typeRefs.Contains(name) : memberRefParents.Contains(name)` —
  because an enum member is a compile-time constant and emits no member reference.
  Naming `KappaWeighting.Linear` and `Normalization.All` in the sample is therefore
  enough for those two, while each metric needs a real call.
- Four rows in `docs/equivalence.md`, naming the divergences: `weighting` renamed
  from `weights`, `replace_undefined_by` mapped onto `ZeroDivision`, MCC's
  `ZeroDivision` being an extension beyond parity, and `normalize=` being a
  projection rather than a parameter.
- The ADR is **`0019`** — `0018` is issue #86's. Check `ls docs/decisions/` before
  writing; another branch has won that race once already. It records: `normalize`
  as a projection, the four `ZeroDivision` defaults, `weighting`'s rename, and
  MCC's knob as a deliberate extension.
- The CHANGELOG heading depends on whether `0.1.0` ships first. Read
  `Version.props` and the published feed at implementation time.

## Out of scope

Regression metrics — issue #92, specced and waiting. A metrics guide under
`docs/guides/`: there is none today, and writing one here would mean documenting
eleven classification metrics, which is wider than this lot; it gets its own issue.
