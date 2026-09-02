# 0074 — Phase 2's gaps restated on what the packages export, not what they advertise

**Status:** accepted · **Date:** 2026-09-02

## Context

[ADR 0059](0059-phase-0-verifications-two-confirmed-voids-do-not-survive-nuget.md) closed V7 by
searching NuGet, and says of itself:

> **0059 records what NuGet shows, not what the code does.** Nobody has run those packages. A
> restated gap needs that reading, and a claim that one of them is inadequate needs it more.

[#440](https://github.com/CyrilB1531/lodestar/issues/440) has been held on exactly that sentence:
its lots 1, 2, 4 and 5 may not open until each gap is restated on what the incumbents do *not* do.
This is that reading.

**Method.** Each package was downloaded from `api.nuget.org/v3-flatcontainer` and its assembly
loaded into a `MetadataLoadContext`, which enumerates the exported types and their public members
without executing anything. A README is a claim; `GetExportedTypes()` is the surface. Download
counts are nuget.org's. **Read on 2026-09-02.**

Three of the four lots lose their premise.

## 1 — `Text.Similarity` is closed: the sketch, the index and the solver all ship

`MinHashSharp` **1.1.1**, 6 753 downloads, is not the sketch-only library the lot assumed:

```text
MinHash(int numPerm, int seed, Func<,> hashFunc)
double Jaccard(MinHash other)          uint[] HashValues(int start, int end)
MinHashLSH(double threshold, int numPerm)
MinHashLSH(double threshold, int numPerm, (double, double) weights)
void Insert(string key, MinHash mh)    IEnumerable<T> Query(MinHash mh)
void Freeze()   void Serialize(string path)   MinHashLSH Deserialize(string path, bool, bool)
```

`MinHashLSH`'s constructor takes a **threshold** and a false-positive / false-negative **weight
pair**. Solving threshold → (b, r) is therefore already done inside it — which is the part #440
named as the one users get wrong by hand.

`SimhashLib` **1.0.0**, 3 850 downloads, covers the SimHash half end to end: `Shingling.Slide` and
`Tokenize`, `Simhash.ComputeHash` over Jenkins, MD5 and MurmurHash3, `SimhashResult.Distance`, and
a banded `SimhashIndex(objs, f, k)` with `GetNearDups` and `MakeOffsets`.

**And `Lucene.Analysis.Minhash.MinHashFilter` ships today**, in `LuceneSharp.Analysis.Common`
**26.8.4415**, 14 417 downloads. #440's reasoning — *"`MinHashFilter` shipped in 6.2 while
Lucene.NET is pinned at 4.8, so the .NET port does not have it and will not soon"* — was about
`Lucene.Net`. LuceneSharp is a different port, tracking 26.x, and it has the filter.

What no surveyed package exposes is one-permutation hashing with densification, b and r as inputs,
and the inverse solve as a public function. **That is a refinement of three shipping libraries, not
a void**, and it does not carry a package.

## 2 — `Text.Keywords` survives, minus YAKE

- `Yake.NET` 1.0.0, 151 downloads — "no C# YAKE exists at all" was already false, per 0059.
- A search for `rake textrank` returns **zero packages**.
- `TajikKEA` 1.0.0 is Tajik-specific: `IWordContext`, `IDFCategory`, per-language stop words.
- `APIVerve.API.KeywordExtractor` is a client for a remote HTTP service, not local extraction.

**RAKE, TextRank and a KeyBERT-style MMR over the ONNX embeddings are unserved** by any local .NET
package. The lot opens at that reduced scope, YAKE removed.

## 3 — `Text.Search` is closed on its stated value, and reopens smaller

`LuceneSharp.Core` **26.8.4415**, 20 548 downloads, exports `BM25Similarity` beside twelve other
similarities — `DFI`, `DFR`, `IB`, `LMDirichlet`, `LMJelinekMercer`, `IndriDirichlet`, `Classic`,
`Boolean`, `SweetSpot` — **and the Block-Max WAND machinery**:

```text
Lucene.Search.MaxScoreCache            Lucene.Search.ImpactsDISI
Lucene.Codecs.CompetitiveImpactAccumulator   Lucene.Codecs.Impact
Lucene.Index.Impacts / ImpactsEnum / IImpactsSource / SlowImpactsEnum
```

The issue said *"the value is in WAND / Block-Max WAND top-k, not in the formula"*. That is the half
that ships.

Reciprocal rank fusion ships too: `drittich.ReciprocalRankFusion` 1.0.1, 497 downloads, is exactly
one method, `SearchResultFuser.FuseSearchResults(Dictionary<,>, int k)`.

`SemanticKernel.Rankers.BM25` 1.3.5, 6 001 downloads, has `BM25Reranker` and
`CorpusStatistics { AverageDocumentLength, DocumentFrequencies, TotalDocuments }` — but its
assembly references `Mosaik.Core` (Catalyst), and the load context cannot read `BM25Reranker`'s
constructor without it. It is not the dependency-free option.

**What is left is BM25 scored over an in-memory `CsrMatrix` from `CountVectorizer`** — no
`Directory`, no `IndexWriter`, no codec, no NLP dependency. Smaller than the lot as written, and no
longer about WAND.

## 4 — `Text.Index` keeps its BK-tree half only

`vptree` **0.9.1** targets `net8.0` and `net10.0`, was last pushed **2026-04-08**, 537 downloads:

```csharp
delegate double CalculateDistance<T>(T item1, T item2);
static VpTree<T> Create(T[] newItems, CalculateDistance<T> distanceCalculator);
void Search(T target, int numberOfResults, out T[] results, out double[] distances);
```

Generic over any metric, so our distance kernels plug straight in. #440's *"existing repos are 2–5 ★
and abandoned"* is out of date for this half.

It offers no radius query — k-NN only — no incremental insert, and `Create` takes the whole `T[]`
up front.

**BK-tree is genuinely absent.** A search for `bktree` returns `FSharpx.Core` and `CaseON`, neither
of which contains one. That is the structure that matters for edit-distance dictionary lookup over
a discrete metric, and it is what the lot reduces to.

## Decision

**Lots 1 and 4 of #440 do not open.** Their value was named in the issue, and it ships.

**Lots 2 and 5 open at the scope above**: RAKE, TextRank and MMR for the first; BK-tree, plus the
radius query and incremental insert `vptree` lacks, for the second.

## Consequences

The roadmap's protocol gains a step it did not have: *check NuGet* found four packages, and
**reading their exported surface then closed two more lots that the search alone had left open**. A
package's description is not a capability list — `MinHashSharp` reads as a sketching library and
carries an LSH index with a threshold solver; `LuceneSharp.Core` reads as a Lucene port and carries
the exact top-k algorithm a lot was proposed to write.

This ADR records a reading, like 0059 — nothing here was benchmarked, and a claim that one of these
packages is *slow* would need its own measurement.
