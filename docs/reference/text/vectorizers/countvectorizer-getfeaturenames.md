# CountVectorizer.GetFeatureNames

The term each column stands for, in column order.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<string> GetFeatureNames()
```

**Returns** — `IReadOnlyList<string>` of length `ColumnCount`, sorted, where index `i` is the term
counted by column `i`.

**Exceptions** — `InvalidOperationException` when nothing has been fitted yet.

**Example** — the vocabulary is sorted, so the order depends on the terms and not the documents.

```csharp
using Lodestar.Text.Vectorization;

var cv = new CountVectorizer();
cv.Fit(["the cat eats", "the dog eats", "the cat and the dog"]);

IReadOnlyList<string> names = cv.GetFeatureNames();

string first = names[0];   // => and
string last = names[4];    // => the
```

**Remarks** — this is what makes a count matrix readable, and it is the thing
[`HashingVectorizer`](hashingvectorizer.md) cannot offer: hashing throws the vocabulary away, so
there is no name to return and no method to return it. Choosing that vectorizer is choosing to
give this up.

The list is the vectorizer's own, exposed read-only rather than copied, so reading it allocates
nothing.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer`](countvectorizer.md), [`CsrMatrix`](csrmatrix.md).
