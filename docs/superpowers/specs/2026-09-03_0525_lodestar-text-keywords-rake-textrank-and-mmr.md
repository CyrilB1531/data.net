# 0525 — `Lodestar.Text.Keywords`: RAKE, TextRank, and MMR beside the embeddings

**Issue:** [#525](https://github.com/CyrilB1531/lodestar/issues/525) ·
**Status:** accepted · **Date:** 2026-09-03

## Problem

Given one document, name the phrases it is about. No training set, no labels, no model — the
document is the whole input.

[ADR 0074](../../decisions/0074-the-phase-2-gaps-restated-on-what-the-packages-export.md) read the
incumbents' exported surfaces rather than their descriptions, and closed two of
[#440](https://github.com/CyrilB1531/lodestar/issues/440)'s four remaining lots on what it found.
This one survived that reading:

- `Yake.NET` 1.0.0 exists, so *"no C# YAKE at all"* was already false and **YAKE is out of scope**.
- A NuGet search for `rake textrank` returns **zero packages**.
- `TajikKEA` 1.0.0 is Tajik-specific — `IWordContext`, `IDFCategory`, per-language stop words.
- `APIVerve.API.KeywordExtractor` is a client for a remote HTTP service, not local extraction.

So RAKE, TextRank and a KeyBERT-style selection are unserved by any local .NET package.

## Two of the issue's assumptions did not survive contact

Both were checked against the running libraries before this spec was written, and both are load-bearing.

**1. Candidate generation is not shared.** #525 says *"Candidate generation is shared: n-gram
ranges over the tokenizer already in `Lodestar.Text`."* It is not, and cannot be. RAKE's candidates
*are* the maximal runs between stop words and punctuation — the split is the algorithm, not a
preprocessing step in front of it. TextRank has no phrase candidates at all: it ranks single words
in a graph and re-glues the survivors that happened to be adjacent in the source. Two mechanisms,
neither derivable from the other, and no shared generator to write.

The existing `TextAnalyzer` cannot serve either. It is `internal`, and its `Tokenize` **drops**
stop words rather than marking their positions — which is precisely the information RAKE needs.

**2. TextRank has no convergence tolerance to parameterise.** #525 says *"The damping factor, the
window and the convergence tolerance are all parameters, not constants."* `summa` does not iterate.
`summa.keywords` imports `pagerank_weighted_scipy`, which builds the dense matrix

```text
M = damping · A + (1 − damping) · (1/n)
```

where `A` is the adjacency matrix with each row divided by that node's weighted degree, and then
calls `scipy.linalg.eig(M, left=True, right=False)`. `CONVERGENCE_THRESHOLD = 0.0001` is dead code
on this path. Its `process_results` takes `abs(vecs[i][0])` — the first column LAPACK returns,
not the one belonging to the largest eigenvalue.

**`keywords()` deletes every unreachable node before ranking**, which `get_graph` does not — a
word whose weighted degree is zero is dropped by `remove_unreachable_nodes`. That step is
load-bearing and easy to miss: with the isolated nodes still in, `M`'s rows sum to `{0.15, 1.0}`,
the matrix is substochastic, and its dominant eigenvector is a different vector. After the removal,
every row sums to `1.0`.

So `M` is row-stochastic *once the unreachable nodes are gone*, its dominant left eigenvector is
the stationary distribution, and `scipy` normalizes eigenvectors to unit L2 norm — **the scores are
that distribution scaled to unit L2 norm**.

Measured on a two-sentence document: power iteration with per-step renormalisation reproduces the
four scores `summa` returns to within **2e-15** — `linear` at `0.4686942795397482` against
`summa`'s `0.46869427953974613`. The agreement is at machine precision, not at the `1e-6` a first
reading of "eigensolver against power iteration" would suggest.

LAPACK's column ordering is still not a contract, and `process_results` reads `vecs[i][0]`
regardless. The generator therefore asserts per document that `summa`'s own output matches an
independently computed stationary distribution, and refuses to freeze a case where it does not: a
corpus entry holding a non-dominant eigenvector would make the C# side fail for being right.

## The POS filter is inert, so the no-tagger constraint holds

Issue #525 rules out a part-of-speech tagger: *"no tagger — we do not have one and will not add one for
this."* `summa.keywords` declares `INCLUDING_FILTER = ['NN', 'JJ']`, which reads like a blocker.

It is not. `SyntacticUnit.tag` is `None` for every unit — measured on
`clean_text_by_word("linear constraints over natural numbers")`, all four tags `None` — and the
guard is `if (include_filters and unit.tag in include_filters) or not include_filters or not
unit.tag`, whose last clause admits everything. `summa` never tags, and parity with it needs no
tagger.

## What already exists and is reused

`summa` stems its graph nodes with **Snowball English** — `get_graph` over the RAKE paper's abstract
returns `['compat', 'system', 'linear', 'constraint', 'set', 'natur', 'number']`. `Lodestar.Text`
ships `EnglishSnowballStemmer` at nltk parity, and `summa`'s bundled copy agrees with nltk's on all
24 words of a probe drawn from that abstract. Nothing new is written for stemming.

## Scope — RAKE

Rose et al. Split the document into candidate phrases at every stop word and every punctuation
mark; score each word by the chosen metric over the co-occurrence graph of the phrases it appears
in; sum over the phrase.

```csharp
namespace Lodestar.Text.Keywords;

public sealed record RakeOptions
{
    public IReadOnlyCollection<string>? StopWords { get; init; }   // null → StopWords.English
    public RakeMetric Metric { get; init; } = RakeMetric.DegreeToFrequencyRatio;
    public int MinLength { get; init; } = 1;                       // words, inclusive
    public int MaxLength { get; init; } = 100_000;                 // words, inclusive
    public bool IncludeRepeatedPhrases { get; init; } = true;
    public string TokenPattern { get; init; } = @"\b\w+\b";
}

public enum RakeMetric
{
    DegreeToFrequencyRatio,   // deg(w) / freq(w), summed — the paper's, and rake-nltk's default
    WordDegree,               // deg(w), summed
    WordFrequency,            // freq(w), summed
}

public sealed class Rake
{
    public Rake(RakeOptions? options = null);
    public IReadOnlyList<KeywordMatch> Extract(string text);
}
```

`TokenPattern` is `\b\w+\b` and not the vectorizers' `\b\w\w+\b`: a one-letter word is a phrase
boundary's neighbour, not a stop word, and dropping it silently would merge two candidates that the
paper keeps apart. **The generator injects this same pattern into `rake-nltk`'s `word_tokenizer`**,
so the two sides tokenize identically by construction rather than by coincidence.

All three metrics ship. They are one `switch` over the same degree and frequency tables, the paper
defines all three, and `rake-nltk` exposes all three — offering one and calling it parity would be
a choice nobody could see.

Worked example, the abstract from Rose et al. that `rake-nltk` reproduces exactly:

| metric | top three |
| --- | --- |
| `DegreeToFrequencyRatio` | `8.5 minimal generating sets`, `8.5 linear diophantine equations`, `4.5 linear constraints` |
| `WordDegree` | `11.0 minimal generating sets`, `11.0 linear diophantine equations`, `8.0 minimal set` |
| `WordFrequency` | `4.0 minimal set`, `4.0 minimal generating sets`, `4.0 linear diophantine equations` |

## Scope — TextRank

Mihalcea and Tarau, as `summa` implements it.

```csharp
namespace Lodestar.Text.Keywords;

public sealed record TextRankOptions
{
    public IReadOnlyCollection<string>? StopWords { get; init; }   // null → StopWords.English
    public int Window { get; init; } = 2;
    public double Damping { get; init; } = 0.85;
    public double Tolerance { get; init; } = 1e-12;
    public int MaxIterations { get; init; } = 1_000;
    public double Ratio { get; init; } = 0.2;                      // used when Words is null
    public int? Words { get; init; }
}

public sealed class TextRank
{
    public TextRank(TextRankOptions? options = null);
    public IReadOnlyList<KeywordMatch> Extract(string text);
}
```

The pipeline, in the order `summa` runs it: tokenize; drop stop words; stem with
`EnglishSnowballStemmer`; build the undirected co-occurrence graph over a sliding window of
`Window`; **delete every node of zero weighted degree**; row-normalize by weighted degree;
power-iterate to the stationary distribution, renormalising each step;
normalize to unit L2 norm; take the top `Ratio` proportion (or `Words` count) of stems; map each
back to its most frequent surface form; **re-glue stems adjacent in the source**, scoring a glued
phrase as the mean of its parts.

`Tolerance` and `MaxIterations` are this implementation's, not `summa`'s — they are how a power
iteration reaches what an eigensolver returns in one call, and the spec says so rather than
implying `summa` has them.

Worked example, the same abstract, `Ratio = 0.2`: `0.3851507605 inequations` and
`0.3851507605 equations strict`. The second is the re-gluing, and it is not a grammatical phrase —
that is `summa`'s behaviour and reproducing it is the point.

## Scope — MMR

Carbonell and Goldstein. Greedy selection that trades relevance against redundancy.

```csharp
namespace Lodestar.Embeddings.Search;

public static class Mmr
{
    public static int[] Select(
        ReadOnlySpan<float> query,
        IReadOnlyList<float[]> candidates,
        int count,
        double lambda = 0.5);
}
```

Returns the chosen **indices**, in selection order. The first is `argmax sim(c, query)`; each
later one maximises `λ·sim(c, query) − (1 − λ)·max(sim(c, s) for s already selected)`. Similarity
is cosine, over `VectorMath.Dot` and `VectorMath.L2Norm`, which live in the same namespace.

Indices rather than scores: the caller owns the candidates and knows what they are, and an index
array composes with anything — keyword phrases, retrieved passages, index rows. That is also what
keeps this out of `Lodestar.Text`: MMR knows nothing about text, and putting it beside
`EmbeddingIndex` avoids the `Text ↔ Embeddings` edge every other package avoids.

**There is no `KeyBert` type.** The KeyBERT equivalent is a composition — candidates from
`Lodestar.Text.Keywords`, vectors from `Lodestar.Onnx`, selection from `Mmr` — about ten lines at
the call site, and `docs/guides/keywords.md` shows them. A type for it would manufacture the edge
this layout is built to avoid, and would name a generic algorithm after one of its callers.

## The shared result type

```csharp
namespace Lodestar.Text.Keywords;

public readonly record struct KeywordMatch(string Phrase, double Score);
```

Both extractors return `IReadOnlyList<KeywordMatch>` in descending score. Same shape as
`BkTreeMatch` from [#526](https://github.com/CyrilB1531/lodestar/issues/526), for the same reason:
a ranked result is a pair, and a `record struct` costs no allocation per hit.

No `IKeywordExtractor`. The two share only `Extract(string)`; their options have nothing in common
— phrase length and a metric against a window, a damping factor and a selection ratio — so an
interface would carry one method and buy a substitutability nobody asked for.

## Placement

| unit | package | namespace |
| --- | --- | --- |
| `Rake`, `RakeOptions`, `RakeMetric` | `Lodestar.Text` | `Lodestar.Text.Keywords` |
| `TextRank`, `TextRankOptions` | `Lodestar.Text` | `Lodestar.Text.Keywords` |
| `KeywordMatch` | `Lodestar.Text` | `Lodestar.Text.Keywords` |
| `Mmr` | `Lodestar.Embeddings` | `Lodestar.Embeddings.Search` |

No new package and no new edge. Both packages stay core tier under
[ADR 0076](../../decisions/0076-a-core-package-carries-no-external-dependency.md): nothing here
needs an external dependency.

`Lodestar.Text.Keywords` gets its own tokenization, small and private, because `TextAnalyzer`
discards exactly what RAKE needs. It is not a second public analyzer.

## Oracles and parity

Three corpora under `tests/oracles/`, three generators in `tools/generate_oracles.py`, each
declaring `library`, `library_version` and `reference_calls` like every other.

| corpus | oracle | compared |
| --- | --- | --- |
| `keywords_rake.json` | `rake-nltk` 1.0.6 | phrases **exactly**, scores at `1e-9` |
| `keywords_textrank.json` | `summa` 1.2.0 | phrases **exactly**, scores at `1e-12` |
| `mmr.json` | `keybert` 0.9.0, `keybert._mmr.mmr` | selected **set** exactly |

All three are **MIT**, which [ADR 0003](../../decisions/0003-provenance-and-licensing.md) requires
even of a library used only to generate test data.

**Each corpus freezes the stop-word list its oracle used**, and the test passes that list to the
extractor explicitly. The three lists differ — nltk's, `summa`'s own 339 words, and the pinned list
`Lodestar.Text` ships — so parity is only meaningful when the list is an input rather than a
default. The API default stays `StopWords.English`; the gap between it and each oracle's list is
recorded in the ADR, not papered over.

**`rake-nltk` is called with an injected tokenizer and stop-word list**, which its constructor
accepts as `word_tokenizer`, `sentence_tokenizer` and `stopwords`. The generator therefore needs no
`nltk.download` of `punkt_tab` or `stopwords`, and the *Oracles are reproducible* job acquires no
new network dependency. Parity is with RAKE's algorithm as this repository calls it, not with
nltk's defaults — which is the honest claim, since the defaults are data this repository does not
pin.

`keybert` is installed `--no-deps`. `keybert._mmr` imports only numpy and scikit-learn, both
already oracle dependencies; `sentence-transformers` and torch are never installed. Verified by
importing and running it in the oracle environment.

### Divergences, for the ADR

1. **TextRank scores agree numerically, not exactly.** Power iteration against `scipy.linalg.eig`,
   compared at `1e-12` — measured agreement is 2e-15. The ranking is exact; the last digits are not
   guaranteed to be.
2. **`keybert` parameterises `diversity = 1 − λ`.** `Mmr.Select` takes `lambda`, per the paper.
3. **`keybert` rounds its returned scores to four decimals**, and **sorts its result by similarity
   to the document rather than by selection order** — measured at `lambda = 0`, where it returns
   `[c0, c2, c3]` for a selection that ran `c0, c3, c2`. The corpus therefore compares the selected
   **set**, which is what MMR determines; the order `Select` returns is this repository's contract
   and is asserted by its own tests. Returning indices rather than keybert's rounded scores is a
   second reason for the signature.
4. **The API's default stop-word list is none of the three oracles'.** Deliberate: a caller of
   `Lodestar.Text` should get `Lodestar.Text`'s list.

## Testing

Both target frameworks, both mirrors, as everything else here.

- Oracle replay per corpus, at the tolerances above.
- RAKE: the empty document, a document that is entirely stop words, `MinLength`/`MaxLength`
  boundaries, `IncludeRepeatedPhrases` on and off, each of the three metrics.
- TextRank: a document with one content word (a graph of one node, no edges), `Words` against
  `Ratio`, a window wider than the document, and non-convergence within `MaxIterations` — which
  throws rather than returning a half-iterated vector.
- MMR: `count` greater than the candidate count, `λ = 1` (pure relevance) and `λ = 0` (pure
  diversity) as the two closed-form ends, duplicate candidates, and a zero vector — whose cosine is
  undefined and which is refused by name.
- Guards: null text, null candidates, negative `count`, `lambda` outside `[0, 1]`, `Window < 1`,
  `Damping` outside `(0, 1)`, `Ratio` outside `(0, 1]`.

## Versions

**Neither package's version moves.** `Lodestar.Text` declares 0.5.0 and the feed holds 0.4.0;
`Lodestar.Embeddings` declares 0.6.0 and the feed holds 0.5.0. A declared version that has not
shipped is still open, so this work lands *in* those releases beside `BkTree` (#526) and the ONNX
split (#533) rather than past them. Incrementing before publication would mint a number nothing
contains.

## Definition of done

`dotnet test Lodestar.slnx -c Release` green on both target frameworks — the test count read, not
the colour. The three corpora regenerate without drift from a neutral working directory. Reference
pages under `docs/reference/text/keywords/` and `docs/reference/embeddings/search/`, with
`docs/wiki-map.json`'s `covered` table extended to `Lodestar.Text.Keywords`. A `docs/guides/keywords.md`
carrying the KeyBERT composition. A `docs/equivalence.md` row in the same commit as each function.
A `Lot*.cs` use of every new public type for the packaging gate. An ADR for the four divergences.
`tools/requirements.txt` and `tools/requirements.lock.txt` gain `rake-nltk`, `summa` and `keybert`.
The six Lint steps pass before the push.
