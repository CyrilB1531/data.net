# AnalyzerKind

Whether a document is cut into words or into runs of characters.

<!-- docs-declaration -->

```csharp
public enum AnalyzerKind { Word, Char, CharWordBoundary }
```

**Members** — `Word` splits on the token pattern and makes each token a feature, which is the
default and what most corpora want. `Char` slides an n-gram window over the whole document,
spaces included, so a feature can straddle two words. `CharWordBoundary` slides the same window
but never crosses a word boundary, padding short words instead.

**Example** — the same document, cut three ways.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats"];

var words = new CountVectorizer(new CountVectorizerOptions { Analyzer = AnalyzerKind.Word });
int wordFeatures = words.FitTransform(docs).ColumnCount;  // => 3

var chars = new CountVectorizer(new CountVectorizerOptions
{
    Analyzer = AnalyzerKind.Char,
    NgramRange = (3, 3),
});
int charFeatures = chars.FitTransform(docs).ColumnCount;
```

**Remarks** — character n-grams are what to reach for when word boundaries are unreliable:
misspellings, agglutinative languages, code, or text with no spaces at all. They cost columns —
a character trigram vocabulary is far larger than a word vocabulary over the same corpus — and
they are robust in exactly the cases a word analyzer breaks.

`CharWordBoundary` is the middle position: it keeps the robustness to spelling inside a word
while refusing to invent features that span two of them. scikit-learn calls the three
`analyzer='word'`, `'char'` and `'char_wb'`, and the behaviour is the same.

`NgramRange` applies to whichever of the three is chosen, and means something different in each:
a range of `(1, 2)` under `Word` is unigrams and bigrams of *words*, and under `Char` is one- and
two-*character* runs.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizerOptions`](countvectorizeroptions.md),
[`CountVectorizer`](countvectorizer.md), the
[Python equivalence table](../../../equivalence.md).
