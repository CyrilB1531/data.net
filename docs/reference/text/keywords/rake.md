# Rake

Rapid Automatic Keyword Extraction: candidates are the runs between stop words, scored by summing
a per-word score over the run.

<!-- docs-declaration -->

```csharp
public sealed class Rake
```

**Example** — the paper's own worked sentence, with a small stop-word list standing in for
`StopWords.English`.

```csharp
using Lodestar.Text.Keywords;

string[] stop = ["of", "the", "over", "a", "and", "are", "for", "all", "to", "in", "is", "this", "that"];
var rake = new Rake(new RakeOptions { StopWords = stop });

var hits = rake.Extract("Compatibility of systems of linear constraints over the set of natural numbers.");
string top = hits[0].Phrase;   // => linear constraints
double score = hits[0].Score;  // => 4
```

**Remarks** — the constructor is `Rake(RakeOptions? options = null)`; `null` takes every default,
which is `StopWords.English` for the stop-word list. It throws `ArgumentOutOfRangeException` when
`options.MinLength` is below `1`, and `ArgumentException` when `options.MaxLength` is below
`options.MinLength` — a range no candidate could ever satisfy.

The co-occurrence tables — degree and frequency — are built once, over every surviving run, before
any candidate is scored. That ordering is what makes `IncludeRepeatedPhrases = false` change a
score and not only the output: dropping the duplicate before the tables are built removes its
contribution to degree and frequency too, not merely its second row in the result
([decision 0077](../../../decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md)).

For a graph-ranked alternative that looks at co-occurrence beyond a single run, see
[`TextRank`](textrank.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`RakeOptions`](rakeoptions.md), [`RakeMetric`](rakemetric.md),
[`KeywordMatch`](keywordmatch.md), [`TextRank`](textrank.md),
[the keyword extraction guide](../../../guides/keyword-extraction.md).

## Members

| Member | What it does |
| --- | --- |
| [`Rake.Extract`](rake-extract.md) | Extracts the ranked candidates of one document. |
