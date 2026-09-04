# Rake.Extract

Extracts the ranked candidates of one document.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<KeywordMatch> Extract(string text)
```

**Parameters** — `text` is the document. A `string` is required, not a span: the candidates are
runs of it, kept as new strings in the result, so nothing is saved by taking a span in.

**Returns** — `IReadOnlyList<KeywordMatch>`, one entry per surviving candidate, sorted by
descending score; a tie breaks by phrase, ordinal **descending over UTF-16 code units** —
rake-nltk's own rule (`rake_nltk/rake.py:241`, `(score, phrase)` sorted with `reverse=True`
over Python's **code-point** order), not text order. The two agree except when the deciding
character sits outside the Basic Multilingual Plane. Empty when the document has none — every
token was a stop word, or nothing survived `RakeOptions.MinLength`/`MaxLength`.

**Exceptions** — `ArgumentNullException` when `text` is null.

**Example** — two two-word candidates tie for the top score; unlike `TextRank.Extract`, nothing
here is glued back together, so a candidate is exactly the run RAKE found. The tie puts "natural
numbers" first, ahead of "linear constraints" — reverse-alphabetical, not the order either
phrase occurs in the source.

```csharp
using Lodestar.Text.Keywords;

string[] stop = ["of", "the", "over", "a", "and", "are", "for", "all", "to", "in", "is", "this", "that"];
var rake = new Rake(new RakeOptions { StopWords = stop });

var hits = rake.Extract("Compatibility of systems of linear constraints over the set of natural numbers.");
int count = hits.Count;        // => 5
string top = hits[0].Phrase;      // => natural numbers
double topScore = hits[0].Score;  // => 4
```

**Remarks** — the default metric, `RakeMetric.DegreeToFrequencyRatio`, sums `deg(w) / freq(w)` over
the run's words; [`RakeMetric`](rakemetric.md) has the other two. A one-word candidate's degree
equals its frequency times one, so a longer run generally outranks a single repeated word — that
is the whole mechanism, not a special case.

The document is scanned once: the co-occurrence tables are built from every surviving run before
any candidate is scored, so a run [`RakeOptions.MinLength`](rakeoptions.md)/`MaxLength` drops never
contributes degree or frequency to what remains.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Rake`](rake.md), [`RakeOptions`](rakeoptions.md), [`RakeMetric`](rakemetric.md),
[`KeywordMatch`](keywordmatch.md), [`TextRank.Extract`](textrank-extract.md), the
[Python equivalence table](../../../equivalence.md).
