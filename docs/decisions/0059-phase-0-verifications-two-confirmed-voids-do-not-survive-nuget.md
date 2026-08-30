# 0059 — Phase 0's verifications: two confirmed voids do not survive a NuGet search

**Status:** accepted · **Date:** 2026-08-30

## Context

[#427](https://github.com/CyrilB1531/lodestar/issues/427)'s roadmap opens phases on gaps, and
[#437](https://github.com/CyrilB1531/lodestar/issues/437) exists because each gap was read from
documentation that may have gone stale. Its exit criterion is answers committed here, each with its
date and its source. This records **V3, V4, V5 and V7**; V6 wants a measurement rather than a
reading and is left to its own lot.

The roadmap's own protocol is what this applies outward: *"check our own repo before declaring a
gap — this roadmap initially proposed a metrics phase that was already shipped."*

**Read on 2026-08-30.** Every figure below is what the named page showed that day.

## V3 — Math.NET has not moved, and sparse factorizations are still absent

`MathNet.Numerics` last shipped a **stable 5.0.0 on 2022-04-03**. The only movement since is
`6.0.0-beta2`, **2025-03-02** — one prerelease in four years.
([nuget.org](https://www.nuget.org/packages/MathNet.Numerics))

Its QR and SVD are dense. The request for a sparse SVD,
[mathnet-numerics#117](https://github.com/mathnet/mathnet-numerics/issues/117), has been **open
since 2013-04-24** and was opened for exactly our shape of problem: a matrix too large for a dense
result.

**So the roadmap's Phase 2 plan stands as written** — randomized SVD as our sparse SpMM plus
Math.NET's *dense* QR and *dense* SVD on the small projected matrix, with only `svd_flip` ours. The
documentation it was planned from was generated from 5.0.0, and 5.0.0 is still the stable release,
so nothing read from it has expired.

**What did turn up is a better dependency for the sparse half, if one is ever wanted.**
`CSparse` is at **4.4.1, published 2026-08-24, 171 300 downloads**, targeting `net8.0`, `net10.0`
and `netstandard2.0`, and provides **sparse LU, Cholesky and QR**.
([nuget.org](https://www.nuget.org/packages/CSparse)) It is not needed by the plan as written —
randomized SVD never factorizes the sparse matrix — but a decomposition lot that finds it does
should reach for this rather than write one.

## V4 — ML.NET does centre, and the roadmap's reason for the gap is wrong anyway

`ProjectToPrincipalComponents` takes `bool ensureZeroMean = PrincipalComponentAnalyzer.Defaults.EnsureZeroMean`,
and that default is `true`:

```csharp
internal static class Defaults
{
    public const string WeightColumn = null;
    public const int Rank = 20;
    public const int Oversampling = 20;
    public const bool EnsureZeroMean = true;
    public const int Seed = 0;
}
```

([`PCACatalog.cs`](https://github.com/dotnet/machinelearning/blob/main/src/Microsoft.ML.PCA/PCACatalog.cs),
[`PcaTransformer.cs`](https://github.com/dotnet/machinelearning/blob/main/src/Microsoft.ML.PCA/PcaTransformer.cs))

**But it does not densify to centre, which is what the roadmap assumed.** The mean is subtracted
*after* the projection, not from the input:

```csharp
editor.Values[i] = VectorUtils.DotProductWithOffset(
    transformInfo.Eigenvectors[i], 0, in src) -
    (transformInfo.MeanProjected == null ? 0 : transformInfo.MeanProjected[i]);
```

`src` is a `VBuffer<float>` throughout, so the dot product runs over the stored entries. **ML.NET
centres and still consumes a sparse vector in O(nnz).** The roadmap's *"if it centres data it does
not compete with TruncatedSVD on sparse input"* is therefore false as stated, and any phase opened
on it would have been opened on a wrong reason.

**The gap survives, restated.** What separates the two is not how sparsity is handled but **what is
computed**: `TruncatedSVD` deliberately does *not* centre, which is the whole reason it is what LSA
uses on tf-idf, and centred PCA on the same matrix is a different quantity. That distinction is
narrower than the roadmap's, and it narrows further still — `ensureZeroMean: false` gives an
uncentred projection, which is TruncatedSVD-shaped.

**What is left of the gap, and a `Decomposition` lot must say which it is taking:** rank is a
constructor default of 20 rather than a swept parameter, there is no explained-variance ratio, no
NMF, and the whole thing is `IDataView`-coupled. Those are real and none of them is "it densifies".

## V5 — the Phase 5 reading is correct, and the interface has a name

`Microsoft.Extensions.VectorData.Abstractions` is at **10.9.0 stable, 2026-08-11**, with monthly
stable releases through 2026. ([nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.VectorData.Abstractions))

The API is `VectorStore` as the top-level type and `VectorStoreCollection<TKey, TRecord>` for a
named collection — which is what the roadmap already says, so the October 2024 blog it warned
about did not mislead it.

**Hybrid search is `IKeywordHybridSearchable<TRecord>`**, implemented only by providers whose
database supports it, and a caller is expected to test for it. That is the concrete thing Phase 5's
*"our BM25 + RRF and our kNN together are an in-process hybrid provider"* would conform to, and it
is now a named interface rather than a described capability.

## V7 — the searches were done one way, and two confirmed voids do not survive the other

Every "confirmed void" in Phase 2 rested on GitHub searches. Repeating them on NuGet:

| roadmap claim | what NuGet shows on 2026-08-30 |
| --- | --- |
| **MinHash/LSH** "confirmed void: MinHashSharp (12 ★, frozen 2023)" | `Atulin.MinHash`, updated **2026-04-19**, "Maximum-performance MinHash for .NET 10. Zero-allocation hot paths, SIMD-accelerated (AVX2/SSE2)". Also `Mostlylucid.StyloExtract.Fingerprint`, **2026-06-30**, 8 649 downloads, "MinHash, LSH banding, anchor-path signatures" |
| **BM25** "Lucene.NET has `BM25Similarity` but … pinned at 4.8. The gap is BM25 *without an indexing engine*" | 95 packages match. `LuceneSharp.Core`, **2026-08-23**, 20 542 downloads, "Modern .NET 10 port of Apache Lucene" with BM25 scoring. `ElBruno.BM25`, 675 downloads, "Lightweight, zero-dependency BM25 full-text search" — which *is* the stated gap |
| **Keywords** "No C# YAKE at all" | `Yake.NET` 1.0.0, **2026-02-22**, 151 downloads, "A C# implementation of YAKE … for single documents" |

**None of these three claims is true as written.** Two of them — MinHash/LSH and BM25 — are not
merely inaccurate but describe the opposite of what is there: a .NET 10 SIMD MinHash from four
months ago, and both a modern Lucene port and a standalone engine-free BM25.

The download counts matter to how much this changes. `Atulin.MinHash` at 352 and `ElBruno.BM25` at
675 are not incumbents in any meaningful sense, and `Yake.NET` at 151 is a first release. But
**"confirmed void" is not a claim about adoption, it is a claim about existence**, and it is the
claim the roadmap used to justify opening the phases.

## Decision

**Phase 2 may not open on its void claims as written.** Items 1 (`Text.Similarity`), 2
(`Text.Keywords`) and 4 (`Text.Search`) each rest on a statement this verification refutes, and
each needs restating — on what the existing packages do *not* do, and on measurements against them
— before a lot is written. That is Phase 1's P2 and P3 work ("name the incumbents", "without
Fastenshtein in the table, no performance claim will be believed") arriving earlier than planned,
because V7 found the incumbents Phase 1 was going to name.

**Phase 2 item 3 (`Decomposition`) may open**, with its gap restated per V4: the distinction is
uncentred LSA against centred PCA, a swept rank, explained variance, NMF, and freedom from
`IDataView` — not sparsity handling, which ML.NET does correctly.

**Phase 5 may open as written.** V5 confirms the API and names the interface.

## What was refused

**Treating a low download count as a void.** `Atulin.MinHash` has 352 downloads and it would have
been easy to call that "no real incumbent" and proceed. But the roadmap's own protocol asks whether
a capability exists, and it does; a package's obscurity is an argument about *positioning*, which
belongs to Phase 1, not an argument that the code does not exist. Deciding the two are the same is
how a phase gets opened on a gap that closed.

**Reading V4 as "the gap is gone".** ML.NET centring by default and still handling sparse input
refutes the roadmap's *reason*, not its conclusion. Throwing the conclusion out with the reason
would have been the same error in the other direction.

## Consequences

- **V3, V4, V5 and V7 are answered; V6 is not.** V6 asks how `TensorPrimitives.CosineSimilarity`
  performs against our kernel, which is a measurement, and the open-risks table entry
  *"`TensorPrimitives` makes the kNN redundant"* stays open until one exists.
- **The `Decomposition` lot inherits a narrower gap than the roadmap gave it**, and a specific
  thing to measure against: `ProjectToPrincipalComponents` with `ensureZeroMean: false`, which is
  the closest thing in .NET to what it would ship.
- **What would change this decision** is any of these packages being read rather than listed. This
  records what NuGet shows, not what the code does: nobody has run `Atulin.MinHash`, checked
  whether `ElBruno.BM25` implements WAND, or compared `Yake.NET` against the reference
  implementation. A restated gap needs that reading, and a claim that one of them is inadequate
  needs it more.
