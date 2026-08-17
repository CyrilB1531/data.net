# TfidfVectorizer.GetFeatureNames

The term each column stands for, in column order.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<string> GetFeatureNames()
```

**Returns** — `IReadOnlyList<string>` of length `ColumnCount`, sorted, where index `i` names the
term weighted by column `i`.

**Exceptions** — `InvalidOperationException` when nothing has been fitted yet.

**Example** — the names, and the weight beside them.

```csharp
using Lodestar.Text.Vectorization;

var tv = new TfidfVectorizer();
CsrMatrix weighted = tv.FitTransform(["the cat eats", "the dog eats", "the cat and the dog"]);

IReadOnlyList<string> names = tv.GetFeatureNames();
string first = names[0];  // => and
```

**Remarks** — the vocabulary is sorted and shared with the count half, so this returns exactly
what [`CountVectorizer.GetFeatureNames`](countvectorizer-getfeaturenames.md) would over the same
corpus and options. Pairing it with `Idf` is how to see which terms the weighting considered rare.

[`HashingVectorizer`](hashingvectorizer.md) has no counterpart, because it keeps no vocabulary to
name.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer`](tfidfvectorizer.md),
[`CountVectorizer.GetFeatureNames`](countvectorizer-getfeaturenames.md).
