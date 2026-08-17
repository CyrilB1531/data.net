# SparseNorm

Which norm [`CsrMatrix.NormalizeRows`](csrmatrix-normalizerows.md) divides each row by.

<!-- docs-declaration -->

```csharp
public enum SparseNorm { L1, L2 }
```

**Members** — `L1` divides each row by the sum of the absolute values of its entries, so the row
sums to `1` and reads as a distribution. `L2` divides by the Euclidean length, so the row lies on
the unit sphere and a dot product between two rows is their cosine similarity.

**Example** — the same counts, normalized both ways.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];

CsrMatrix byLength = new CountVectorizer().FitTransform(docs);
byLength.NormalizeRows(SparseNorm.L2);
double euclidean = byLength.RowL2Norm(0);  // => 1

CsrMatrix byMass = new CountVectorizer().FitTransform(docs);
byMass.NormalizeRows(SparseNorm.L1);
double mass = byMass.RowL1Norm(2);  // => 1
```

**Remarks** — `L2` is the one to want when the vectors are going into a similarity computation,
which is most of the time: it is what makes documents of different lengths comparable, and it is
[`TfidfOptions`](tfidfoptions.md)'s default for that reason. `L1` is the one to want when the row
should be read as "what share of this document is each term", which is a different question and a
rarer one.

A row that is entirely zero has no norm to divide by and is left alone rather than producing
`NaN` — the same choice scikit-learn makes.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CsrMatrix.NormalizeRows`](csrmatrix-normalizerows.md),
[`TfidfOptions`](tfidfoptions.md), the [Python equivalence table](../../../equivalence.md).
