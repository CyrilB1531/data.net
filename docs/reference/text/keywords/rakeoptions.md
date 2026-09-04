# RakeOptions

What `Rake` is built with.

<!-- docs-declaration -->

```csharp
public sealed record RakeOptions
```

**Properties** — `StopWords` (default `null`, which takes `StopWords.English`) is the collection
that delimits candidates; nothing is downloaded, so a caller who wants rake-nltk's own list
supplies it. `Metric` (default `RakeMetric.DegreeToFrequencyRatio`) is which per-word score
[`Rake.Extract`](rake-extract.md) sums into a phrase score — see [`RakeMetric`](rakemetric.md).
`MinLength` (default `1`) and `MaxLength` (default `100_000`) bound a candidate's length in words,
inclusive. `IncludeRepeatedPhrases` (default `true`) — false reports a repeated candidate once, and
removes the duplicate **before** the degree and frequency tables are built, so it changes scores
and not only the output. `TokenPattern` (default `\b\w+\b`) is what counts as a word; note the
single `\w+` rather than the vectorizers' `\b\w\w+\b` — a one-letter word neighbours a run boundary
here rather than being filtered out, and dropping it would merge two candidates into one.

**Example** — turning off repeats on a document that has one.

```csharp
using Lodestar.Text.Keywords;

var rake = new Rake(new RakeOptions { IncludeRepeatedPhrases = false });
var hits = rake.Extract("linear constraints and linear constraints");

int count = hits.Count;        // => 1
string phrase = hits[0].Phrase;  // => linear constraints
```

**Remarks** — every default here is the paper's, with two exceptions: rake-nltk downloads its
stop-word list from the `nltk` corpus at run time and this does not, and `TokenPattern`'s `\b\w+\b`
is not rake-nltk's own default tokenizer either — it is what the oracle generator injects into
rake-nltk's `word_tokenizer` so the two sides can be compared at all. A caller who wants exact
parity with a specific rake-nltk run supplies that run's own list and tokenizer through `StopWords`
and `TokenPattern`
([decision 0077](../../../decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md)).

`Rake`'s constructor validates two of these fields eagerly: a `MinLength` below `1` or a
`MaxLength` below `MinLength` throws at construction, before any document is read.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Rake`](rake.md), [`RakeMetric`](rakemetric.md), [`KeywordMatch`](keywordmatch.md),
the [Python equivalence table](../../../equivalence.md).
