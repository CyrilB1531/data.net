# Latent structure in a sparse corpus — `Lodestar.Decomposition`

A term-document matrix is mostly zero and mostly redundant. Two thousand documents over fifty
thousand terms is a hundred million cells holding perhaps two million counts, and the columns are
not independent: *car* and *vehicle* carry nearly the same signal, and a query for one misses the
documents that used the other. **Truncated SVD** replaces those fifty thousand columns with twenty
or two hundred dense ones that span most of the same information — latent semantic analysis — so
similarity is measured between meanings rather than between spellings.

The word this package leaves out of that description is *centred*, and it is the whole reason the
operation exists as its own thing. Principal component analysis subtracts each column's mean
before factorizing, which turns every one of those hundred million zeros into a small non-zero
number: the matrix that fitted in memory as a `CsrMatrix` no longer fits at all. Truncated SVD
factorizes the matrix as it stands, so the sparsity that made a corpus storable survives the
decomposition. That is not an approximation of PCA — it is a different factorization, with a
first component that carries the mean direction instead of discarding it.

```bash
dotnet add package Lodestar.Decomposition
```

## From a `CountVectorizer` to a rank-20 projection

Three calls: vectorize the corpus, fit the factorization, project the rows.

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition;
using Lodestar.Text.Vectorization;

// One document per line -- anything that yields a sequence of strings will do.
string[] corpus = File.ReadAllLines("corpus.txt");

CountVectorizer vectorizer = new();
CsrMatrix counts = vectorizer.FitTransform(corpus);

// Twenty components, out of however many terms the corpus turned out to hold.
TruncatedSvd lsa = TruncatedSvd.Fit(counts, componentCount: 20);
double[] projected = lsa.Transform(counts);

// projected is row-major and 20 wide: document i's coordinates start at i * 20.
double firstCoordinate = projected[0];
```

[`TruncatedSvd.Fit`](../reference/decomposition/factorization/truncatedsvd-fit.md) reads the
matrix and never modifies it, and every property of what it returns is populated by the time it
does — there is no second call, and no unfitted state to guard against.
[`TruncatedSvd.Transform`](../reference/decomposition/factorization/truncatedsvd-transform.md)
then projects any matrix with the same number of columns, including the one that was fitted;
`Components` is `20 × FeatureCount`, and `SingularValues` holds the twenty singular values kept,
largest first.

Nothing is centred and nothing is scaled, here or in the fit, so a document twice as long
projects twice as far. If length is not what you want the geometry to encode, normalize the rows
of the matrix — [`CsrMatrix.NormalizeRows`](../reference/abstractions/sparse/csrmatrix-normalizerows.md)
does it in place — before fitting rather than after.

## Reading the explained-variance ratio

`ExplainedVarianceRatio` is the number that answers *is twenty enough?*, and it answers it as a
**sum**, not entry by entry.

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition;
using Lodestar.Text.Vectorization;

CsrMatrix counts = new CountVectorizer().FitTransform(File.ReadAllLines("corpus.txt"));

TruncatedSvd twenty = TruncatedSvd.Fit(counts, 20);
double coveredAtTwenty = twenty.ExplainedVarianceRatio.Sum();

TruncatedSvd fifty = TruncatedSvd.Fit(counts, 50);
double coveredAtFifty = fifty.ExplainedVarianceRatio.Sum();

// The rank to ship is the smallest one whose sum stops moving much.
double bought = coveredAtFifty - coveredAtTwenty;
```

Each entry is one component's share of the **input's total** column variance, so the entries sum
to less than one by construction — the denominator counts the variance the truncation threw away,
which is the only way the number can say anything about the rank. A sum of `0.41` means these
twenty components carry 41 % of what the corpus varies by; raising the rank raises the sum,
and the rank worth shipping is the one after which raising it stops buying much.

Two habits from PCA do not transfer. The entries are **not sorted**: an uncentred factorization's
leading component carries the mean direction, which is large in norm and small in variance, so it
is routinely not the largest share — that is arithmetic, not a bug. And the sum never reaches one
short of keeping every column, which a *truncated* SVD by definition does not do.

## Reproducing a scikit-learn run

The factorization is randomized: a thin block Ω probes the matrix's range, and two runs from two
different Ω agree on the subspace while disagreeing in the last digits of every number in it. So
comparing this package against `sklearn.decomposition.TruncatedSVD` means fixing Ω, and **fixing
Ω means passing it, not seeding it**.

```csharp
using System.Globalization;
using Lodestar.Decomposition;

// Ω exactly as NumPy drew it: ColumnCount × (20 + 10) values, row-major.
// np.savetxt("omega.txt", omega.ravel()) writes one value a line, which is what the
// parse below reads; np.savetxt on the two-dimensional block writes one row a line.
double[] omega = Array.ConvertAll(
    File.ReadAllLines("omega.txt"),
    line => double.Parse(line, CultureInfo.InvariantCulture));

TruncatedSvdOptions reproducible = new() { RandomMatrix = omega };
```

The `20 + 10` in that shape is the rank plus
[`TruncatedSvdOptions`](../reference/decomposition/factorization/truncatedsvdoptions.md)'s
`Oversampling`, which defaults to 10 — scikit-learn's `n_oversamples`, and the same 10. An Ω of
any other length is refused rather than quietly reshaped, so changing either number changes the
block you have to draw.

Hand `reproducible` to
[`TruncatedSvd.Fit`](../reference/decomposition/factorization/truncatedsvd-fit.md) and the
components come back equal to scikit-learn's entry by entry rather than close to them: over a
shared Ω the two implementations agree to **exactly 0.0**, which is what the frozen corpus in
`tests/oracles/decomposition_svd.json` asserts.

`Seed` answers a different question. It makes a run of *this* package repeatable by drawing Ω
from a generator this package owns, and that generator is not NumPy's — no seed is portable
between the two ecosystems, and one that looked portable would be the expensive mistake here.
[ADR 0072](../decisions/0072-omega-is-an-input-not-a-seed.md) records why Ω is an input, the
measurement that makes it affordable, and the two features refused along with the generator:
`transpose="auto"`, which would swap the products on a matrix with fewer rows than columns, and
the `nndsvdar` initialisation below.

## Non-negative factorization, and which loss

A truncated SVD's components are signed and orthogonal, so a "topic" may subtract a word as
readily as add one. **Non-negative matrix factorization** answers the other question: `X ≈ W H`,
with no negative number anywhere, so a row of `H` is a set of terms that occur together and a row
of `W` is one document's recipe of them.

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition;
using Lodestar.Text.Vectorization;

string[] reviews = File.ReadAllLines("reviews.txt");
CsrMatrix counts = new CountVectorizer().FitTransform(reviews);

// Counts are Poisson, so Kullback-Leibler is the loss that matches them.
Nmf topics = Nmf.Fit(counts, 20, new NmfOptions
{
    BetaLoss = NmfBetaLoss.KullbackLeibler,
    Initialization = NmfInitialization.NndSvda,
});

// Component c's weight on term t sits at c * FeatureCount + t.
double weightOfFirstTerm = topics.Components[0];
double documentMixture = topics.Weights[0];
```

**Which loss is a statement about the data, not a tuning knob.** `Frobenius` is the maximum
likelihood fit under Gaussian noise and is what a matrix of continuous measurements wants;
`KullbackLeibler` is the Poisson one and is what counts want — a term-document matrix included,
which is why [`Nmf.Fit`](../reference/decomposition/factorization/nmf-fit.md) offers it.
`ReconstructionError` is reported in whichever loss produced it, and a Frobenius distance and a
Kullback–Leibler divergence are not on one scale, so compare a loss against itself across ranks
and never across the enum.

**The initialisation decides the zeros, permanently.** Every update multiplies both factors by a
non-negative ratio, which is what keeps them non-negative with no projection — and what means a
zero can never come back. `NndSvd` keeps the zeros the non-negative double SVD produces, which is
the choice when a sparse, readable factorization is the point; `NndSvda` fills them with the
matrix's mean, which converges in fewer iterations because nothing is frozen. Neither is random
once Ω is fixed, so an NMF fit here is reproducible in a way `init="random"` is not.

The answer is a local minimum rather than the minimum, and two initialisations reach two
different valid factorizations of the same rank. That is the cost of the readability, and it is
why the second [`Nmf.Fit`](../reference/decomposition/factorization/nmf-fit.md) overload takes
`W₀` and `H₀` outright: an experiment that must be compared step for step supplies its own
starting point and sets `Tolerance` to zero, which turns `MaxIterations` into an exact iteration
count.

## What is not here in 0.1.0

**PCA.** Not an oversight and not one flag away — centring is what this package refuses to do to
a sparse matrix, and a dense implementation would be Math.NET Numerics' job rather than this
package's.

**`solver="cd"`.** Only the multiplicative updates ship. Coordinate descent is scikit-learn's
default solver and converges differently, so it is a second parity target rather than a faster
path to the same numbers.

**Regularization.** `alpha_W`, `alpha_H` and `l1_ratio` have no counterpart, so the factorization
is unpenalised.

**A fitted model on disk.** Neither type has `Save` or `Load`; `Components` and `SingularValues`
are plain lists, and persisting them is the caller's to arrange.

**`nndsvdar`.** scikit-learn's third initialisation fills the zeros from NumPy's Gaussian stream,
so it cannot be checked against the reference entry by entry — the reason above, and
[ADR 0072](../decisions/0072-omega-is-an-input-not-a-seed.md) again. `NndSvda` is what it
approximates, without the noise.

**A `Transform` for NMF.** Projecting an unseen row onto a non-negative basis is a factorization
with `H` held fixed, not a product, and shipping it under the SVD's name would promise a cost it
does not have.

## Parity

Every member on this page is replayed against scikit-learn 1.9.0 —
`TruncatedSVD(algorithm="randomized")` and `NMF(solver="mu")` — from corpora frozen in
`tests/oracles/`, compared at `1e-9` over a shared Ω.
[`docs/equivalence.md`](../equivalence.md) maps each Python call to its counterpart here and
names every place the two part.
