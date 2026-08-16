# 0172 — Clustering metrics: agreement between partitions, then silhouette

**Issue:** [#172](https://github.com/CyrilB1531/data.net/issues/172) ·
**Status:** proposed · **Date:** 2026-08-15

## Problem

`DataNet.Metrics` ships classification and regression at scikit-learn parity and nothing for
clustering. Measured by grep over `src/`: no silhouette, no Rand, no mutual information, no
homogeneity.

## Everything below was measured, not transcribed

`scikit-learn` 1.9.0 in `.venv-oracles`, run from a neutral directory on 2026-08-15.

| case | ARI | NMI | homogeneity / completeness / V |
| --- | --- | --- | --- |
| identical partitions | `1.0` | `1.0` | `1.0` / `1.0` / `1.0` |
| labels renamed | `1.0` | `1.0` | `1.0` / `1.0` / `1.0` |
| independent (`[0,0,1,1]` vs `[0,1,0,1]`) | `-0.5` | `0.0` | `0.0` / `0.0` / `0.0` |
| one cluster predicted | `0.0` | `0.0` | `0.0` / `1.0` / `0.0` |
| one cluster in both | `1.0` | `1.0` | `1.0` / `1.0` / `1.0` |
| every sample its own cluster | `0.0` | `0.667` | `1.0` / `0.5` / `0.667` |
| **empty input** | `1.0` | `1.0` | `1.0` / `1.0` / `1.0` |
| single sample | `1.0` | `1.0` | `1.0` / `1.0` / `1.0` |

The last two are the surprise, and the reason they are in this table rather than in a comment: an
empty label set is **perfect agreement**, not an error and not `NaN`. Nothing in
`DataNet.Metrics` behaves that way today, so it is the first thing a reader will disbelieve.

Silhouette, same session:

- from feature vectors with the euclidean metric and from a precomputed distance matrix, the same
  fixture gives the identical `double`: `0.9738594604105609`. The two paths are one computation.
- `silhouette_samples` returns one value per sample, and the score is their mean.
- the number of distinct labels must be in `[2, n_samples - 1]`; outside it, `ValueError:
  Number of labels is 1. Valid values are 2 to n_samples - 1 (inclusive)`.
- a cluster holding one sample contributes `0.0` for that sample, measured, and the other clusters
  score normally around it.

## Decisions

### D1 — two lots, agreement first

`AdjustedRand`, `NormalizedMutualInformation`, `Homogeneity`, `Completeness`, `VMeasure` take two
label arrays and return a `double`, which is the shape the 31 metrics already at parity have. They
are proven the way those are — a frozen corpus replayed at `1e-9` — and need no design.

`Silhouette` carries the whole input question, so it is its own lot and its own pull request.

### D2 — silhouette takes a precomputed matrix, or vectors with the euclidean distance

Two overloads, no metric zoo:

```csharp
double Silhouette.Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> distances, int sampleCount)
double Silhouette.Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
```

`metric='precomputed'` is the first; the euclidean default is the second. scikit-learn accepts about
twenty metric names, and each one admitted here is a parity claim to prove and keep — a caller who
wants cosine computes the matrix, which is what `DataNet.Embeddings` is for. Row-major spans with an
explicit count, as the regression metrics already take 2-D targets ([D of
0021](../../decisions/0021-multioutput-is-a-method-not-an-enum.md)); there is no 2-D overload
because a span cannot carry one.

### D3 — `Silhouette.PerSample` ships beside `Score`

The per-sample array is what makes silhouette a diagnostic rather than a number, and the scalar is
its mean, so implementing one implements both. A separate method rather than an enum member,
because the return type changes — the ruling of
[0016](../../decisions/0016-metrics-package-placement.md), applied again in
[0021](../../decisions/0021-multioutput-is-a-method-not-an-enum.md).

### D4 — the undefined cases are reproduced, including the ones that look wrong

Every value in the table above is what DataNet returns. Two carry an argument:

- **empty input scores `1.0`.** It is scikit-learn's answer and this package's promise is parity, so
  it is reproduced rather than improved. It goes in the member's `<remarks>` with the measurement,
  the way `BalancedAccuracy` cites its own edge.
- **silhouette refuses outside `[2, n_samples - 1]`.** `ArgumentException`, carrying scikit-learn's
  own sentence, as the regression lot already does for `check_array`'s messages.

### D5 — no `ZeroDivision` knob

The classification metrics take one because scikit-learn's `zero_division` exists. None of these six
has such a parameter, and every degenerate case above returns a defined number rather than dividing
by zero. Adding a knob with no reference behind it would be an extension to defend, not parity.

## What lands with the code

- a `docs/equivalence.md` row per function, in the same commit as the function;
- a member page per method under `docs/reference/metrics/clustering/`, with the type page and index
  the layout of [#189](https://github.com/CyrilB1531/data.net/issues/189) requires — `covered` gains
  the directory, so the gate enforces the pages from the first commit;
- oracle corpora generated by `tools/generate_oracles.py`, compared at `1e-9`.

## Risks

- **The empty-input answer will be read as a bug.** Mitigated by the table above being in the spec,
  the `<remarks>`, and the equivalence row — three places, because one is what a reader misses.
- **A precomputed matrix is `n²` doubles.** At 100 000 samples that is 80 GB, so the matrix overload
  is for the size at which a reader would compute one anyway; the euclidean overload is `O(n²)` in
  time but `O(n)` in memory, and that difference is the reason both exist.
