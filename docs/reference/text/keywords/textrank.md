# TextRank

TextRank over a co-occurrence graph: rank the stems, keep the best, and re-glue the ones that stood
next to each other in the source.

<!-- docs-declaration -->

```csharp
public sealed class TextRank
```

**Example** — the top-ranked word of a two-sentence abstract.

```csharp
using Lodestar.Text.Keywords;

string doc =
    "Compatibility of systems of linear constraints over the set of natural numbers. " +
    "Criteria of compatibility of a system of linear Diophantine equations.";

var textRank = new TextRank(new TextRankOptions { Words = 4 });
var hits = textRank.Extract(doc);

string top = hits[0].Phrase;  // => numbers
int count = hits.Count;       // => 4
```

**Remarks** — the constructor is `TextRank(TextRankOptions? options = null)`; `null` takes every
default, `StopWords.English` for the stop-word list included. It throws
`ArgumentOutOfRangeException` when `options.Window` is below `1`, `options.Damping` is outside
`(0, 1)`, `options.Ratio` is outside `(0, 1]`, `options.MaxIterations` is below `1`, or
`options.Words` is set and negative.

A glued phrase — where two ranked stems stood adjacent in the source and are re-joined — scores the
**mean** of its parts and need not be grammatical; that is summa's own behaviour, reproduced on
purpose rather than smoothed over. A stem extends a run only when the source spelled it exactly as
it lower-cases to and it stood clear of punctuation on both sides — summa's own `text.split()`
equality check.

**`Rank` returns the dominant left eigenvector, not "whatever `eig` returns first".** summa reads
`scipy.linalg.eig`'s first column unchecked, and for a near-bipartite co-occurrence graph that
column is not the dominant one — measured on the Rose abstract, where it belongs to λ = −0.85
against a dominant 1.0. `TextRank.Extract` always returns the dominant ranking; two documents
drafted for the frozen oracle corpus disagreed with it for this reason and were removed by hand
before the corpus was written, and the generator now carries a guard that raises rather than freeze
a future document shaped the same way
([decision 0077](../../../decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md)).

For a run-based alternative that scores candidates without building a graph, see [`Rake`](rake.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TextRankOptions`](textrankoptions.md), [`KeywordMatch`](keywordmatch.md),
[`Rake`](rake.md), [the keyword extraction guide](../../../guides/keyword-extraction.md).

## Members

| Member | What it does |
| --- | --- |
| [`TextRank.Extract`](textrank-extract.md) | Extracts the ranked keywords of one document. |
