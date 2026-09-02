# 0526 — `Lodestar.Text.Indexing`: a BK-tree over the integer distances

**Issue:** [#526](https://github.com/CyrilB1531/lodestar/issues/526) ·
**Status:** accepted · **Date:** 2026-09-02

## Problem

`Lodestar.Text` computes a distance between two strings quickly. `Lodestar.Fuzzy`'s
`Process.Extract` computes it between one query and *every* choice, linearly. That is fast per pair
and every pair — the corpus size is in the query cost, so a dictionary of twenty thousand words
costs twenty thousand dynamic programs per lookup.

A BK-tree removes most of them. It is built on a *discrete* metric, keys each node's children by
the exact distance to that node, and prunes to `[d − k, d + k]` at every step, which makes
*"every word within edit distance k"* — the spelling-correction query — sublinear.

[ADR 0074](../../decisions/0074-the-phase-2-gaps-restated-on-what-the-packages-export.md) closed
the VP-tree half of [#440](https://github.com/CyrilB1531/lodestar/issues/440)'s lot 5: `vptree`
0.9.1 targets `net10.0`, was pushed 2026-04-08, and is generic over any metric. **BK-tree is the
half that is genuinely absent** — a NuGet search for `bktree` returns `FSharpx.Core` and `CaseON`,
neither of which contains one.

## What it is worth, measured before it was designed

The tempting claim is "sublinear, therefore fast". Measured against the baseline a caller actually
writes — a linear scan that skips any word whose *length* already puts it out of range, which is
three lines and costs nothing — the tree is worth far less than that at the radii people reach for.

Twenty thousand words, two hundred queries drawn from the corpus, counting the distance
computations each approach performs. Two corpora: **uniform** random words of length 4–10 over the
Latin alphabet, and **clustered** — 2 500 roots plus one or two random edits each, which is the
shape a natural dictionary has.

| radius | BK-tree nodes visited | length-filtered scan | ratio (uniform) | ratio (clustered) |
| ---: | ---: | ---: | ---: | ---: |
| k = 1 | 2 521 / 2 109 | 7 843 / 7 363 | **0.32** | **0.29** |
| k = 2 | 9 321 / 7 719 | 11 889 / 11 540 | 0.78 | 0.67 |
| k = 3 | 13 956 / 11 790 | 15 057 / 14 848 | 0.93 | 0.79 |
| k = 4 | 16 820 / 14 580 | 17 606 / 17 239 | 0.96 | 0.85 |

**The tree pays at k = 1 — roughly three times fewer distance computations — pays something at
k = 2, and stops paying at k ≥ 3.** A visited node costs a full distance computation, the same work
the scan does, so the ratio *is* the relative cost up to the tree's pointer-chasing overhead.

That is not a reason not to ship it: k = 1 and k = 2 are the radii a spelling corrector uses, and
the corpus-size term is what a caller wants gone. It is a reason the number belongs in the
reference page and the guide rather than only here. **A structure that reads as a speedup and is
not one at k = 3 is worse than no structure**, because the caller stops looking.

The measurement is reproduced in `bench/` (see *Benchmarks*), so it can be re-run rather than
believed.

## Which distances qualify, measured rather than assumed

A BK-tree is correct only on a distance satisfying the triangle inequality — the pruning to
`[d − k, d + k]` *is* the triangle inequality. Getting this wrong does not throw; it silently
returns an incomplete set, which is the worst failure mode a lookup structure has.

Exhaustively checked over every triple: all words up to length 4 on a three-letter alphabet
(121³ ≈ 1.77 M triples) and up to length 6 on a two-letter alphabet (127³ ≈ 2.05 M).

| distance | triangle inequality | admitted |
| --- | --- | :-: |
| [`Levenshtein.Distance`](../../reference/text/distances/levenshtein.md) | holds (standard result) | yes |
| [`DamerauLevenshtein.Distance`](../../reference/text/distances/dameraulevenshtein.md) | holds — unrestricted, so it is a true metric | yes |
| [`Indel.Distance`](../../reference/text/distances/indel.md) | holds — `len(a) + len(b) − 2·LCS` is the LCS edit distance | yes |
| [`Hamming.Distance`](../../reference/text/distances/hamming.md) | **0 violations measured** on both sweeps | yes |
| [`Osa.Distance`](../../reference/text/distances/osa.md) | **violated**: `d("ab","bca") = 3 > d("ab","ba") + d("ba","bca") = 1 + 1` | no |
| `Lcs.SubsequenceLength` | not a distance at all — it returns a length | no |
| `Jaro`, `JaroWinkler`, `RatcliffObershelp` | similarities, and `JaroWinkler` is not a metric | no |

Two of those are worth saying out loud because #526 got them wrong when it was written.
**`Lcs` is not a candidate** — `Lcs.SubsequenceLength` returns the length of the longest common
subsequence, and `Indel` is the distance built from it. And **Lodestar's `Hamming` is not textbook
Hamming**: it adds the absolute length difference for unequal-length inputs, which textbook Hamming
refuses outright. That variant had to be checked rather than assumed, and it holds.

`Osa`'s own summary already says *"Not a metric"*. The counter-example above is what that costs a
caller who ignores it, and it goes in the reference page for `BkTree` so the reason travels with
the constraint.

## Scope

One class, in a new `Lodestar.Text.Indexing` namespace inside the existing `Lodestar.Text` package,
plus one overload in `Lodestar.Fuzzy`.

```csharp
namespace Lodestar.Text.Indexing;

/// <summary>One hit: the indexed item and its distance to the query.</summary>
public readonly record struct BkTreeMatch(string Item, int Distance);

public sealed class BkTree
{
    public BkTree(Func<string, string, int> metric);

    public static BkTree OverLevenshtein(TextElement element = TextElement.Utf16Unit);
    public static BkTree OverDamerauLevenshtein(TextElement element = TextElement.Utf16Unit);
    public static BkTree OverIndel(TextElement element = TextElement.Utf16Unit);
    public static BkTree OverHamming(TextElement element = TextElement.Utf16Unit);

    public int Count { get; }

    public bool Add(string item);
    public void AddRange(IEnumerable<string> items);

    public IReadOnlyList<BkTreeMatch> WithinDistance(string query, int maxDistance, int? limit = null);
    public IReadOnlyList<BkTreeMatch> Nearest(string query, int count);
}
```

**Not generic.** #526 proposed `BkTree<T>`. All four admissible metrics are string metrics, the
package is a text package, and `vptree` already serves the bring-your-own-type case. A type
parameter every documentation example carries, for no named user, is the definition of what YAGNI
removes. The `Func<string, string, int>` constructor is the extension point that matters, and it is
public.

**Structure.** A node holds its item and `Dictionary<int, Node>` children keyed by the exact
distance from that item. `Add` walks from the root: at each node compute `d`; `d == 0` means the
item is already present, so return `false`; otherwise descend into the child at `d` or attach a new
node there. `WithinDistance` walks the same way: report the node when `d <= maxDistance`, and
descend into every child whose key lies in `[d − maxDistance, d + maxDistance]`.

**`Nearest` is `WithinDistance` with a shrinking radius.** Keep a bounded max-heap of `count`
hits; the radius is `int.MaxValue` until the heap fills and the current worst distance afterwards.
Each hit that displaces the heap's worst tightens the bound, so the pruning gets stronger as the
search proceeds. No second traversal.

**Ordering.** Both queries return distance ascending, ties by insertion order — the convention
`Process.Extract` already holds ("stable order: score descending, ties by original index"). The
tree's *shape* depends on insertion order; its *answers* must not, and a test proves it.

**`limit` applies after that ordering**, so `WithinDistance(q, k, limit: n)` returns the `n`
*nearest* hits within `k`, never the first `n` the traversal happened to reach. It is a cap on the
returned list, not a bound on the search: the traversal still visits what the radius requires,
because a nearer hit can be found at any point in it. `Nearest(q, n)` is the query that genuinely
tightens as it goes.

**Duplicates.** `Add` returns `false` for an item already present and `Count` counts distinct
items, matching `HashSet<T>.Add`. A duplicate can only ever be the node itself, since `d == 0`
terminates the walk.

**No removal.** Deleting from a BK-tree means re-inserting the deleted node's whole subtree, and
nothing in this issue's motivating case removes words from a dictionary. It is not in scope, and
the reference page says so rather than leaving a reader hunting for it.

## The `Lodestar.Fuzzy` overload, and the contract it cannot dodge

```csharp
public static IReadOnlyList<ExtractResult> ExtractIndexed(
    string query,
    BkTree index,
    int maxDistance,
    Func<string, string, double>? scorer = null,
    int? limit = 5,
    double scoreCutoff = 0.0);
```

Candidates come from `index.WithinDistance(query, maxDistance)`; scoring, sorting, cutoff and limit
are then exactly `Extract`'s, so the two return the same shape and the same `ExtractResult`.

**They do not always return the same thing, and the documentation must lead with that.**
`ExtractIndexed` equals `Extract` over the same items **if and only if every choice further than
`maxDistance` would have scored below `scoreCutoff`**. The tree filters on an integer edit
distance; the scorer is a similarity in `[0, 100]`, by default `Fuzz.WRatio`, which is not a
function of that distance. A caller who sets `maxDistance = 1` and `scoreCutoff = 0` gets a
*subset*, silently.

So `ExtractIndexed` is documented as **prefilter-then-verify**, not as "a faster `Extract`", and
the reference page carries the condition as a precondition on the caller rather than a remark. A
test asserts both directions on a corpus: equality when the cutoff is consistent with the radius,
and a named case where it is not.

`ExtractIndexed` lives in `Process` beside `Extract` and `ExtractOne`.

## Placement

**Inside `Lodestar.Text`**, not a new package.

The criterion is whether a caller would want this without the rest of `Lodestar.Text`. They would
not: all four admissible metrics are in `Lodestar.Text.Distances`, so a tree built over them cannot
be used without the package, and a caller who brings their own `Func<string, string, int>` is
served by `vptree` today.

The release cost decides the rest. `src/` references published packages, never projects
([ADR 0069](../../decisions/0069-the-package-layout-as-built-and-what-enforces-it.md)), so a
package between `Text` and `Fuzzy` would make this branch — which touches both — a three-step
release chain rather than two. This branch already needs
`export LodestarUseProjectRefs=true` and CONTRIBUTING's
[*Working across two packages*](../../../CONTRIBUTING.md#working-across-two-packages) order:
`Lodestar.Text` 0.6.0 ships first, the floor in `src/Directory.Packages.props` moves, then
`Lodestar.Fuzzy` 0.5.0.

`Lodestar.Text`'s package description gains the index. `docs/wiki-map.json` gains
`Lodestar.Text.Indexing` in the `covered` table, so the reference gate enforces the new surface.

## Testing

**The oracle is brute force, not another implementation.** There is no canonical Python BK-tree to
replay, and there does not need to be: for a corpus of words and a `(query, radius)`, the correct
answer is the set a linear scan with the same distance returns. That set is a property of the
distance and the corpus, not of any tree — so freezing it is *stronger* than parity with someone
else's structure, which could be wrong in the same way we are.

`tools/generate_oracles.py` gains `decomposition`-style fixtures under
`tests/oracles/text_bktree.json`: several corpora drawn from `SeededRandom`, each with queries at
radii 0 to 3, and for each the sorted expected `(item, distance)` list computed by scanning. The
distances themselves are already frozen by `levenshtein.json` and its siblings, so this corpus adds
the *set*, not the arithmetic.

Beyond the corpus, three properties, all in C# and needing no oracle:

1. **Shape independence.** For many random insertion orders of the same items,
   `WithinDistance(q, k)` returns the same set. This is what proves the tree, and it is the test a
   subtly wrong pruning bound fails.
2. **Agreement with the scan.** Over random corpora and radii, `WithinDistance` equals a linear
   scan with the same metric — the same statement as the oracle, run over inputs the corpus does
   not fix.
3. **`Nearest` agrees with `WithinDistance`.** `Nearest(q, n)` is the first `n` of
   `WithinDistance(q, int.MaxValue)`, ordered. The shrinking radius is an optimization and must not
   change the answer.

Plus the ordinary edges: an empty tree, a query equal to an indexed item (`d = 0`), `maxDistance`
of 0, `limit` smaller than the hit count, `Add` returning `false` on a duplicate, and `Guard`
rejection of a null item, a null query and a negative `maxDistance`.

Everything runs on both target frameworks through the linked `*.NetStandard.Tests` sources, as
usual.

## Benchmarks

`bench/Lodestar.Text.Benchmarks` gains the comparison the *What it is worth* table above states, so
the claim is reproducible on a named machine rather than trusted from this document:
`BkTree.WithinDistance` against a length-filtered linear scan, at radii 1 to 4, over a generated
dictionary. `bench/corpus/` gains its generator, seeded like the others through
`tools/seeded_random.py`, and `bench/bench-map.json` its entry.

`docs/guides/performance.md` gets the resulting numbers with their machine and window, per this
repository's rule that a measurement lives with its machine.

## Definition of done

The project's usual, and `CONTRIBUTING.md` is the authority:

- Oracle corpus committed, and the `Oracles are reproducible` gate green.
- Both target frameworks, tests linked into `Lodestar.Text.NetStandard.Tests` and
  `Lodestar.Fuzzy.NetStandard.Tests`.
- A row in `docs/equivalence.md` **in the same commit as the function** — with the honest note that
  there is no Python counterpart, since no canonical library exposes this.
- Reference pages under `docs/reference/text/indexing/` for `BkTree` and `BkTreeMatch`, and
  `docs/reference/fuzzy/matching/process-extractindexed.md` added beside
  `process-extract.md`, with `process.md`'s member list extended; `docs/wiki-map.json` updated so
  the gate enforces them.
- `BkTreeSample.cs` and `BkTreeMatchSample.cs` in `samples/Lodestar.Sample`, reachable from
  `Program.cs`, so `check_sample_coverage.py` and the packaging gate pass.
- A guide section, carrying the radius table so a reader learns where the structure stops paying.
- `CHANGELOG.md` entries for `Lodestar.Text` 0.6.0 and `Lodestar.Fuzzy` 0.5.0.
