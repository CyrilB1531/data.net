# Deduplicator.FindClusters

Group near-duplicate records, comparing only within blocks.

<!-- docs-declaration -->

```csharp
public static IReadOnlyList<IReadOnlyList<int>> FindClusters<T>(IReadOnlyList<T> records, Func<T, string> blockingKey, Func<T, T, double> similarity, double threshold)
```

**Parameters** — `records` is the dataset. `blockingKey` maps a record to the key that decides
which records it is compared against. `similarity` scores two records. `threshold` is the score at
which two are the same thing.

**Returns** — `IReadOnlyList<IReadOnlyList<int>>`: clusters of **indices** into `records`.

**Example** — two typo pairs, blocked on the first letter.

```csharp
using Lodestar.Fuzzy;

string[] records = ["apple pie", "appel pie", "banana bread", "banana bred"];

IReadOnlyList<IReadOnlyList<int>> clusters = Deduplicator.FindClusters(
    records, record => record[..1], (a, b) => Fuzz.Ratio(a, b), threshold: 80);

int clusterCount = clusters.Count;  // => 2
int firstSize = clusters[0].Count;  // => 2
```

**Remarks** — indices rather than records, so the caller can reach whatever else the row carries —
an id, a timestamp, the field that decides which duplicate to keep.

**Two records in different blocks are never compared, whatever their similarity.** That is the
trade the blocking key buys and the way this is misused: a key of `record => record[..1]` misses
every duplicate whose first character differs, typos included. Keys worth considering are a sorted
initials string, a phonetic encoding — `Soundex` or `Metaphone` — or a
truncated postcode.

`threshold` and `similarity` travel together: `80` means something different under
[`Fuzz.Ratio`](fuzz-ratio.md) than under [`Fuzz.TokenSetRatio`](fuzz-tokensetratio.md), which
scores far more generously. Changing the scorer without revisiting the threshold silently changes
what counts as a duplicate.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Deduplicator`](deduplicator.md), [`Fuzz.Ratio`](fuzz-ratio.md),
[the matching index](../matching.md).
