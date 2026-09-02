# BkTree

A metric index over strings: everything within edit distance `k`, without scanning the corpus.

A Burkhard-Keller tree keys each node's children by the exact distance from that node's item, so a
radius query descends only into `[d - k, d + k]` at every step. It is what turns the distances in
`Lodestar.Text.Distances` from fast per pair into fast per query against a dictionary.

**Properties** — `Count` is how many distinct items the tree holds, `0` for an empty tree.
Duplicates refused by [`Add`](bktree-add.md) are not counted, and it is maintained on insert
rather than walked, so reading it is free.

## Where it stops paying

Measured against the baseline a caller actually writes — a linear scan that skips any word whose
length already puts it out of range — over 20 000 words and 200 queries
([`docs/guides/performance.md`](../../../guides/performance.md#bk-tree-vs-a-length-filtered-scan-issue-526)
has the machine and the window). The `uniform` corpus is random words; `clustered` is 2 500 roots
plus one or two edits each, the shape a natural dictionary has.

| radius | tree / length-filtered scan (uniform) | (clustered) |
| ---: | ---: | ---: |
| k = 1 | 0.54 | 0.61 |
| k = 2 | 1.32 | 1.54 |
| k = 3 | 1.58 | 1.83 |
| k = 4 | 1.80 | 1.70 |

Ratio is wall-clock mean time, tree over scan. **Worth using only at `k = 1`, where it costs
roughly half the time; past it, worse than not building the tree at all.** Counted by distance
computations alone the tree still wins out to `k = 3`–`4` — a visited node costs one distance
call, and the tree visits far fewer of them at low radius — but every visited node also costs a
`Dictionary<int, Node>` lookup, a stack push and list growth, against a scan candidate's one array
read and one integer subtraction; that per-node cost overtakes the fewer-calls advantage at
`k = 2` and the gap widens from there.
[`docs/guides/dictionary-lookup.md`](../../../guides/dictionary-lookup.md#where-the-tree-stops-paying) has the full
account of the two measures diverging. `k = 1` is the radius a spelling corrector's first pass
uses, which is why the structure exists; past it, scan.

## Which distances it accepts

Correct **only** on a distance satisfying the triangle inequality — the pruning to `[d - k, d + k]`
*is* that inequality, and a distance that violates it returns an incomplete set rather than
throwing. The four factory methods bind the ones that qualify, verified over every triple of words
up to length 4 on a three-letter alphabet and up to length 6 on a two-letter one.

| distance | admissible |
| --- | :-: |
| `Levenshtein.Distance` | yes |
| `DamerauLevenshtein.Distance` | yes — unrestricted, so a true metric |
| `Indel.Distance` | yes |
| `Hamming.Distance` | yes — Lodestar's variant adds the length difference, and still holds |
| `Osa.Distance` | **no** — `d("ab","bca") = 3 > d("ab","ba") + d("ba","bca") = 2` |
| `Jaro`, `JaroWinkler`, `RatcliffObershelp` | **no** — similarities, not distances |

`Lcs.SubsequenceLength` is not a candidate either: it returns a length. `Indel` is the distance
built from it.

## Members

| Member | What it does |
| --- | --- |
| [`BkTree.OverLevenshtein`](bktree-overlevenshtein.md) | Builds a tree over `Levenshtein.Distance`. |
| [`BkTree.OverDamerauLevenshtein`](bktree-overdameraulevenshtein.md) | Builds a tree over `DamerauLevenshtein.Distance`. |
| [`BkTree.OverIndel`](bktree-overindel.md) | Builds a tree over `Indel.Distance`. |
| [`BkTree.OverHamming`](bktree-overhamming.md) | Builds a tree over `Hamming.Distance`. |
| [`BkTree.Add`](bktree-add.md) | Adds one item, returning `false` if it is already indexed. |
| [`BkTree.AddRange`](bktree-addrange.md) | Adds every item, skipping duplicates. |
| [`BkTree.WithinDistance`](bktree-withindistance.md) | Every item within a radius, nearest first. |
| [`BkTree.Nearest`](bktree-nearest.md) | The *n* nearest items, however far they are. |

**Removal is not offered.** Deleting from a BK-tree means re-inserting the deleted node's whole
subtree, and nothing this index is for removes words from a dictionary. Build a new tree.

**Thread safety** — not safe for concurrent writes. Concurrent queries against a tree nothing is
adding to are safe.
