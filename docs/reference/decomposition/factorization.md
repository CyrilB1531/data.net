# Truncated SVD — `Lodestar.Decomposition`

A term-document matrix has one column per word and one row per document, and almost every entry
is zero. This page factorizes it into a handful of dense components — latent semantic analysis —
**without centring it first**, which is the whole reason the operation exists: centring a sparse
matrix fills it in, and a corpus that fitted in memory as a `CsrMatrix` does not fit as a dense
one. That is also what separates this from PCA, which centres and is therefore a different
answer.

The factorization is randomized, not exact. A thin random block Ω probes the matrix's range, a
few power iterations sharpen it, and the singular values fall out of a small dense problem whose
size is the rank you asked for rather than the size of the corpus. The cost is that two runs from
different Ω disagree in the last digits, which is why Ω is handled the way it is below.

**Ω is an input, not a seed.** [`TruncatedSvdOptions.RandomMatrix`](factorization/truncatedsvdoptions.md)
takes the block itself, so a run of this package and a run of scikit-learn can be compared entry
by entry instead of hoping two generators agree; `Seed` is there for when you only need
repeatability and draws from this package's own generator, which is not NumPy's. See
[ADR 0072](../../decisions/0072-omega-is-an-input-not-a-seed.md).

| Type | What it is |
| --- | --- |
| [`TruncatedSvd`](factorization/truncatedsvd.md) | A fitted factorization: its components, its singular values, and the projection of new rows onto them. |
| [`TruncatedSvdOptions`](factorization/truncatedsvdoptions.md) | Oversampling, power iterations, the normalizer, and Ω itself. |
| [`PowerIterationNormalizer`](factorization/poweriterationnormalizer.md) | What happens to the probe block between the two products of a power iteration. |
| [`NmfInitialization`](factorization/nmfinitialization.md) | Where a non-negative matrix factorization starts from — the two NNDSVD variants this package ships. |

The whole procedure is two calls: [`TruncatedSvd.Fit`](factorization/truncatedsvd-fit.md) on the
corpus, then [`TruncatedSvd.Transform`](factorization/truncatedsvd-transform.md) on whatever you
want projected — including the corpus itself.

> **Nothing here is a topic model.** The components are signed and orthogonal, so a "topic" may
> subtract a word as readily as add one. Non-negative matrix factorization is the decomposition
> that answers that question, and it is a different type.

**See also** — the [Python equivalence table](../../equivalence.md), which maps every member on
these pages to the scikit-learn call it matches.
