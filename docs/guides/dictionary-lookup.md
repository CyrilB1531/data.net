# Indexing strings for fast lookup — `Lodestar.Text.Indexing`

[`Process.Extract`](../reference/fuzzy/matching/process-extract.md) is linear: every call scores the query against every choice, in full. That is
fine for one query against a short list. It stops being fine for a spelling corrector that repeats
the same full scan for every keystroke against a dictionary of thousands of words.
`Lodestar.Text.Indexing` holds a metric index built once and queried many times: `BkTree` answers
"everything within edit distance `k`" without touching the whole corpus, on any distance that
satisfies the triangle inequality.

```bash
dotnet add package Lodestar.Text
```

## Building the tree and querying a radius

```csharp
using Lodestar.Text.Indexing;

BkTree dictionary = BkTree.OverLevenshtein();
dictionary.AddRange(["book", "books", "boo", "cook", "cake"]);

IReadOnlyList<BkTreeMatch> hits = dictionary.WithinDistance("bok", maxDistance: 1);
int found = hits.Count;         // nearest first: "book" and "boo" are one edit from "bok"
string nearest = hits[0].Item;
```

[`BkTree.OverLevenshtein`](../reference/text/indexing/bktree-overlevenshtein.md) is one of four
factories — `OverDamerauLevenshtein`, `OverIndel` and `OverHamming` are the others — each bound to
a distance that is a true metric. [`Osa.Distance`](../reference/text/distances/osa-distance.md) is
not offered: it fails the triangle inequality the pruning relies on
(`d("ab","bca") = 3 > d("ab","ba") + d("ba","bca") = 2`), and using it would return an incomplete
result set rather than throw.

## Where the tree stops paying

Measured with `BkTreeBenchmarks` (see
[`bench/README.md`](https://github.com/CyrilB1531/lodestar/blob/main/bench/README.md#17-bk-tree-vs-a-length-filtered-scan-issue-526)
for how) against
the baseline a caller actually writes instead of an index: a linear scan that skips any word whose
*length* already puts it out of range, then computes
[`Levenshtein.Distance`](../reference/text/distances/levenshtein-distance.md) on what survives.
**Both arms materialise and sort the same shape of answer** — a `(item, distance)` list ordered by
distance ascending — so neither is charged for producing a result the other does not; an earlier
version of this benchmark counted hits on the scan side and let only the tree pay for sorting its
result, which would have overstated the tree's cost. 20 000 words, 200 queries drawn from the
corpus itself — looking up a word already in the dictionary is the hardest case for the tree,
since its own neighbourhood is dense. `uniform` is independent random words; `clustered` is 2 500
roots plus one or two edits each, the shape a natural dictionary has. **Building the tree is
excluded from the table below** — it runs once in BenchmarkDotNet's `[GlobalSetup]`, before timing
starts, so every ratio compares query cost alone; the scan pays no equivalent setup, which is the
one asymmetry in the tree's favour anywhere in this comparison.

| radius | tree / length-filtered scan (uniform) | (clustered) |
| ---: | ---: | ---: |
| 1 | 0.52 | 0.59 |
| 2 | 1.35 | 1.58 |
| 3 | 1.66 | 1.75 |
| 4 | 1.79 | 1.74 |

Ratio is wall-clock mean time, tree over scan — machine and window are in
[`docs/guides/performance.md`](performance.md#bk-tree-vs-a-length-filtered-scan-issue-526). Below
`1` the tree wins; **above `1` it is slower to use than not building it at all.**

**Worthwhile only at `k = 1`, where it costs roughly half the time.** From `k = 2` on, the
length-filtered scan is the better answer — not merely "less of a win", but measurably slower to
use the tree. Counted purely by distance computations (the way a BK-tree's pruning is usually
judged, and how the figures a Python simulation predicted for this table were derived), the
tree's advantage also fades fast rather than holding: about a third as many distance calls as the
scan at `k = 1`, but 79–86% of them by `k = 2` and 92–96% by `k = 4` — almost the whole scan,
computed one node at a time instead of one array element at a time. Wall-clock time crosses over
one radius sooner than that comparison count does, because every tree node the traversal visits
costs a dictionary lookup keyed by exact distance, a stack push, and one call through the metric
delegate the tree stores rather than the inlinable static method the scan calls directly — on top
of the same list growth both arms now pay for their result, plus the array copy the tree's own
sort step makes to hand back its answer — against a scan whose rejected candidates cost one array
read and one integer subtraction. That per-node traversal cost, not the cost of sorting a result,
is what a comparison-count budget cannot see and a wall-clock budget always pays.

`k = 1` is also what a spelling corrector's first pass needs, which is why the structure exists.
Past it, a large radius over a large dictionary is a linear scan wearing a tree — reach for
`WithinDistance` at `k = 1`, and for a length-filtered scan beyond it.

## See also

- [`BkTree`](../reference/text/indexing/bktree.md) — the full admissible-distance table and every
  member.
- [`docs/guides/performance.md`](performance.md#bk-tree-vs-a-length-filtered-scan-issue-526) — the
  measured table, with its machine and window.
