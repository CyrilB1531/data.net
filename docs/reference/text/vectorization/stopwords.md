# StopWords

The six built-in stop-word lists, one per language with a Snowball stemmer.

<!-- docs-declaration -->

```csharp
public static class StopWords
```

**Properties** — `English`, `French`, `German`, `Italian`, `Portuguese` and `Spanish`, each an
`IReadOnlyCollection<string>` of lowercase words — a collection rather than a set because
`IReadOnlySet<T>` does not exist on netstandard2.0, which this package still targets. They are frozen sets built once and shared, so reading
one allocates nothing and the same instance comes back every time.

**Example** — the English list, passed where a vectorizer expects one.

```csharp
using Lodestar.Text.Vectorization;

int english = StopWords.English.Count;  // => 318

var cv = new CountVectorizer(new CountVectorizerOptions { StopWords = StopWords.English });
CsrMatrix counts = cv.FitTransform(["the cat eats", "a dog eats"]);
```

**Remarks** — a stop word is removed **after** tokenization and lowercasing, so a list of
lowercase words is all that is needed however the document was written. Removal happens before
n-grams are formed, which is why `NgramRange = (2, 2)` over a corpus with stop words removed
produces bigrams of words that were never adjacent in the source.

The English list is scikit-learn's `'english'` exactly, which is worth knowing because that list
is [documented by scikit-learn itself as problematic](../../../equivalence.md) — it is aggressive,
it removes words that carry meaning in some domains, and it exists mostly for parity. The other
five have no scikit-learn counterpart at all: `stop_words='english'` is the only built-in list
there, so those five are additions rather than reproductions.

Nothing stops a caller passing its own words instead; the option takes any
`IReadOnlyCollection<string>`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizerOptions`](countvectorizeroptions.md),
[`CountVectorizer`](countvectorizer.md), the
[Python equivalence table](../../../equivalence.md).
