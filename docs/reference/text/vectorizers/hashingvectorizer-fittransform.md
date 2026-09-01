# HashingVectorizer.FitTransform

The same as [`Transform`](hashingvectorizer-transform.md) — there is nothing to fit.

<!-- docs-declaration -->

```csharp

public CsrMatrix FitTransform(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus to vectorize.

**Returns** — [`CsrMatrix`](../../abstractions/sparse/csrmatrix.md), identical to what
[`Transform`](hashingvectorizer-transform.md) returns for the same input.

**Exceptions** — `ArgumentNullException` when `documents` is null.

**Example** — the two calls agree, which is the whole content of this member.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats"];
var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });

CsrMatrix fitted = hv.FitTransform(docs);
CsrMatrix transformed = hv.Transform(docs);

bool same = fitted.NonZeroCount == transformed.NonZeroCount;  // => True
```

**Remarks** — it exists so that the three vectorizers can be swapped for one another without the
calling code changing shape. Code written against `FitTransform` works with all three; code that
also calls `Fit` does not, because this type has none.

scikit-learn's `HashingVectorizer` carries a `fit` for the same reason — its pipeline API requires
one — and it likewise does nothing.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizer.Transform`](hashingvectorizer-transform.md),
[`CountVectorizer.FitTransform`](countvectorizer-fittransform.md).
