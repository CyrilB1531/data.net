# TfidfOptions

The four switches that decide how a count becomes a weight.

<!-- docs-declaration -->

```csharp
public sealed record TfidfOptions
```

**Properties** — `UseIdf` (default `true`) multiplies each term frequency by the inverse document
frequency; turn it off and the result is term frequencies with the normalization still applied.
`SmoothIdf` (default `true`) adds one to every document count, as if a document containing every
term had been seen, which is what keeps a term appearing in every document from dividing by zero.
`SublinearTf` (default `false`) replaces the count `tf` with `1 + log(tf)`, so a term appearing
twenty times counts as a little more than one appearing ten, rather than twice as much.
`Norm` (default `SparseNorm.L2`) is the norm each row is divided by afterwards, and is **nullable**: `null` leaves the rows unnormalized, which is scikit-learn's `norm=None`. Measured, a row whose normalized length would be `1` comes out at `1.9938235052555415` with `Norm = null`.

**Example** — the defaults, and the row they produce.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];

CsrMatrix weighted = new TfidfVectorizer().FitTransform(docs);
double rowLength = weighted.RowL2Norm(0);  // => 1
```

**Remarks** — every default here is scikit-learn's, and the four names answer to `use_idf`,
`smooth_idf`, `sublinear_tf` and `norm`. The one worth understanding rather than accepting is
`SmoothIdf`: with it on, the IDF of a term is `log((1 + n) / (1 + df)) + 1`, and the trailing
`+ 1` means **a term appearing in every document still has a non-zero weight**. That surprises
readers who expect ubiquitous terms to vanish, and it is deliberate on both sides — the
normalization that follows is what actually demotes them.

`SublinearTf` is the switch to reach for when documents vary a lot in length and a long document
should not dominate merely by repeating itself. It is off by default because scikit-learn has it
off.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfTransformer`](tfidftransformer.md),
[`TfidfVectorizer`](tfidfvectorizer.md), [`SparseNorm`](../../abstractions/sparse/sparsenorm.md), the
[Python equivalence table](../../../equivalence.md).
