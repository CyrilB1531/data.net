# String indexing — `Lodestar.Text.Indexing`

Comparing a query against every entry in a dictionary is `O(n)` per lookup, and that cost is paid
again for every query. `Lodestar.Text.Indexing` holds a metric index built once and queried many
times: a Burkhard-Keller tree that answers "everything within edit distance `k`" and "the `n`
nearest" without a full scan, on any distance that satisfies the triangle inequality.

[`BkTree`](indexing/bktree.md) is the whole namespace. It carries the measured cost of the
pruning — worth using at a radius of 1, measurably slower than a length-filtered scan past it —
and the table of which distances in [`Lodestar.Text.Distances`](distances.md) are admissible. Read
it before choosing a radius or a factory method.

## Types

| Type | What it is |
| --- | --- |
| [`BkTree`](indexing/bktree.md) | A metric index over strings: everything within edit distance `k`, without scanning the corpus. |
| [`BkTreeMatch`](indexing/bktreematch.md) | One hit from a `BkTree` query: the item, and how far it is. |

## See also

- [`Lodestar.Text.Distances`](distances.md) — the distances a tree is built over, and the triangle
  inequality that decides which ones qualify.
- [`Process.ExtractIndexed`](../fuzzy/matching/process-extractindexed.md) — a `BkTree` used as a
  prefilter in front of a fuzzy-matching scorer.
