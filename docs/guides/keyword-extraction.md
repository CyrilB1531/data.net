# Keyword extraction

`Lodestar.Text.Keywords` holds two unsupervised extractors — [`Rake.Extract`](../reference/text/keywords/rake-extract.md)
and [`TextRank.Extract`](../reference/text/keywords/textrank-extract.md) — and neither needs a
model: both read the co-occurrence of the document's own words. Together with
`Lodestar.Onnx` and [`Mmr.Select`](../reference/embeddings/search/mmr-select.md) they also compose
into a KeyBERT-style pipeline, the last section below.

```bash
dotnet add package Lodestar.Text
```

## RAKE: candidates from stop-word-delimited runs

RAKE never builds a graph. It splits the document into runs of words between stop words and
punctuation, then scores each run by summing a per-word score — how often the word occurs, and how
much company it keeps — over the run.

```csharp
using Lodestar.Text.Keywords;

string abstractText =
    "Compatibility of systems of linear constraints over the set of natural numbers.";

var rake = new Rake();   // StopWords.English by default -- nothing downloaded
IReadOnlyList<KeywordMatch> hits = rake.Extract(abstractText);

foreach (KeywordMatch hit in hits.Take(3))
{
    Console.WriteLine($"{hit.Phrase}  ({hit.Score:F2})");
}
```

Nothing here is downloaded at run time: `RakeOptions.StopWords` defaults to `StopWords.English`,
already in the assembly, and a caller who wants rake-nltk's own list passes it explicitly. See
[decision 0077](../decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md)
for why, and [`RakeOptions`](../reference/text/keywords/rakeoptions.md) for every switch —
`Metric`, the length bounds, and whether a repeated candidate is reported once or every time it
occurs.

## TextRank: candidates from a ranked graph

TextRank builds a co-occurrence graph over the document's stems — an edge between two words that
stood within `TextRankOptions.Window` tokens of each other — and ranks it the way PageRank ranks a
link graph: a power iteration to the dominant eigenvector. The highest-ranked stems are kept, and
ones that stood adjacent in the source are glued back into a phrase.

```csharp
using Lodestar.Text.Keywords;

string abstractText =
    "Compatibility of systems of linear constraints over the set of natural numbers. " +
    "Criteria of compatibility of a system of linear Diophantine equations.";

var textRank = new TextRank(new TextRankOptions { Words = 4 });
IReadOnlyList<KeywordMatch> ranked = textRank.Extract(abstractText);

string best = ranked[0].Phrase;
```

**"Dominant" is not a formality.** summa, the Python reference, reads `scipy.linalg.eig`'s first
column without checking it is the dominant one, and for a near-bipartite co-occurrence graph it
often is not — measured on a real abstract, where that column belongs to an eigenvalue of −0.85
against a dominant 1.0. [`TextRank.Extract`](../reference/text/keywords/textrank-extract.md) always
ranks by the dominant eigenvector; two documents drafted for the frozen oracle corpus disagreed with
it for exactly this reason and were removed by hand before the corpus was written. What is left is
worse than a wrong pick: when the graph's transition matrix has a repeated eigenvalue — measured,
`two_sentences` carries 0.85 at multiplicity 3 — which column `eig` returns first is not even the
same from one machine's BLAS build to another's, so summa's own output is not reproducible. The
oracle generator no longer trusts it: it selects the dominant left eigenvector itself, by eigenvalue
rather than by column position, before calling summa at all — forced by reproducibility, not chosen.
[Decision 0077](../decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md)
has the measurement and [`TextRank`](../reference/text/keywords/textrank.md)'s own Remarks have the
rest of the divergence.

## KeyBERT-style selection: RAKE candidates, embedded, spread out by MMR

`keybert` — the Python library the [equivalence table](../equivalence.md) compares
[`Mmr.Select`](../reference/embeddings/search/mmr-select.md) against — is not one algorithm but a
composition of three: RAKE-shaped candidate generation, a sentence embedding of the document and of
each candidate, and Maximal Marginal Relevance to pick a spread-out subset instead of the `n` most
relevant (which are usually near-duplicates of each other). Nothing in Lodestar bundles that
composition into one call — each of the three pieces already exists as its own package, and writing
it out is a dozen lines:

```csharp
using Lodestar.Embeddings.Search;
using Lodestar.Onnx;
using Lodestar.Text.Keywords;

// 1. Candidates: every RAKE run, deduplicated, is a reasonable keyword shape.
IReadOnlyList<KeywordMatch> candidates = new Rake(new RakeOptions { IncludeRepeatedPhrases = false })
    .Extract(document);
string[] phrases = candidates.Select(c => c.Phrase).ToArray();

// 2. Vectors: the document and every candidate, in one batch.
using var embedder = new OnnxTextEmbedder("model.onnx", wp);
string[] batch = [document, .. phrases];
float[][] vectors = embedder.EmbedBatch(batch);
float[] documentVector = vectors[0];
float[][] candidateVectors = vectors[1..];

// 3. Selection: spread the top picks out instead of returning near-duplicates.
int[] chosen = Mmr.Select(documentVector, candidateVectors, count: 5, lambda: 0.7);
string[] keywords = chosen.Select(i => phrases[i]).ToArray();
```

Three divergences from `keybert` itself, all recorded in
[decision 0077](../decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md)
and in the [equivalence table](../equivalence.md)'s
[`Mmr.Select`](../reference/embeddings/search/mmr-select.md) row: `keybert` parameterises
`diversity = 1 − λ` rather than taking `λ` directly, it rounds its scores to four decimals, and it
returns its picks **sorted by relevance** rather than in selection order —
[`Mmr.Select`](../reference/embeddings/search/mmr-select.md) returns selection order, so a caller
matching `keybert`'s output ordering re-sorts by score afterwards. The oracle corpus behind
[`Mmr.Select`](../reference/embeddings/search/mmr-select.md)'s parity claim therefore compares the
selected **set**, not the sequence.

## See also

- [`Rake`](../reference/text/keywords/rake.md), [`TextRank`](../reference/text/keywords/textrank.md) —
  every member, every default, every exception.
- [`Mmr`](../reference/embeddings/search/mmr.md) — the selection step alone, without the embedding
  or the candidate generation.
- [The ONNX inference guide](onnx.md) — `OnnxTextEmbedder`, batching, and what `EncodingOptions`
  controls.
- [Python → C# equivalence](../equivalence.md) — the `rake-nltk`, `summa` and `keybert` rows.
