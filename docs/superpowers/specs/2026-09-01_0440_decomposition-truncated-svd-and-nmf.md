# 0440 lot 3 — `Lodestar.Decomposition`: TruncatedSVD and NMF, and the `Abstractions` package they force

**Issue:** [#440](https://github.com/CyrilB1531/lodestar/issues/440) ·
**Status:** accepted · **Date:** 2026-09-01

## Why this lot and not another

Phase 0 ([#437](https://github.com/CyrilB1531/lodestar/issues/437)) re-ran Phase 2's surveys against
NuGet and found three of its five "confirmed void" claims false. Lots 1, 2 and 4 may not open until
their gaps are restated on what `Atulin.MinHash`, `Yake.NET`, `ElBruno.BM25` and `LuceneSharp.Core`
do *not* do, with measurements. **Lot 3 is the one Phase 0 cleared**, on V4's restated gap, and this
spec opens it.

## The gap, as V4 restated it

The original claim — that ML.NET's `ProjectToPrincipalComponents` densifies to centre — was **wrong**
and [ADR 0059](../../decisions/0059-phase-0-verifications-two-confirmed-voids-do-not-survive-nuget.md)
says so: it centres after the projection, over a `VBuffer`. What survives is a different list, and it
is about what is computed rather than how:

- It computes **centred PCA**, not the **uncentred LSA** a term-document matrix wants. Centring a
  sparse matrix is what destroys the sparsity that made it worth storing.
- A **fixed rank of 20**, not a parameter.
- **No explained variance**, so nothing tells a caller how much of the matrix the rank kept.
- **No NMF at all**, so no parts-based decomposition and no topic model outside LDA's 512-token
  truncation.
- `IDataView` coupling, the same wall `Lodestar.Metrics` exists to avoid.

There is **no SVD to write**: randomized SVD never factorizes the sparse matrix. It multiplies it by
a thin dense block and factorizes *that*, which is where the dense kernels below come in.

## Scope

Two public algorithms, in one new package:

- **`TruncatedSvd`** — `sklearn.decomposition.TruncatedSVD` at `algorithm="randomized"`, which is
  `sklearn.utils.extmath.randomized_svd`. Fit, transform, components, singular values, explained
  variance and its ratio. Explained variance is scikit-learn's: the per-component variance of the
  **transformed** data, and the ratio divides it by the total variance of the input's columns — not
  by the sum of the kept components, which is what makes the ratios sum to less than one and is the
  number a caller reads to decide the rank. *Transformed* means `X · componentsᵀ`, not `U · Σ`:
  `_truncated_svd.py:257-262` takes the first for `algorithm="randomized"`, under the comment
  "X @ V is not the same as U @ Sigma", and the two disagree in the last bits. An earlier draft of
  this line said `U · Σ`; the implementation measured it.
  `randomized_svd`'s `transpose="auto"` is **not offered**: it swaps the two products when there are
  fewer rows than columns, which a term-document matrix routinely has, and a flag that silently
  changes which factorization runs is a parity claim with two shapes. `transpose=False` is what the
  corpus freezes and what ships.
- **`Nmf`** — `sklearn.decomposition.NMF` at `solver="mu"`, both `beta_loss="frobenius"` and
  `"kullback-leibler"`, with the NNDSVD initialisation family.

Out of scope for 0.1.0, named so the absence is a decision rather than an oversight: persistence of a
fitted model (`Lodestar.Text`'s artifact format is not reopened here), `solver="cd"`, the
`sparse_encode`/dictionary-learning family, and PCA proper — centring is the thing this deliberately
does not do.

## What probing scikit-learn 1.9.0 settled

Measured before any C# was written, because a randomized algorithm invites the assumption that only
its *subspace* can be compared.

**The algorithm is fully specified and bit-reproducible.** `_randomized_range_finder` draws
`Q = random_state.normal(size=(n_features, k + p))`, then runs `n_iter` power iterations
`Q ← normalizer(A Q)`, `Q ← normalizer(Aᵀ Q)`, and a final `Q, _ = qr(A Q)`. A step-by-step
reimplementation over the **same** `Ω`, with `power_iteration_normalizer="QR"` and
`transpose=False`, reproduces `randomized_svd`'s `U`, `s` and `Vᵀ` to **exactly 0.0** — not to a
tolerance, to the last bit, on a 40×25 sparse fixture at `k=4, p=6, n_iter=3`.

That is what makes this an ordinary parity target rather than a subspace comparison. `Ω` is an
**input**, so the corpus freezes it and both sides start from the same matrix.

**The `auto` normalizer is `none` below three power iterations and `LU` above.** `TruncatedSVD`'s
default is `n_iter=5`, so the shipped default path goes through LU. All three normalizers are
therefore implemented — see *Rejected* below.

**NNDSVD is not deterministic on its own.** `_initialize_nmf(X, k, init="nndsvd", random_state=s)`
calls `randomized_svd` internally, so its `W₀, H₀` depend on `s`; measured, seeds 7 and 99 give
different matrices. The NMF corpus therefore freezes `W₀, H₀` as **inputs** beside the final `W, H`,
which decouples the initialisation from the multiplicative-update loop and lets each fail on its own.

**`nndsvdar` cannot be reproduced, and that is why it is not shipped.** `_nmf.py:355-359` fills the
zeros left by NNDSVD with `abs(X.mean() * rng.standard_normal(...) / 100)` — numpy's *Gaussian*
stream, not a uniform one as an earlier draft of this spec said. It is the same MT19937 dependency
the *Rejected* section refuses below, with none of Ω's escape hatch: there is no "pass the noise in"
a caller would ever use. `nndsvd` and `nndsvda` are deterministic once Ω is fixed and both ship.

**NMF is reproducible once the initialisation is fixed.** `NMF(init="nndsvd", solver="mu",
beta_loss="kullback-leibler", tol=0.0, max_iter=60)` returns the identical `W` on two runs, and
reports `n_iter_ = 60` — `tol=0.0` disables the early stop, so the iteration count is an input rather
than a result.

## The dense kernels, written here

`CsrMatrix.Multiply` is SpMV; everything above needs SpMM. Five kernels are new — the two matrix
products, public on `CsrMatrix`, and three factorizations internal to `Lodestar.Decomposition`:

| kernel | shape | why it cannot be borrowed |
| --- | --- | --- |
| `A Ω` and `Aᵀ Q` | sparse × dense block | the only place the sparse matrix is read at all |
| thin Householder QR | `m × (k+p)`, tall and skinny | the normalizer, and the final range basis |
| one-sided Jacobi SVD | `(k+p) × n`, wide and short | the small factorization the whole method exists to reach |
| LU with partial pivoting | `m × (k+p)` | `power_iteration_normalizer="LU"`, sklearn's default at `n_iter ≥ 3` |

A block reaching the normalizer can be **wider than it is tall** when `k + p` exceeds the feature
count: `Aᵀ Q` is then `n × (k+p)` with `n < k+p`, and scipy's economic QR answers with a basis of
`min(n, k+p)` columns rather than refusing. The block narrows from there, and two corpus fixtures
reach it.

QR sign conventions do not leak into the answer. `Q → QD` for a diagonal sign matrix `D` leaves
`B = QᵀA` as `DB`, whose SVD returns `DŨ`, and `QD · DŨ = QŨ`. The final `U`, `s` and `Vᵀ` are
invariant, which is why a Householder QR that differs from LAPACK's column-for-column still lands on
the same answer. `svd_flip` then pins the remaining sign freedom exactly as scikit-learn does —
**on the right vectors, not the left.** `TruncatedSVD` does not take `randomized_svd`'s own flip:
`_truncated_svd.py:248-253` asks for `flip_sign=False` and then calls
`svd_flip(U, VT, u_based_decision=False)`, "to be consistent with PCA". The two conventions
disagree on four of the six corpus fixtures — measured, the stored `U` fails the left-based rule
there while `Vᵀ` satisfies the right-based one on all six — so the corpus asserts the estimator's
convention and the generator carries `assert np.array_equal(vt, svd.components_)` to keep proving
it. An earlier draft of this line described the bare function's flip, which is the wrong reference.

## Placement, and the package it forces

`TruncatedSvd` consumes `CsrMatrix`, which lives in `Lodestar.Text`.
[Decision 0069](../../decisions/0069-the-package-layout-as-built-and-what-enforces-it.md) recorded
`Lodestar.Abstractions` as **decided against** — and left the question of a second edge into
`Lodestar.Text` explicitly open "for whoever opens the first of those lots". This is that lot, and
the answer taken here is the one 0069 decided against, so it needs a decision record of its own that
amends it rather than an edit.

**Three packages, in this order**, because `src/` references published packages and never projects
(0069 rule 3), so each step needs the previous one on nuget.org:

| step | package | contents |
| --- | --- | --- |
| A | `Lodestar.Abstractions` 0.1.0 | `CsrMatrix` and `SparseNorm`, plus the two matrix products above. Nothing else: not `IDistance`, not the tokenizer interfaces — [#427](https://github.com/CyrilB1531/lodestar/issues/427) proposed those, and nothing here needs them. |
| B | `Lodestar.Text` 0.5.0 | drops its own `CsrMatrix`, takes a `PackageReference` on `Lodestar.Abstractions`, moves the seven reference pages and fills the `covered` map step A left empty; deletes the `sonar.cpd.exclusions` line step A needed |
| C | `Lodestar.Decomposition` 0.1.0 | `TruncatedSvd`, `Nmf`, one edge, to `Abstractions` |

Step B is a **breaking source change, accepted rather than softened**: the type becomes
`Lodestar.Abstractions.CsrMatrix`. A type-forward keeping `Lodestar.Text.Vectorization` as the
namespace was refused — it would leave `Lodestar.Abstractions` declaring, forever, a type in a
namespace naming a different package, to spare a `using` in a pre-1.0 library. The blast radius is
known: seven reference pages move, and about two dozen source files, the sample, the executed
snippets, three ADRs and the README name the type.

All three stay core tier: `net10.0;netstandard2.0`, no third-party dependency.

**Each step is its own plan.** A, B and C are separated by a release nobody can automate away, and
each produces working software on its own — A a package, B a package that consumes it, C the
algorithms. Writing one plan across the three would produce checkboxes nobody can tick until a
publish that is not theirs to do.

## Testing

Frozen corpora replayed at `1e-9`, generated from scikit-learn 1.9.0 and scipy, with `Ω` and `W₀, H₀`
frozen as inputs:

- **`decomposition_svd.json`** — `randomized_svd` over several shapes, ranks, oversamplings and
  power-iteration counts, one case per normalizer, plus a case where `k + p ≥ rank(A)` so the
  randomized answer is the exact one and can be checked against `scipy.linalg.svd`.
- **`decomposition_qr.json`** and **`decomposition_lu.json`** — the two factorizations on their own,
  against `scipy.linalg.qr(mode="economic")` and `scipy.linalg.lu(permute_l=True)`, so a failure in
  the composed algorithm has somewhere smaller to land. Compared on `Q R = A` and `P L U = A` rather
  than on the factors, which are only unique up to signs and pivots.
- **`decomposition_nmf.json`** — `_initialize_nmf` for the NNDSVD family, and the multiplicative
  updates for both beta losses at `tol=0` and a fixed `max_iter`.

The same suite runs against the `netstandard2.0` assembly, as every package's does.

## Benchmarks

Against ML.NET's `ProjectToPrincipalComponents`, which is the incumbent V4 restated rather than
removed — and the comparison is **not like-for-like**, so the section says what differs (uncentred
against centred, rank 20 fixed against a parameter) before it reports a ratio, the way
[#438](https://github.com/CyrilB1531/lodestar/issues/438)'s harness already does for
`FeaturizeText`. Agreement cannot be checked between two different decompositions, so what is checked
instead is that each side reconstructs its own input to its own stated error.

## Rejected

**Math.NET Numerics for the dense kernels.** Its last stable is 5.0.0, April 2022, with nothing but a
beta in four years (V3), and the sparse SVD request has been open since 2013. Taking it would move
this package out of the core tier and inherit exactly the staleness `docs/migration/README.md` marks
⛔ elsewhere. `CSparse` 4.4.1 is more alive but is a sparse direct solver; a tall-skinny dense QR and
a wide dense SVD are not its subject.

**A namespace inside `Lodestar.Text`.** Cheapest by far — no new package, no new edge, no
`Abstractions` question. Refused because `Lodestar.Text` would then own a decomposition family that a
reader arriving for `Levenshtein` carries anyway, and because the edge question 0069 left open does
not get easier by being deferred a second time.

**Only the `QR` normalizer.** It is the one the corpus proves bit-identical and needs no second
factorization family. Refused because `TruncatedSVD`'s own default is `n_iter=5`, which resolves to
`LU`: shipping only QR would mean the default call disagrees with scikit-learn's default call, which
is a parity gap in the place a reader meets first. LU with partial pivoting is the smallest of the
three kernels.

**Reproducing numpy's `RandomState.normal` from a seed.** It would make a seed portable between the
two ecosystems, at the cost of implementing MT19937 and numpy's cached-polar Gaussian. Refused: `Ω`
is an input, the API accepts one explicitly, and that is what the corpus passes. A seed drives this
package's own generator instead, and the reference page says a seed does not reproduce scikit-learn's
matrix.

**Deferring `ExplainedVariance`.** It is one of the five things V4's restated gap names, so it ships
in 0.1.0 or the gap is not closed.
