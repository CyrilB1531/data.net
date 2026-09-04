# KeywordMatch

One extracted phrase and the score that ranked it.

<!-- docs-declaration -->

```csharp
public readonly record struct KeywordMatch(string Phrase, double Score)
```

**Example** — reading a hit apart.

```csharp
using Lodestar.Text.Keywords;

var rake = new Rake();
var hits = rake.Extract("linear constraints and linear constraints");

KeywordMatch hit = hits[0];
string phrase = hit.Phrase;  // => linear constraints
```

**Remarks** — `Phrase` is the extractor's own assembly of the source text — a RAKE run verbatim, or
a TextRank glue of adjacent survivors. `Score` is higher-is-better and on a scale specific to the
extractor that produced it: a `Rake` score and a `TextRank` score are never comparable, even on the
same document.

Being a `record struct`, it compares by value and deconstructs — `var (phrase, score) = hit;` works.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Rake.Extract`](rake-extract.md), [`TextRank.Extract`](textrank-extract.md),
[the keyword extraction guide](../../../guides/keyword-extraction.md).
