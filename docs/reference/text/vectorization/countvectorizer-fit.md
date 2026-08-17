# CountVectorizer.Fit

Learn the vocabulary from a corpus, and return the same instance for chaining.

<!-- docs-declaration -->

```csharp
public CountVectorizer Fit(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus to learn from. It is enumerated once.

**Returns** — `CountVectorizer`, **the same instance**, so a call can be chained. Nothing is
copied and the fit is stored on this object.

**Exceptions** — `ArgumentNullException` when `documents` is null. A corpus that leaves no terms
does **not** throw: it yields a model of zero columns, which every later transform will produce
empty rows against.

**Example** — fit on one corpus, transform another.

```csharp
using Lodestar.Text.Vectorization;

string[] training = ["the cat eats", "the dog eats"];
string[] later = ["the cat sleeps"];

var cv = new CountVectorizer();
cv.Fit(training);
CsrMatrix counts = cv.Transform(later);

int columns = counts.ColumnCount;  // => 4
```

**Remarks** — the transformed matrix has four columns because the *training* corpus had four
terms. `sleeps` was never seen during the fit, has no column, and is **dropped silently** — which
is scikit-learn's behaviour and the reason fitting on training data and transforming test data is
the correct order rather than a convenience. A term the fit never saw cannot be counted, because
there is nowhere to count it.

Fitting twice replaces the first vocabulary rather than adding to it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer.Transform`](countvectorizer-transform.md),
[`CountVectorizer.FitTransform`](countvectorizer-fittransform.md).
