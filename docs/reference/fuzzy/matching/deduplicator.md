# Deduplicator

Near-duplicate clustering over a dataset, with blocking so it does not compare everything to
everything.

<!-- docs-declaration -->

```csharp
public static class Deduplicator
```

**Example** — four records, two pairs.

```csharp
using Lodestar.Fuzzy;

string[] records = ["apple pie", "appel pie", "banana bread", "banana bred"];

IReadOnlyList<IReadOnlyList<int>> clusters = Deduplicator.FindClusters(
    records, record => record[..1], (a, b) => Fuzz.Ratio(a, b), threshold: 80);

int found = clusters.Count;  // => 2
```

**Remarks** — comparing every pair is `n²`, which is fine for a thousand records and impossible
for a million. Blocking is the way out: records sharing a key are compared, records that do not are
never considered at all.

That makes the **key the whole performance question, and the whole correctness risk**. Too coarse
and nothing is saved; too fine and true duplicates land in different blocks and are never compared.
A first letter, as above, is a demonstration rather than a recommendation.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Deduplicator.FindClusters`](deduplicator-findclusters.md), [`Fuzz`](fuzz.md).

## Members

| Member | What it does |
| --- | --- |
| [`Deduplicator.FindClusters`](deduplicator-findclusters.md) | Group near-duplicate records, comparing only within blocks. |
