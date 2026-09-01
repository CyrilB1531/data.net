# HashingVectorizerOptions

How many columns the hash lands in, and what it does with signs.

<!-- docs-declaration -->

```csharp
public sealed record HashingVectorizerOptions
```

**Properties** — `NumFeatures` (default `1048576`, which is `2^20`) is how many columns the matrix
has, and therefore how often two different terms collide into one. `AlternateSign` (default
`true`) gives half the terms a negative weight, so that collisions tend to cancel rather than
accumulate. `Norm` (default `SparseNorm.L2`) is the norm each row is divided by, and is **nullable**: `null` leaves the rows unnormalized, which is scikit-learn's `norm=None`. `Count` carries
the tokenization settings — the token pattern, lowercasing, n-gram range and the rest — because
hashing changes only what happens *after* a document is cut into terms.

**Example** — a deliberately tiny feature space, so collisions are certain.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });
CsrMatrix hashed = hv.Transform(["the cat eats", "the dog eats", "the cat and the dog"]);

int columns = hashed.ColumnCount;  // => 16
double rowLength = hashed.RowL2Norm(0);  // => 1
```

**Remarks** — `NumFeatures` is the whole trade. Large enough and collisions are rare and the matrix
is wide; small enough and the matrix is compact and two unrelated terms share a column. The default
of `2^20` is scikit-learn's `n_features=2**20`, chosen so that collisions are negligible for most
corpora while the matrix stays sparse — the columns cost nothing until something lands in them.

`AlternateSign` is the part that looks like a trick and is not. When two terms collide, one of them
carrying a negative sign means the collision partly cancels instead of doubling, so the inner
product between two documents stays close to what it would have been without the collision. It is
`alternate_sign=True` in scikit-learn and on by default for the same reason.

There is no `MinDf` or `MaxDf` here, and there cannot be: both need document frequencies, and
counting those would mean the pass over the corpus that this vectorizer exists to avoid.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizer`](hashingvectorizer.md),
[`CountVectorizerOptions`](countvectorizeroptions.md), [`SparseNorm`](../../abstractions/sparse/sparsenorm.md), the
[Python equivalence table](../../../equivalence.md).
