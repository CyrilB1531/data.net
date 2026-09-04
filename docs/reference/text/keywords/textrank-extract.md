# TextRank.Extract

Extracts the ranked keywords of one document.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<KeywordMatch> Extract(string text)
```

**Parameters** — `text` is the document.

**Returns** — `IReadOnlyList<KeywordMatch>`, sorted by descending score, glued where their parts
were adjacent in the source. Empty when the document has no co-occurrence at all — too short, or
every token a stop word.

**Exceptions** — `ArgumentNullException` when `text` is null. `InvalidOperationException` when the
power iteration does not converge within `TextRankOptions.MaxIterations`.

**Example** — a clean run that reaches the last token of the document is dropped whole rather than
reported as a partial phrase; adding a period changes what survives.

```csharp
using Lodestar.Text.Keywords;

string doc = "Copper wires conduct electricity through metal circuits";
var textRank = new TextRank(new TextRankOptions { Words = 4 });

var withoutPeriod = textRank.Extract(doc);
var withPeriod = textRank.Extract(doc + ".");

int countWithoutPeriod = withoutPeriod.Count;  // => 2
int countWithPeriod = withPeriod.Count;        // => 3
```

**Remarks** — that difference is summa's own quirk, reproduced deliberately: its inner loop reports
a continuation only when it is rejected, and a clean run that only stops because the document ran
out is dropped rather than reported — see `Copper wires conduct electricity through metal circuits`
above, where `metal circuits` never appears without the trailing period. A phrase carries the exact
spelling found at its own position — never a document-wide most common form — and a spelling is
consumed once it is glued into a phrase, so a repeated keyword contributes to at most one phrase
per document.

`TextRankOptions.Words` overrides `TextRankOptions.Ratio` when set; with neither given, the default
keeps the top 20% of ranked stems. The ranking itself is [`TextRank`](textrank.md)'s dominant
eigenvector, not whichever one an unchecked eigensolver returns first — see that page's Remarks.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TextRank`](textrank.md), [`TextRankOptions`](textrankoptions.md),
[`KeywordMatch`](keywordmatch.md), [`Rake.Extract`](rake-extract.md), the
[Python equivalence table](../../../equivalence.md).
